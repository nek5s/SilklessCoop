using BepInEx.Logging;
using SilklessCoop.Connectors;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilklessCoop
{
    internal class GameSync : MonoBehaviour
    {
        public ManualLogSource Logger;
        public ModConfig Config;

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
        private GameObject _mainQuests = null;
        private GameObject _compass = null;

        // map sync - others
        private Dictionary<string, GameObject> _playerCompasses = new Dictionary<string, GameObject>();
        private Dictionary<string, tk2dSprite> _playerCompassSprites = new Dictionary<string, tk2dSprite>();

        // player count
        private HashSet<string> _playerIds = new HashSet<string>();
        private List<GameObject> _playerCountPins = new List<GameObject>();

        private void Start()
        {
            _network = GetComponent<NetworkInterface>();
            _connector = GetComponent<Connector>();

            _network.AddHandler<PacketTypes.JoinPacket>(OnJoinPacket);
            _network.AddHandler<PacketTypes.LeavePacket>(OnLeavePacket);
            _network.AddHandler<PacketTypes.HornetPositionPacket>(OnHornetPositionPacket);
            _network.AddHandler<PacketTypes.HornetAnimationPacket>(OnHornetAnimationPacket);
            _network.AddHandler<PacketTypes.CompassPositionPacket>(OnCompassPositionPacket);
        }

        private void Update()
        {
            // tick
            if (_tickTimeout >= 0)
            {
                _tickTimeout -= Time.unscaledDeltaTime;
                if (_tickTimeout <= 0)
                    { Tick(); _tickTimeout = 1.0f / Config.TickRate; }
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

            UpdateUI();
        }

        private void UpdateUI()
        {
            foreach (GameObject g in _playerCompasses.Values)
                if (g) g.SetActive(true);

            if (_compass)
            {
                while (_playerCountPins.Count < _playerIds.Count)
                {
                    GameObject newObject = Instantiate(_compass, _map.transform);
                    newObject.SetActive(true);
                    newObject.SetName("SilklessPlayerCount");
                    newObject.transform.position = new Vector3(-14.8f + 0.6f * _playerCountPins.Count, -8.2f, 0);
                    newObject.transform.localScale = new Vector3(0.6f, 0.6f, 1);

                    _playerCountPins.Add(newObject);
                }

                while (_playerCountPins.Count > _playerIds.Count)
                {
                    Destroy(_playerCountPins.Last());
                    _playerCountPins.RemoveAt(_playerCountPins.Count - 1);
                }

                for (int i = 0; i < _playerCountPins.Count; i++)
                {
                    _playerCountPins[i].transform.position = new Vector3(-14.8f + 0.6f * i, -8.2f, 0);
                    _playerCountPins[i].SetActive(_mainQuests.activeSelf);
                }
            }
        }

        private void Tick()
        {
            if (!_connector.Initialized || !_connector.Enabled || !_connector.Connected) return;

            SendHornetPositionPacket();
            SendHornetAnimationPacket();
            SendCompassPositionPacket();
        }

        public void Reset()
        {
            foreach (GameObject g in _playerObjects.Values)
                if (g) Destroy(g);

            foreach (GameObject g in _playerCompasses.Values)
                if (g) Destroy(g);

            foreach (GameObject g in _playerCountPins)
                if (g) Destroy(g);
            _playerCountPins.Clear();
        }

        private void OnJoinPacket(PacketTypes.JoinPacket packet)
        {
            _playerIds.Add(packet.id);

            if (Config.PrintDebugOutput) Logger.LogInfo($"Player {packet.id} joined ({packet.version}).");
        }
        private void OnLeavePacket(PacketTypes.LeavePacket packet)
        {
            _playerIds.Remove(packet.id);

            if (_playerObjects.TryGetValue(packet.id, out GameObject g1) && g1 != null) Destroy(g1);
            if (_playerCompasses.TryGetValue(packet.id, out GameObject g2) && g2 != null) Destroy(g2);

            if (Config.PrintDebugOutput) Logger.LogInfo($"Player {packet.id} left.");
        }

        private void SendHornetPositionPacket()
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
        private void OnHornetPositionPacket(PacketTypes.HornetPositionPacket packet)
        {
            _playerIds.Add(packet.id);

            if (!_hornetObject) return;

            if (packet.scene != SceneManager.GetActiveScene().name)
            {
                if (_playerCompasses.TryGetValue(packet.id, out GameObject playerObject) && playerObject)
                {
                    Destroy(playerObject);
                }
            } else
            {
                if (!_playerObjects.TryGetValue(packet.id, out GameObject playerObject) || !playerObject)
                {
                    // create player
                    if (Config.PrintDebugOutput) Logger.LogInfo($"Creating new player object for player {packet.id}...");

                    GameObject newObject = new GameObject();
                    newObject.SetName($"SilklessCooperator - {packet.id}");
                    newObject.transform.SetParent(transform);
                    newObject.transform.position = new Vector3(packet.posX, packet.posY, _hornetObject.transform.position.z + 0.001f);
                    newObject.transform.localScale = new Vector3(packet.scaleX, 1, 1);

                    tk2dSprite newSprite = tk2dSprite.AddComponent(newObject, _hornetSprite.Collection, _hornetSprite.spriteId);
                    newSprite.color = new Color(1, 1, 1, Config.PlayerOpacity);

                    SimpleInterpolator newInterpolator = newObject.AddComponent<SimpleInterpolator>();
                    newInterpolator.velocity = new Vector3(packet.vX, packet.vY, 0);

                    _playerObjects[packet.id] = newObject;
                    _playerSprites[packet.id] = newSprite;
                    _playerInterpolators[packet.id] = newInterpolator;

                    if (Config.PrintDebugOutput) Logger.LogInfo($"Created new player object for player {packet.id}.");
                }
                else
                {
                    if (!_playerInterpolators.TryGetValue(packet.id, out SimpleInterpolator playerInterpolator)) return;

                    // update player
                    playerObject.transform.position = new Vector3(packet.posX, packet.posY, _hornetObject.transform.position.z + 0.001f);
                    playerObject.transform.localScale = new Vector3(packet.scaleX, 1, 1);
                    playerInterpolator.velocity = new Vector3(packet.vX, packet.vY, 0);

                    if (Config.PrintDebugOutput) Logger.LogInfo($"Updated position of player {packet.id} to ({packet.posX} {packet.posY})");
                }
            }
        }

        private void SendHornetAnimationPacket()
        {
            if (!_hornetSprite) return;

            _network.SendPacket(new PacketTypes.HornetAnimationPacket
            {
                id = _id,
                collectionGuid = _hornetSprite.Collection.spriteCollectionGUID,
                spriteId = _hornetSprite.spriteId,
            });
        }
        private void OnHornetAnimationPacket(PacketTypes.HornetAnimationPacket packet)
        {
            _playerIds.Add(packet.id);

            if (!_hornetObject) return;
            if (!_playerSprites.TryGetValue(packet.id, out tk2dSprite playerSprite) || !playerSprite) return;
            if (!_collectionCache.TryGetValue(packet.collectionGuid, out tk2dSpriteCollectionData collectionData) || !collectionData) return;

            playerSprite.Collection = collectionData;
            playerSprite.spriteId = packet.spriteId;

            if (Config.PrintDebugOutput) Logger.LogInfo($"Set sprite for player {packet.id} to {packet.collectionGuid}/{packet.spriteId}");
        }
    
        private void SendCompassPositionPacket()
        {
            if (!_hornetObject || !_compass) return;

            _network.SendPacket(new PacketTypes.CompassPositionPacket
            {
                id = _id,
                active = _mainQuests.activeSelf,
                posX = _compass.transform.localPosition.x,
                posY = _compass.transform.localPosition.y,
            });
        }
        private void OnCompassPositionPacket(PacketTypes.CompassPositionPacket packet)
        {
            _playerIds.Add(packet.id);

            if (!_map || !_compass || !_mainQuests) return;
            if (!_playerCompasses.TryGetValue(packet.id, out GameObject playerCompass) || !playerCompass) {
                // create compass
                if (Config.PrintDebugOutput) Logger.LogInfo($"Creating new compass object for player {packet.id}...");

                GameObject newObject = Instantiate(_compass, _map.transform);
                newObject.SetActive(_mainQuests.activeSelf);
                newObject.SetName($"SilklessCompass - {packet.id}");
                newObject.transform.localPosition = new Vector2(packet.posX, packet.posY);

                tk2dSprite newSprite = newObject.GetComponent<tk2dSprite>();
                newSprite.color = new Color(1, 1, 1, Config.ActiveCompassOpacity);

                _playerCompasses[packet.id] = newObject;

                if (Config.PrintDebugOutput) Logger.LogInfo($"Created new player object for player {packet.id}.");
            } else
            {
                if (!_playerCompassSprites.TryGetValue(packet.id, out tk2dSprite compassSprite) || !compassSprite) return;

                // update compass
                playerCompass.transform.localPosition = new Vector2(packet.posX, packet.posY);
                compassSprite.color = new Color(1, 1, 1, packet.active ? Config.ActiveCompassOpacity : Config.InactiveCompassOpacity);

                if (Config.PrintDebugOutput) Logger.LogInfo($"Updated position of compass {packet.id} to ({packet.posX} {packet.posY})");
            }
        }
    }
}
