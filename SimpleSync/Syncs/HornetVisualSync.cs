using System;
using System.Collections.Generic;
using HarmonyLib;
using SilklessLib;
using SimpleSync.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleSync.Syncs;

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

public class HornetVisualSync : Sync
{
    // self
    private GameObject _hornetObject;
    private tk2dSprite _hornetSprite;
    private tk2dSpriteAnimator _hornetAnimator;
    private Rigidbody2D _hornetRigidbody;
    private readonly Dictionary<string, tk2dSpriteCollectionData> _collectionCache = new();
    
    // others
    private readonly Dictionary<string, GameObject> _playerObjects = new();
    private readonly Dictionary<string, tk2dSprite> _playerSprites = new();
    private readonly Dictionary<string, tk2dSpriteAnimator> _playerAnimators = new();
    private readonly Dictionary<string, SimpleInterpolator> _playerInterpolators = new();
    
    protected override void OnConnect()
    {
        SilklessAPI.AddHandler<HornetPositionPacket>(OnHornetPositionPacket);
        SilklessAPI.AddHandler<HornetAnimationPacket>(OnHornetAnimationPacket);
        
        new Harmony("com.silklesscoop.simplesync").PatchAll();
    }

    protected override void OnDisconnect()
    {
        SilklessAPI.RemoveHandler<HornetPositionPacket>(OnHornetPositionPacket);
        SilklessAPI.RemoveHandler<HornetAnimationPacket>(OnHornetAnimationPacket);
    }

    protected override void OnPlayerJoin(string id)
    {
        
    }

    protected override void OnPlayerLeave(string id)
    {
        if (_playerObjects.TryGetValue(id, out GameObject playerObject) && playerObject)
            Destroy(playerObject);
    }
    
    protected override void Update()
    {
        try
        {
            base.Update();
        
            if (!_hornetObject) _hornetObject = GameObject.Find("Hero_Hornet");
            if (!_hornetObject) _hornetObject = GameObject.Find("Hero_Hornet(Clone)");
            if (_hornetObject && !_hornetRigidbody) _hornetRigidbody = _hornetObject.GetComponent<Rigidbody2D>();
            if (_hornetObject && !_hornetSprite) _hornetSprite = _hornetObject.GetComponent<tk2dSprite>();
            if (_hornetObject && !_hornetAnimator) _hornetAnimator = _hornetObject.GetComponent<tk2dSpriteAnimator>();

            if (_hornetSprite && _collectionCache.Count == 0)
                foreach (tk2dSpriteCollectionData c in Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>())
                    _collectionCache[c.spriteCollectionGUID] = c;
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }

    protected override void Tick()
    {
        SendHornetPositionPacket();
    }
    
    protected override void Reset()
    {
        try
        {
            foreach (GameObject playerObject in _playerObjects.Values) if (playerObject) Destroy(playerObject);
            _playerObjects.Clear();
            _playerSprites.Clear();
            _playerAnimators.Clear();
            _playerInterpolators.Clear();
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }

    // position
    private void SendHornetPositionPacket()
    {
        try
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
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }
    private void OnHornetPositionPacket(HornetPositionPacket packet)
    {
        try
        {
            if (!_hornetObject || !_hornetAnimator) return;

            _playerObjects.TryGetValue(packet.ID, out GameObject playerObject);
            _playerSprites.TryGetValue(packet.ID, out tk2dSprite playerSprite);
            _playerAnimators.TryGetValue(packet.ID, out tk2dSpriteAnimator playerAnimator);
            _playerInterpolators.TryGetValue(packet.ID, out SimpleInterpolator playerInterpolator);

            if (!playerObject || !playerSprite || !playerAnimator || !playerInterpolator)
            {
                LogUtil.LogDebug($"Creating new player object for player {packet.ID}");
                
                playerObject = new GameObject();
                playerObject.name = $"SilklessCooperator - {packet.ID}";
                playerObject.transform.SetParent(transform);
                playerObject.transform.position = new Vector3(packet.PositionX, packet.PositionY, _hornetObject.transform.position.z + 0.001f);
                playerObject.transform.localScale = new Vector3(packet.ScaleX, 1, 1);
            
                playerSprite = tk2dSprite.AddComponent(playerObject, _hornetSprite.Collection, _hornetSprite.spriteId);
                playerSprite.color = new Color(1, 1, 1, ModConfig.PlayerOpacity);

                playerAnimator = playerObject.AddComponent<tk2dSpriteAnimator>();
                playerAnimator.Library = _hornetAnimator.Library;
                playerAnimator.Play(_hornetAnimator.CurrentClip);

                playerInterpolator = playerObject.AddComponent<SimpleInterpolator>();
                playerInterpolator.SetVelocity(new Vector3(packet.VelocityX, packet.VelocityY, 0));
            
                LogUtil.LogDebug($"Created new player object for player {packet.ID}.");

                _playerObjects[packet.ID] = playerObject;
                _playerSprites[packet.ID] = playerSprite;
                _playerAnimators[packet.ID] = playerAnimator;
                _playerInterpolators[packet.ID] = playerInterpolator;
            }

            playerObject.transform.position = new Vector3(packet.PositionX, packet.PositionY, _hornetObject.transform.position.z + 0.001f);
            playerObject.transform.localScale = new Vector3(packet.ScaleX, 1, 1);
            playerObject.SetActive(packet.Scene == SceneManager.GetActiveScene().name);
            playerInterpolator.SetVelocity(new Vector3(packet.VelocityX, packet.VelocityY, 0));
            LogUtil.LogDebug($"Updated position of player {packet.ID} to {packet.Scene}/({packet.PositionX} {packet.PositionY})");
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }
    
    // animation
    private void OnHornetAnimationPacket(HornetAnimationPacket packet)
    {
        try
        {
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
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
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