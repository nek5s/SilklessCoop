using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SilklessLib;
using SimpleSync.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleSync;

public class HornetPositionPacket : SilklessPacket
{
    public string Scene;
    public float PositionX;
    public float PositionY;
    public float ScaleX;
    public float VelocityX;
    public float VelocityY;
}

public class HornetAnimationPacket : SilklessPacket
{
    public string CrestName;
    public string ClipName;
}

public class CompassPositionPacket : SilklessPacket
{
    public bool Active;
    public float PosX;
    public float PosY;
}

internal class SimpleSync : MonoBehaviour
{
    private float _tickTimeout;

    // sprite sync - self
    private GameObject _hornetObject;
    private tk2dSprite _hornetSprite;
    private tk2dSpriteAnimator _hornetAnimator;
    private Rigidbody2D _hornetRigidbody;
    private readonly Dictionary<string, tk2dSpriteCollectionData> _collectionCache = new();

    // sprite sync - others
    private readonly Dictionary<string, GameObject> _playerObjects = new();
    private readonly Dictionary<string, tk2dSprite> _playerSprites = new();
    private readonly Dictionary<string, tk2dSpriteAnimator> _playerAnimators = new();
    private readonly Dictionary<string, SimpleInterpolator> _playerInterpolators = new();

    // map sync - self
    private GameObject _map;
    private GameMap _gameMap;
    private GameObject _mainQuests;
    private GameObject _compass;

    // map sync - others
    private readonly Dictionary<string, GameObject> _playerCompasses = new();

    // player count
    private readonly Dictionary<string, float> _lastSeen = new();
    private readonly Dictionary<string, GameObject> _playerCountPins = new();

    private void Awake()
    {
        if (!SilklessAPI.Init())
        {
            Destroy(this);
            LogUtil.LogError("SilklessAPI failed to initialize!");
            return;
        }

        SilklessAPI.OnDisconnect += Reset;

        new Harmony("com.silklesscoop.simplesync").PatchAll();

        SilklessAPI.AddHandler<HornetPositionPacket>(OnHornetPositionPacket);
        SilklessAPI.AddHandler<HornetAnimationPacket>(OnHornetAnimationPacket);
        SilklessAPI.AddHandler<CompassPositionPacket>(OnCompassPositionPacket);
    }

    private void OnDestroy()
    {
        SilklessAPI.RemoveHandler<HornetPositionPacket>(OnHornetPositionPacket);
        SilklessAPI.RemoveHandler<HornetAnimationPacket>(OnHornetAnimationPacket);
        SilklessAPI.RemoveHandler<CompassPositionPacket>(OnCompassPositionPacket);
    }

    private void Update()
    {
        // set up variables
        if (!_hornetObject) _hornetObject = GameObject.Find("Hero_Hornet");
        if (!_hornetObject) _hornetObject = GameObject.Find("Hero_Hornet(Clone)");
        if (_hornetObject && !_hornetRigidbody) _hornetRigidbody = _hornetObject.GetComponent<Rigidbody2D>();
        if (_hornetObject && !_hornetSprite) _hornetSprite = _hornetObject.GetComponent<tk2dSprite>();
        if (_hornetObject && !_hornetAnimator) _hornetAnimator = _hornetObject.GetComponent<tk2dSpriteAnimator>();

        if (_hornetSprite && _collectionCache.Count == 0)
            foreach (tk2dSpriteCollectionData c in Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>())
                _collectionCache[c.spriteCollectionGUID] = c;

        if (!_map) _map = GameObject.Find("Game_Map_Hornet");
        if (!_map) _map = GameObject.Find("Game_Map_Hornet(Clone)");
        if (_map && !_mainQuests) _mainQuests = _map.transform.Find("Main Quest Pins")?.gameObject;
        if (_map && !_compass) _compass = _map.transform.Find("Compass Icon")?.gameObject;
        if (_map && !_gameMap) _gameMap = _map.GetComponent<GameMap>();

        // timeout

        foreach (string id in _lastSeen.ToDictionary(e => e.Key, e => e.Value).Keys)
            if (_lastSeen[id] < Time.unscaledTime - ModConfig.ConnectionTimeout)
                RemovePlayer(id);

        if (SilklessAPI.Ready)
        {
            _lastSeen["self"] = Time.unscaledTime;

            _tickTimeout -= Time.unscaledDeltaTime;
            while (_tickTimeout <= 0)
            {
                // send packets
                SendHornetPositionPacket();
                SendCompassPositionPacket();

                _tickTimeout += 1.0f / ModConfig.TickRate;
            }
        }

        // update ui
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (!_compass || !_map || !_mainQuests) return;

        int i = 0;

        foreach (string id in _lastSeen.Keys)
        {
            if (!_playerCountPins.TryGetValue(id, out GameObject pin) || !pin)
            {
                pin = Instantiate(_compass, _map.transform);
                pin.SetActive(_mainQuests.activeSelf);
                pin.SetName("SilklessPlayerCount");
                pin.transform.position = new Vector3(-14.8f + 0.6f * (i++), -8.2f, 0);
                pin.transform.localScale = new Vector3(0.6f, 0.6f, 1);
                _playerCountPins[id] = pin;
            }

            pin.SetActive(_mainQuests.activeSelf);
            pin.transform.position = new Vector3(-14.8f + 0.6f * (i++), -8.2f, 0);
        }
    }

    private void RemovePlayer(string id)
    {
        if (_playerObjects.TryGetValue(id, out GameObject g1)) Destroy(g1);
        if (_playerCompasses.TryGetValue(id, out GameObject g2)) Destroy(g2);
        if (_playerCountPins.TryGetValue(id, out GameObject g3)) Destroy(g3);
        _lastSeen.Remove(id);

        LogUtil.LogInfo($"Removed player {id}.");
    }

    private void Reset()
    {
        foreach (string id in _lastSeen.ToDictionary(e => e.Key, e => e.Value).Keys) RemovePlayer(id);

        LogUtil.LogInfo("Reset sync artifacts.");
    }

    private void SendHornetPositionPacket()
    {
        if (!_hornetObject || !_hornetRigidbody) return;

        SilklessAPI.SendPacket(new HornetPositionPacket
        {
            Scene = SceneManager.GetActiveScene().name,
            PositionX = _hornetObject.transform.position.x,
            PositionY = _hornetObject.transform.position.y,
            ScaleX = _hornetObject.transform.localScale.x,
            VelocityX = _hornetRigidbody.linearVelocity.x * Time.timeScale,
            VelocityY = _hornetRigidbody.linearVelocity.y * Time.timeScale,
        });
    }
    private void OnHornetPositionPacket(HornetPositionPacket packet)
    {
        _lastSeen[packet.ID] = Time.unscaledTime;

        if (!_hornetObject || !_hornetAnimator) return;

        _playerObjects.TryGetValue(packet.ID, out GameObject playerObject);
        _playerSprites.TryGetValue(packet.ID, out tk2dSprite playerSprite);
        _playerAnimators.TryGetValue(packet.ID, out tk2dSpriteAnimator playerAnimator);
        _playerInterpolators.TryGetValue(packet.ID, out SimpleInterpolator playerInterpolator);

        if (!playerObject)
        {
            playerObject = new GameObject();
            playerObject.SetName($"SilklessCooperator - {packet.ID}");
            playerObject.transform.SetParent(transform);
            playerObject.transform.position = new Vector3(packet.PositionX, packet.PositionY, _hornetObject.transform.position.z + 0.001f);
            playerObject.transform.localScale = new Vector3(packet.ScaleX, 1, 1);

            _playerObjects[packet.ID] = playerObject;
            LogUtil.LogDebug($"Created new player object for player {packet.ID}.");
        }
        if (!playerSprite)
        {
            playerSprite = tk2dSprite.AddComponent(playerObject, _hornetSprite.Collection, _hornetSprite.spriteId);
            playerSprite.color = new Color(1, 1, 1, ModConfig.PlayerOpacity);

            _playerSprites[packet.ID] = playerSprite;
        }
        if (!playerAnimator)
        {
            playerAnimator = playerObject.AddComponent<tk2dSpriteAnimator>();
            playerAnimator.Library = _hornetAnimator.Library;
            playerAnimator.Play(_hornetAnimator.CurrentClip);

            _playerAnimators[packet.ID] = playerAnimator;
        }
        if (!playerInterpolator)
        {
            playerInterpolator = playerObject.AddComponent<SimpleInterpolator>();
            playerInterpolator.SetVelocity(new Vector3(packet.VelocityX, packet.VelocityY, 0));

            _playerInterpolators[packet.ID] = playerInterpolator;
        }

        playerObject.transform.position = new Vector3(packet.PositionX, packet.PositionY, _hornetObject.transform.position.z + 0.001f);
        playerObject.transform.localScale = new Vector3(packet.ScaleX, 1, 1);
        playerObject.SetActive(packet.Scene == SceneManager.GetActiveScene().name);
        playerInterpolator.SetVelocity(new Vector3(packet.VelocityX, packet.VelocityY, 0));
        LogUtil.LogDebug($"Updated position of player {packet.ID} to {packet.Scene}/({packet.PositionX} {packet.PositionY})");
    }

    private void OnHornetAnimationPacket(HornetAnimationPacket packet)
    {
        _lastSeen[packet.ID] = Time.unscaledTime;

        if (!_hornetObject) return;
        if (!_playerAnimators.TryGetValue(packet.ID, out tk2dSpriteAnimator playerAnimator) || !playerAnimator) return;
        
        tk2dSpriteAnimationClip clip = ToolItemManager.GetCrestByName(packet.CrestName)?.HeroConfig?.GetAnimationClip(packet.ClipName);
        if (clip == null) clip = playerAnimator.Library.GetClipByName(packet.ClipName);
        if (clip == null)
        {
            LogUtil.LogError($"Could not find animation clip {packet.CrestName}/{packet.ClipName}");
            return;
        }

        playerAnimator.Play(clip);
        LogUtil.LogDebug($"Started animation {clip.name} for player {packet.ID}");
    }

    private void SendCompassPositionPacket()
    {
        if (!_map || !_gameMap || !_compass) return;

        _gameMap.PositionCompassAndCorpse();

        SilklessAPI.SendPacket(new CompassPositionPacket
        {
            Active = _compass.activeSelf,
            PosX = _compass.transform.localPosition.x,
            PosY = _compass.transform.localPosition.y,
        });
    }
    private void OnCompassPositionPacket(CompassPositionPacket packet)
    {
        _lastSeen[packet.ID] = Time.unscaledTime;

        if (!_map || !_compass || !_mainQuests) return;

        if (!_playerCompasses.TryGetValue(packet.ID, out GameObject playerCompass))
        {
            playerCompass = Instantiate(_compass, _map.transform);
            playerCompass.SetName($"SilklessCompass - {packet.ID}");
            playerCompass.SetActive(packet.Active);
            playerCompass.transform.localPosition = new Vector2(packet.PosX, packet.PosY);

            tk2dSprite newSprite = playerCompass.GetComponent<tk2dSprite>();
            newSprite.color = new Color(1, 1, 1, ModConfig.CompassOpacity);
                
            _playerCompasses[packet.ID] = playerCompass;
        }

        playerCompass.SetActive(packet.Active);
        playerCompass.transform.localPosition = new Vector2(packet.PosX, packet.PosY);
    }
}

[HarmonyPatch(typeof(tk2dSpriteAnimator), "Play", typeof(tk2dSpriteAnimationClip), typeof(float), typeof(float))]
internal class HornetAnimationPatch
{
    // ReSharper disable once InconsistentNaming
    public static void Prefix(tk2dSpriteAnimationClip clip, float clipStartTime, float overrideFps, tk2dSpriteAnimator __instance)
    {
        try
        {
            string name = __instance?.gameObject.name ?? "unknown";
            if (name != "Hero_Hornet" && name != "Hero_Hornet(Clone)") return;

            string crestName = PlayerData.instance?.CurrentCrestID;
            string clipName = clip?.name;
            
            if (crestName == null || clipName == null) return;

            if (SilklessAPI.Ready)
            {
                SilklessAPI.SendPacket(new HornetAnimationPacket
                {
                    CrestName = crestName,
                    ClipName = clipName,
                });
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }
}
