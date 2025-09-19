using SilklessCoop.Connectors;
using SilklessCoop.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilklessCoop.Components
{
    internal class GameSync : MonoBehaviour
    {
        public int PlayerCount => _lastSeen.Count;

        private NetworkInterface _network;
        private Connector _connector;

        private float _tickTimeout;
        private string _id => _connector.GetId();

        // sprite sync - self
        private GameObject _hornetObject = null;
        private tk2dSprite _hornetSprite = null;
        private Rigidbody2D _hornetRigidbody = null;
        private Dictionary<string, tk2dSpriteCollectionData> _collectionCache = new Dictionary<string, tk2dSpriteCollectionData>();

        // sprite sync - others
        private Dictionary<string, GameObject> _playerObjects = new Dictionary<string, GameObject>();
        private Dictionary<string, tk2dSprite> _playerSprites = new Dictionary<string, tk2dSprite>();
        private Dictionary<string, SimpleInterpolator> _playerInterpolators = new Dictionary<string, SimpleInterpolator>();

        // map sync - self
        private GameObject _map = null;
        private GameMap _gameMap = null;
        private GameObject _mainQuests = null;
        private GameObject _compass = null;

        // map sync - others
        private Dictionary<string, GameObject> _playerCompasses = new Dictionary<string, GameObject>();
        private Dictionary<string, tk2dSprite> _playerCompassSprites = new Dictionary<string, tk2dSprite>();

        // player count
        private Dictionary<string, float> _lastSeen = new Dictionary<string, float>();
        private Dictionary<string, GameObject> _playerCountPins = new Dictionary<string, GameObject>();

        private void Start()
        {
            _network = GetComponent<NetworkInterface>();
            _connector = GetComponent<Connector>();

            _network.AddHandler<PacketTypes.HornetPositionPacket>(OnHornetPositionPacket);
            _network.AddHandler<PacketTypes.HornetAnimationPacket>(OnHornetAnimationPacket);
            _network.AddHandler<PacketTypes.CompassPositionPacket>(OnCompassPositionPacket);
        }

        private void Update()
        {
            try
            {
                // tick
                if (_tickTimeout >= 0)
                {
                    _tickTimeout -= Time.unscaledDeltaTime;
                    if (_tickTimeout <= 0)
                    { Tick(); _tickTimeout = 1.0f / ModConfig.TickRate; }
                }

                // setup references
                if (!_hornetObject) _hornetObject = GameObject.Find("Hero_Hornet");
                if (!_hornetObject) _hornetObject = GameObject.Find("Hero_Hornet(Clone)");
                if (_hornetObject && !_hornetRigidbody) _hornetRigidbody = _hornetObject.GetComponent<Rigidbody2D>();
                if (_hornetObject && !_hornetSprite) _hornetSprite = _hornetObject.GetComponent<tk2dSprite>();

                if (_hornetSprite && _collectionCache.Count == 0)
                    foreach (tk2dSpriteCollectionData c in Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>())
                        _collectionCache[c.spriteCollectionGUID] = c;

                if (!_map) _map = GameObject.Find("Game_Map_Hornet");
                if (!_map) _map = GameObject.Find("Game_Map_Hornet(Clone)");
                if (_map && !_mainQuests) _mainQuests = _map.transform.Find("Main Quest Pins")?.gameObject;
                if (_map && !_compass) _compass = _map.transform.Find("Compass Icon")?.gameObject;
                if (_map && !_gameMap) _gameMap = _map.GetComponent<GameMap>();
            } catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        private void UpdateUI()
        {
            try
            {
                if (_compass)
                {
                    int i = 0;

                    foreach (string id in _lastSeen.Keys)
                    {
                        if (!_playerCountPins.TryGetValue(id, out GameObject pin) || !pin)
                        {
                            GameObject newObject = Instantiate(_compass, _map.transform);
                            newObject.SetActive(_mainQuests.activeSelf);
                            newObject.SetName("SilklessPlayerCount");
                            newObject.transform.position = new Vector3(-14.8f + 0.6f * (i++), -8.2f, 0);
                            newObject.transform.localScale = new Vector3(0.6f, 0.6f, 1);
                            _playerCountPins[id] = newObject;
                            continue;
                        }

                        pin.SetActive(_mainQuests.activeSelf);
                        pin.transform.position = new Vector3(-14.8f + 0.6f * (i++), -8.2f, 0);
                    }
                }
            } catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        private void Tick()
        {
            try
            {
                if (!_connector.Initialized || !_connector.Enabled || !_connector.Connected) return;

                // timeouts
                _lastSeen["self"] = Time.unscaledTime;
                foreach (string id in _lastSeen.ToDictionary(e => e.Key, e => e.Value).Keys)
                    if (_lastSeen[id] < Time.unscaledTime - ModConfig.ConnectionTimeout)
                        RemovePlayer(id);

                SendHornetPositionPacket();
                SendHornetAnimationPacket();
                if (ModConfig.SyncCompasses) SendCompassPositionPacket();

                UpdateUI();
            } catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        public void RemovePlayer(string id)
        {
            try
            {
                LogUtil.LogInfo($"Removing player {id}...");

                if (_playerObjects.TryGetValue(id, out GameObject g1)) Destroy(g1);
                if (_playerCompasses.TryGetValue(id, out GameObject g2)) Destroy(g2);
                if (_playerCountPins.TryGetValue(id, out GameObject g3)) Destroy(g3);

                _lastSeen.Remove(id);

                LogUtil.LogInfo($"Player {id} removed.", true);
            } catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        public void Reset()
        {
            try
            {
                LogUtil.LogInfo("Resetting sync artifacts...");

                foreach (GameObject g in _playerObjects.Values) Destroy(g);
                _playerObjects.Clear();

                foreach (GameObject g in _playerCompasses.Values) Destroy(g);
                _playerCompasses.Clear();

                foreach (GameObject g in _playerCountPins.Values) Destroy(g);
                _playerCountPins.Clear();

                _lastSeen.Clear();

                LogUtil.LogInfo("Successfully reset sync artifacts.", true);
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        private void SendHornetPositionPacket()
        {
            try
            {
                if (!_hornetObject || !_hornetRigidbody) return;
            
                _network.SendPacket(new PacketTypes.HornetPositionPacket
                {
                    id = _id,
                    scene = SceneManager.GetActiveScene().name,
                    posX = _hornetObject.transform.position.x,
                    posY = _hornetObject.transform.position.y,
                    scaleX = _hornetObject.transform.localScale.x,
                    vX = _hornetRigidbody.linearVelocity.x * Time.timeScale,
                    vY = _hornetRigidbody.linearVelocity.y * Time.timeScale,
                });
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }
        private void OnHornetPositionPacket(PacketTypes.HornetPositionPacket packet)
        {
            try
            {
                _lastSeen[packet.id] = Time.unscaledTime;

                if (!_hornetObject) return;

                if (!_playerObjects.TryGetValue(packet.id, out GameObject playerObject) || !playerObject)
                {
                    // create player
                    LogUtil.LogDebug($"Creating new player object for player {packet.id}...");

                    GameObject newObject = new GameObject();
                    newObject.SetName($"SilklessCooperator - {packet.id}");
                    newObject.transform.SetParent(transform);
                    newObject.transform.position = new Vector3(packet.posX, packet.posY, _hornetObject.transform.position.z + 0.001f);
                    newObject.transform.localScale = new Vector3(packet.scaleX, 1, 1);

                    tk2dSprite newSprite = tk2dSprite.AddComponent(newObject, _hornetSprite.Collection, _hornetSprite.spriteId);
                    newSprite.color = new Color(1, 1, 1, ModConfig.PlayerOpacity);

                    SimpleInterpolator newInterpolator = newObject.AddComponent<SimpleInterpolator>();
                    newInterpolator.velocity = new Vector3(packet.vX, packet.vY, 0);

                    _playerObjects[packet.id] = newObject;
                    _playerSprites[packet.id] = newSprite;
                    _playerInterpolators[packet.id] = newInterpolator;
                
                    LogUtil.LogDebug($"Created new player object for player {packet.id}.");
                }
                else
                {
                    if (!_playerInterpolators.TryGetValue(packet.id, out SimpleInterpolator playerInterpolator)) return;

                    // update player
                    playerObject.transform.position = new Vector3(packet.posX, packet.posY, _hornetObject.transform.position.z + 0.001f);
                    playerObject.transform.localScale = new Vector3(packet.scaleX, 1, 1);
                    playerObject.SetActive(packet.scene == SceneManager.GetActiveScene().name);
                    playerInterpolator.velocity = new Vector3(packet.vX, packet.vY, 0);

                    LogUtil.LogDebug($"Updated position of player {packet.id} to ({packet.posX} {packet.posY})");
                }
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }

        private void SendHornetAnimationPacket()
        {
            try
            {
                if (!_hornetSprite) return;

                _network.SendPacket(new PacketTypes.HornetAnimationPacket
                {
                    id = _id,
                    collectionGuid = _hornetSprite.Collection.spriteCollectionGUID,
                    spriteId = _hornetSprite.spriteId,
                });
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }
        private void OnHornetAnimationPacket(PacketTypes.HornetAnimationPacket packet)
        {
            try
            {
                _lastSeen[packet.id] = Time.unscaledTime;

                if (!_hornetObject) return;
                if (!_playerSprites.TryGetValue(packet.id, out tk2dSprite playerSprite) || !playerSprite) return;
                if (!_collectionCache.TryGetValue(packet.collectionGuid, out tk2dSpriteCollectionData collectionData) || !collectionData) return;

                playerSprite.Collection = collectionData;
                playerSprite.spriteId = packet.spriteId;

                LogUtil.LogDebug($"Set sprite for player {packet.id} to {packet.collectionGuid}/{packet.spriteId}");
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }
    
        private void SendCompassPositionPacket()
        {
            try
            {
                if (!_map || !_gameMap || !_compass) return;

                _gameMap.PositionCompassAndCorpse();

                _network.SendPacket(new PacketTypes.CompassPositionPacket
                {
                    id = _id,
                    active = _compass.activeSelf,
                    posX = _compass.transform.localPosition.x,
                    posY = _compass.transform.localPosition.y,
                });
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }
        private void OnCompassPositionPacket(PacketTypes.CompassPositionPacket packet)
        {
            try
            {
                _lastSeen[packet.id] = Time.unscaledTime;

                if (!_map || !_compass || !_mainQuests) return;

                if (!_playerCompasses.TryGetValue(packet.id, out GameObject playerCompass) || !playerCompass) {
                    // create compass
                    LogUtil.LogDebug($"Creating new compass object for player {packet.id}...");

                    GameObject newObject = Instantiate(_compass, _map.transform);
                    newObject.SetActive(packet.active);
                    newObject.SetName($"SilklessCompass - {packet.id}");
                    newObject.transform.localPosition = new Vector2(packet.posX, packet.posY);

                    tk2dSprite newSprite = newObject.GetComponent<tk2dSprite>();
                    newSprite.color = new Color(1, 1, 1, ModConfig.CompassOpacity);

                    _playerCompasses[packet.id] = newObject;
                    _playerCompassSprites[packet.id] = newSprite;

                    LogUtil.LogDebug($"Created new player object for player {packet.id}.");
                } else
                {
                    if (!_playerCompassSprites.TryGetValue(packet.id, out tk2dSprite compassSprite) || !compassSprite) return;

                    // update compass
                    playerCompass.transform.localPosition = new Vector2(packet.posX, packet.posY);
                    playerCompass.SetActive(packet.active);

                    LogUtil.LogDebug($"Updated position of compass {packet.id} to ({packet.posX} {packet.posY}) active={packet.active}");
                }
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.ToString());
            }
        }
    }
}
