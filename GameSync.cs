using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static TeamCherry.DebugMenu.DebugMenu;

namespace SilklessCoop
{
    internal class GameSync : MonoBehaviour
    {
        private static float parseFloat(string s)
        {
            float f1 = float.Parse(s.Replace(',', '.'));
            float f2 = float.Parse(s.Replace('.', '.'));

            if (Mathf.Abs(f1) < Mathf.Abs(f2)) return f1;
            else return f2;
        }

        public ManualLogSource Logger;
        public ModConfig Config;

        // sprite sync - self
        private GameObject _hornetObject = null;
        private tk2dSprite _hornetSprite = null;
        private Rigidbody2D _hornetRigidbody = null;

        // sprite sync - others
        private Dictionary<string, GameObject> _playerObjects = new Dictionary<string, GameObject>();
        private Dictionary<string, tk2dSprite> _playerSprites = new Dictionary<string, tk2dSprite>();
        private Dictionary<string, SimpleInterpolator> _playerInterpolators = new Dictionary<string, SimpleInterpolator>();

        // player count
        private GameObject _pauseMenu = null;
        private int _playerCount = 0;
        private List<GameObject> _countPins = new List<GameObject>();

        // map sync - self
        private GameObject _mainQuests = null;
        private GameObject _map = null;
        private GameObject _compass = null;

        // map sync - others
        private Dictionary<string, GameObject> _playerCompasses = new Dictionary<string, GameObject>();
        private Dictionary<string, tk2dSprite> _playerCompassSprites = new Dictionary<string, tk2dSprite>();

        private bool _setup = false;

        private void Update()
        {
            if (!_hornetObject) _hornetObject = GameObject.Find("Hero_Hornet");
            if (!_hornetObject) _hornetObject = GameObject.Find("Hero_Hornet(Clone)");
            if (!_hornetObject) { _setup = false; return; }

            if (!_hornetSprite) _hornetSprite = _hornetObject.GetComponent<tk2dSprite>();
            if (!_hornetSprite) { _setup = false; return; }

            if (!_hornetRigidbody) _hornetRigidbody = _hornetObject.GetComponent<Rigidbody2D>();
            if (!_hornetRigidbody) { _setup = false; return; }

            if (!_map) _map = GameObject.Find("Game_Map_Hornet");
            if (!_map) _map = GameObject.Find("Game_Map_Hornet(Clone)");
            if (!_map) { _setup = false; return; }

            if (!_compass) _compass = _map.transform.Find("Compass Icon")?.gameObject;
            if (!_compass) { _setup = false; return; }

            if (!_pauseMenu) _pauseMenu = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == "NewPauseMenuScreen");
            if (!_pauseMenu) { _setup = false; return; }

            if (!_mainQuests) _mainQuests = _map.transform.Find("Main Quest Pins")?.gameObject;
            if (!_mainQuests) { _setup = false; return; }

            if (Config.SyncCompasses)
            {
                foreach (GameObject g in _playerCompasses.Values)
                    if (g != null) g.SetActive(_mainQuests.activeSelf);
            }

            foreach (GameObject g in _countPins)
                if (g != null) g.SetActive(_mainQuests.activeSelf);

            if (!_setup)
            {
                _setup = true;

                Logger.LogInfo("GameObject setup complete.");
            }
        }

        public string GetUpdateContent()
        {
            if (!_setup) return null;

            string scene = SceneManager.GetActiveScene().name;
            float posX = _hornetObject.transform.position.x;
            float posY = _hornetObject.transform.position.y;
            float posZ = _hornetObject.transform.position.z;
            int spriteId = _hornetSprite.spriteId;
            float scaleX = _hornetObject.transform.localScale.x;
            float vX = _hornetRigidbody.linearVelocity.x;
            float vY = _hornetRigidbody.linearVelocity.y;

            int compassActive = 0;
            float compassX = 0;
            float compassY = 0;

            if (Config.SyncCompasses)
            {
                compassActive = _compass.activeSelf ? 1 : 0;
                compassX = _compass.transform.localPosition.x;
                compassY = _compass.transform.localPosition.y;
            }

            string baseData = $"{scene}:{posX}:{posY}:{posZ}:{spriteId}:{scaleX}:{vX}:{vY}";
            string compassData = Config.SyncCompasses ? $":{compassActive}:{compassX}:{compassY}" : "";
            string data = $"{baseData}{compassData}";

            return data;
        }

        public void ApplyUpdate(string data)
        {
            try
            {
                if (!_setup) return;

                if (Config.PrintDebugOutput) Logger.LogInfo($"Applying update {data}...");

                UpdateUI();

                string[] parts = data.Split("::");
                string id = parts[0];
                string[] metadataParts = parts[1].Split(":");
                string[] contentParts = parts[2].Split(":");

                _playerCount = int.Parse(metadataParts[0]);

                string scene = contentParts[0];
                float posX = parseFloat(contentParts[1]);
                float posY = parseFloat(contentParts[2]);
                float posZ = parseFloat(contentParts[3]);
                int spriteId = int.Parse(contentParts[4]);
                float scaleX = parseFloat(contentParts[5]);
                float vX = parseFloat(contentParts[6]);
                float vY = parseFloat(contentParts[7]);

                bool compassActive = false;
                float compassX = 0;
                float compassY = 0;

                if (Config.SyncCompasses && contentParts.Length > 8)
                {
                    compassActive = contentParts[8] == "1";
                    compassX = parseFloat(contentParts[9]);
                    compassY = parseFloat(contentParts[10]);
                }

                bool sameScene = scene == SceneManager.GetActiveScene().name;

                if (!_playerObjects.ContainsKey(id))
                {
                    _playerObjects.Add(id, null);
                    _playerSprites.Add(id, null);
                    _playerInterpolators.Add(id, null);
                }

                if (!_playerCompasses.ContainsKey(id))
                {
                    _playerCompasses.Add(id, null);
                    if (!_playerCompassSprites.ContainsKey(id)) _playerCompassSprites.Add(id, null);
                }

                if (!sameScene)
                {
                    // clear dupes if player leaves scene
                    if (_playerObjects.ContainsKey(id))
                        if (_playerObjects[id] != null)
                            Destroy(_playerObjects[id]);
                } else
                {
                    if (_playerObjects[id] != null)
                    {
                        // update player
                        _playerObjects[id].transform.position = new Vector3(posX, posY, posZ + 0.001f);
                        _playerObjects[id].transform.localScale = new Vector3(scaleX, 1, 1);
                        _playerSprites[id].spriteId = spriteId;
                        _playerInterpolators[id].velocity = new Vector3(vX, vY, 0);
                    }
                    else
                    {
                        // create player
                        if (Config.PrintDebugOutput) Logger.LogInfo($"Creating new player object for player {id}...");

                        GameObject newObject = new GameObject();
                        newObject.SetName("SilklessCooperator");
                        newObject.transform.position = new Vector3(posX, posY, posZ + 0.001f);
                        newObject.transform.localScale = new Vector3(scaleX, 1, 1);

                        tk2dSprite newSprite = tk2dSprite.AddComponent(newObject, _hornetSprite.Collection, 0);
                        newSprite.color = new Color(1, 1, 1, Config.PlayerOpacity);

                        SimpleInterpolator newInterpolator = newObject.AddComponent<SimpleInterpolator>();
                        newInterpolator.velocity = new Vector3(vX, vY, 0);

                        _playerObjects[id] = newObject;
                        _playerSprites[id] = newSprite;
                        _playerInterpolators[id] = newInterpolator;

                        if (Config.PrintDebugOutput) Logger.LogInfo($"Successfully created new player object for player {id}.");
                    }
                }
                
                if (Config.SyncCompasses)
                {
                    if (compassActive)
                    {
                        if (_playerCompasses[id] != null)
                        {
                            // update compass
                            _playerCompasses[id].transform.localPosition = new Vector3(compassX, compassY, _compass.transform.localPosition.z + 0.001f);
                            _playerCompassSprites[id].color = new Color(1, 1, 1, Config.ActiveCompassOpacity);
                        }
                        else
                        {
                            // create compass
                            if (Config.PrintDebugOutput) Logger.LogInfo($"Creating new compass for player {id}...");

                            GameObject newObject = Instantiate(_compass, _map.transform);
                            newObject.SetName("SilklessCompass");
                            newObject.transform.localPosition = new Vector3(compassX, compassY, _compass.transform.localPosition.z + 0.001f);
                            tk2dSprite newSprite = newObject.GetComponent<tk2dSprite>();
                            newSprite.color = new Color(1, 1, 1, Config.ActiveCompassOpacity);

                            _playerCompasses[id] = newObject;
                            _playerCompassSprites[id] = newSprite;

                            if (Config.PrintDebugOutput) Logger.LogInfo($"Successfully created new compass for player {id}.");
                        }
                    }
                    else
                    {
                        if (_playerCompasses[id] != null)
                            _playerCompassSprites[id].color = new Color(1, 1, 1, Config.InactiveCompassOpacity);
                    }
                }
            } catch (Exception e)
            {
                Logger.LogError($"Error while applying update: {e}");
            }
        }

        private void UpdateUI()
        {
            try
            {
                while (_countPins.Count < _playerCount)
                {
                    if (Config.PrintDebugOutput) Logger.LogInfo($"Creating player count pin {_countPins.Count + 1}...");

                    GameObject newPin = Instantiate(_compass, _map.transform);
                    newPin.SetName("SilklessPlayerCountPin");
                    _countPins.Add(newPin);

                    if (Config.PrintDebugOutput) Logger.LogInfo($"Successfully created player count pin {_countPins.Count}.");
                }

                while (_countPins.Count > _playerCount)
                {
                    if (Config.PrintDebugOutput) Logger.LogInfo($"Removing player count pin {_countPins.Count}...");

                    Destroy(_countPins[_countPins.Count - 1]);
                    _countPins.RemoveAt(_countPins.Count - 1);

                    if (Config.PrintDebugOutput) Logger.LogInfo($"Successfully removed player count pin {_countPins.Count + 1}.");
                }

                for (int i = 0; i < _countPins.Count; i++)
                    _countPins[i].transform.position = new Vector3(-14.8f + i * 0.9f, -8.2f, -5f);
            }
            catch (Exception e)
            {
                Logger.LogError($"Error while updating ui: {e}");
            }
        }

        public void Reset()
        {
            foreach (GameObject g in _playerObjects.Values)
                if (g != null) Destroy(g);
            _playerObjects.Clear();
            _playerSprites.Clear();
            _playerInterpolators.Clear();

            foreach (GameObject g in _countPins)
                if (g != null) Destroy(g);
            _countPins.Clear();

            foreach (GameObject g in _playerCompasses.Values)
                if (g != null) Destroy(g);
            _playerCompasses.Clear();
        }
    }
}
