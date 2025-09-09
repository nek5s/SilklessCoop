using BepInEx.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilklessCoop
{
    internal class GameSync : MonoBehaviour
    {
        private static float parseFloat(string s)
        {
            s = s.Replace(",", ".");
            if (s.IndexOf('.') < 0) return float.Parse(s);

            string before = s.Split(".")[0];
            string after = s.Split(".")[1];

            float fbefore = parseFloat(before);
            float fafter = parseFloat(after) / MathF.Pow(10, after.Length);

            return fbefore + fafter;
        }

        public ManualLogSource Logger;

        private GameObject _hornetObject = null;
        private tk2dSprite _hornetSprite = null;
        private Rigidbody2D _hornetRigidbody = null;

        private Dictionary<string, GameObject> _playerObjects = new Dictionary<string, GameObject>();
        private Dictionary<string, tk2dSprite> _playerSprites = new Dictionary<string, tk2dSprite>();
        private Dictionary<string, SimpleInterpolator> _playerInterpolators = new Dictionary<string, SimpleInterpolator>();

        private bool _setup = false;

        private void Update()
        {
            if (!_hornetObject) _hornetObject = GameObject.FindGameObjectWithTag("Player");
            if (!_hornetObject) { _setup = false; return; }
            if (!_hornetObject.name.Contains("Hero_Hornet")) _hornetObject = null;
            if (!_hornetObject) { _setup = false; return; }

            if (!_hornetSprite) _hornetSprite = _hornetObject.GetComponent<tk2dSprite>();
            if (!_hornetSprite) { _setup = false; return; }

            if (!_hornetRigidbody) _hornetRigidbody = _hornetObject.GetComponent<Rigidbody2D>();
            if (!_hornetRigidbody) { _setup = false; return; }

            if (!_setup)
            {
                Logger.LogInfo("GameObject setup complete.");
                _setup = true;
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

            return $"{scene}:{posX}:{posY}:{posZ}:{spriteId}:{scaleX}:{vX}:{vY}";
        }

        public void ApplyUpdate(string data)
        {
            try
            {
                if (!_setup) return;

                Logger.LogInfo($"Applying update {data}...");

                string[] parts = data.Split("::");
                string id = parts[0];
                string[] metadataParts = parts[1].Split(":");
                string[] contentParts = parts[2].Split(":");

                string scene = contentParts[0];
                float posX = parseFloat(contentParts[1]);
                float posY = parseFloat(contentParts[2]);
                float posZ = parseFloat(contentParts[3]);
                int spriteId = int.Parse(contentParts[4]);
                float scaleX = parseFloat(contentParts[5]);
                float vX = parseFloat(contentParts[6]);
                float vY = parseFloat(contentParts[7]);

                if (scene != SceneManager.GetActiveScene().name)
                {
                    // clear dupes if player is in other scene
                    if (_playerObjects.ContainsKey(id))
                        if (_playerObjects[id] != null)
                            Destroy(_playerObjects[id]);
                    return;
                }

                if (!_playerObjects.ContainsKey(id))
                {
                    _playerObjects.Add(id, null);
                    _playerSprites.Add(id, null);
                    _playerInterpolators.Add(id, null);
                }

                if (_playerObjects[id] != null)
                {
                    _playerObjects[id].transform.position = new Vector3(posX, posY, posZ + 0.001f);
                    _playerObjects[id].transform.localScale = new Vector3(scaleX, 1, 1);
                    _playerSprites[id].spriteId = spriteId;
                    _playerInterpolators[id].velocity = new Vector3(vX, vY, 0);
                }
                else
                {
                    Logger.LogInfo($"Spawning new player object for player {id}.");

                    GameObject newObject = new GameObject("SilklessCooperator");
                    newObject.transform.position = new Vector3(posX, posY, posZ + 0.001f);
                    newObject.transform.localScale = new Vector3(scaleX, 1, 1);

                    tk2dSprite newSprite = tk2dSprite.AddComponent(newObject, _hornetSprite.Collection, 0);
                    newSprite.color = new Color(1, 1, 1, 0.7f);

                    SimpleInterpolator newInterpolator = newObject.AddComponent<SimpleInterpolator>();
                    newInterpolator.velocity = new Vector3(vX, vY, 0);

                    _playerObjects[id] = newObject;
                    _playerSprites[id] = newSprite;
                    _playerInterpolators[id] = newInterpolator;

                    Logger.LogInfo($"Successfully spawned new player object for player {id}.");
                }
            } catch (Exception e)
            {
                Logger.LogError($"Error while applying update: {e}");
            }
        }

        public void Reset()
        {
            foreach (GameObject g in _playerObjects.Values)
                if (g != null)
                    Destroy(g);

            _playerObjects.Clear();
            _playerSprites.Clear();
            _playerInterpolators.Clear();
        }
    }
}
