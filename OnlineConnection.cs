using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilklessCoop
{
    internal class OnlineConnection : MonoBehaviour
    {
        // config
        public int TickRate = 20;
        public float PlayerOpacity = 0.7f;
        public bool Connected = false;

        // connection
        private ManualLogSource _logger;
        private TcpClient _socket;
        private NetworkStream _stream;
        private bool _rxRunning;
        private Thread _rxThread;
        private Queue<string> _rxQueue;

        // game
        private GameObject _hornet;
        private tk2dSprite _hornetSprite;
        private Rigidbody2D _hornetRigidbody;
        private int _playerCount;
        private Dictionary<string, GameObject> _playerObjects;
        private Dictionary<string, tk2dSprite> _playerSprites;
        private Dictionary<string, SimpleInterpolator> _playerInterpolators;

        private GameObject _mapQuad;
        private GameObject _compassIcon;
        private GameObject _uiPinRoot;
        private List<GameObject> _uiPins;

        public void Connect(string address, int port, ManualLogSource logger)
        {
            try
            {
                Task.Run(() =>
                {
                    _logger = logger;
                    _logger.LogInfo($"Attempting to connect to {address}:{port} ({TickRate}Hz)...");

                    // set up internal variables
                    _hornet = null;
                    _uiPinRoot = null;
                    _uiPins = new List<GameObject>();
                    _playerObjects = new Dictionary<string, GameObject>();
                    _playerSprites = new Dictionary<string, tk2dSprite>();
                    _playerInterpolators = new Dictionary<string, SimpleInterpolator>();
                    _rxQueue = new Queue<string>();

                    // set up connection variables
                    _socket = new TcpClient(address, port);
                    _stream = _socket.GetStream();
                    _stream.ReadTimeout = 500;
                    _rxRunning = true;

                    // start threads
                    _rxThread = new Thread(RxThreadFunction);
                    _rxThread.Start();
                    InvokeRepeating("Tick", 0, 1.0f / TickRate);

                    // success
                    Connected = true;

                    _logger.LogInfo("Connected successfully.");
                });
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to connect!");
                _logger.LogError(e.ToString());

                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (!Connected) return;

            _logger.LogInfo("Shutting down connection...");

            // remove ui
            if (_uiPinRoot) Destroy(_uiPinRoot);
            foreach (GameObject o in _playerObjects.Values)
                if (o) Destroy(o);

            // stop threads
            Task.Run(() =>
            {
                CancelInvoke("Tick");
                _rxRunning = false;
                _rxThread.Join();
                if (_stream != null) _stream.Close();
                if (_socket != null) _socket.Close();
            });

            // success
            Connected = false;

            _logger.LogInfo("Connection shut down.");
        }

        private void Tick()
        {
            // fill empty variables and skip invalid states
            if (!_hornet) _hornet = GameObject.FindGameObjectWithTag("Player");
            if (!_hornet) return;
            if (!_hornet.name.Contains("Hero_Hornet")) _hornet = null;
            if (!_hornet) return;

            if (!_hornetSprite) _hornetSprite = _hornet.GetComponent<tk2dSprite>();
            if (!_hornetSprite) return;

            if (!_hornetRigidbody) _hornetRigidbody = _hornet.GetComponent<Rigidbody2D>();
            if (!_hornetRigidbody) return;

            UpdateUI();
            SendUpdate();
            while (_rxQueue.Count > 0) HandleUpdate(_rxQueue.Dequeue());
        }

        private void UpdateUI()
        {
            // fill empty variables and skip invalid states
            if (!_mapQuad) _mapQuad = GameObject.Find("Game Map Quad");
            if (!_mapQuad) return;
            if (!_compassIcon) _compassIcon = Resources.FindObjectsOfTypeAll<GameObject>().Where(g => g.name == "Compass Icon").FirstOrDefault();
            if (!_compassIcon) return;

            // spawn container
            if (!_uiPinRoot)
            {
                _uiPinRoot = new GameObject("SilklessPin");
                _uiPinRoot.transform.SetParent(_mapQuad.transform);

                _logger.LogInfo("UI added successfully.");
            }

            // remove pins
            while (_uiPins.Count > _playerCount)
            {
                Destroy(_uiPins[_uiPins.Count - 1]);
                _uiPins.RemoveAt(_uiPins.Count - 1);
                _logger.LogInfo($"Removed Pin {_uiPins.Count}.");
            }

            // add pins
            while (_uiPins.Count < _playerCount)
            {
                GameObject pin = Instantiate(_compassIcon, _uiPinRoot.transform);
                pin.SetActive(true);
                pin.transform.localPosition = new Vector3(-15f + 0.6f * _uiPins.Count, -8.3f, 0f);
                _uiPins.Add(pin);
                _logger.LogInfo($"Added Pin {_uiPins.Count - 1}.");
            }
        }

        private void SendUpdate()
        {
            try
            {
                // build packet
                Packet p = new Packet();
                p.sceneName = SceneManager.GetActiveScene().name;
                p.posX = _hornet.transform.position.x;
                p.posY = _hornet.transform.position.y;
                p.posZ = _hornet.transform.position.z;
                p.scaleX = _hornet.transform.localScale.x > 0 ? 1 : -1;
                p.spriteId = _hornetSprite.spriteId;
                p.vX = _hornetRigidbody.linearVelocity.x;
                p.vY = _hornetRigidbody.linearVelocity.y;

                string msg = Packet.ToString(p);
                byte[] data = Encoding.UTF8.GetBytes(msg);
                _stream.Write(data, 0, data.Length);
            } catch (Exception e)
            {
                _logger.LogError("Error while sending update!");
                _logger.LogError(e.ToString());
                Disconnect();
            }
        }

        private void HandleUpdate(string line)
        {
            try
            {
                // _logger.LogInfo($"Received update: {line}");

                // read packet
                Packet p = Packet.FromString(line);

                _playerCount = p.playerCount;

                if (p.sceneName != SceneManager.GetActiveScene().name)
                {
                    if (_playerObjects.ContainsKey(p.playerId))
                        if (_playerObjects[p.playerId])
                            Destroy(_playerObjects[p.playerId]);
                    return;
                }

                if (!_playerObjects.ContainsKey(p.playerId))
                {
                    _playerObjects.Add(p.playerId, null);
                    _playerSprites.Add(p.playerId, null);
                    _playerInterpolators.Add(p.playerId, null);
                }

                if (_playerObjects[p.playerId] != null)
                {
                    _playerObjects[p.playerId].transform.position = new Vector3(p.posX, p.posY, p.posZ + 0.0001f);
                    _playerObjects[p.playerId].transform.localScale = new Vector3(p.scaleX, 1, 1);
                    _playerSprites[p.playerId].spriteId = p.spriteId;
                    _playerInterpolators[p.playerId].velocity = new Vector3(p.vX, p.vY, 0);
                } else
                {
                    _logger.LogInfo($"Spawning new player object for player {p.playerId}.");

                    GameObject newObject = new GameObject("SilklessCooperator");
                    newObject.transform.position = new Vector3(p.posX, p.posY, p.posZ) + new Vector3(0, 0, 0.0001f);
                    newObject.transform.localScale = new Vector3(p.scaleX, 1, 1);
                    
                    tk2dSprite newSprite = tk2dSprite.AddComponent(newObject, _hornet.GetComponent<tk2dSprite>().Collection, 0);
                    newSprite.color = new Color(1, 1, 1, PlayerOpacity);

                    SimpleInterpolator newInterpolator = newObject.AddComponent<SimpleInterpolator>();
                    newInterpolator.velocity = new Vector3(p.vX, p.vY, 0);

                    _playerObjects[p.playerId] = newObject;
                    _playerSprites[p.playerId] = newSprite;
                    _playerInterpolators[p.playerId] = newInterpolator;

                    _logger.LogInfo($"Successfully spawned new player object for player {p.playerId}.");
                }
            } catch (Exception e)
            {
                _logger.LogError("Error while handling update!");
                _logger.LogError(e.ToString());
                Disconnect();
            }
        }

        private void RxThreadFunction()
        {
            try
            {
                byte[] buffer = new byte[1024];
                StringBuilder incomingData = new StringBuilder();

                while (_rxRunning)
                {
                    try
                    {
                        if (_stream == null) { break; }
                        if (_socket == null) { break; }
                        if (_socket.Client.Poll(0, SelectMode.SelectRead) && _socket.Available == 0) { _logger.LogInfo("no data"); break; }

                        if (!_stream.CanRead)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead <= 0) continue;

                        string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        incomingData.Append(chunk);

                        while (true)
                        {
                            int newlineIndex = incomingData.ToString().IndexOf('\n');
                            if (newlineIndex < 0) break;
                            string line = incomingData.ToString(0, newlineIndex).Trim();
                            incomingData.Remove(0, newlineIndex + 1);

                            _rxQueue.Enqueue(line);
                        }
                    }
                    catch (IOException e)
                    {
                        if (e.InnerException is SocketException sockEx && sockEx.SocketErrorCode == SocketError.TimedOut) {
                            continue;
                        } else {
                            _logger.LogError("Error while reading incoming data!");
                            _logger.LogError(e.ToString());

                            Disconnect();
                            break;
                        }
                    }
                }

                _logger.LogInfo("Receive thread ended.");
            }
            catch (Exception e)
            {
                _logger.LogError("Error in receive thread!");
                _logger.LogError(e.ToString());
                Disconnect();
            }
        }
    }
}
