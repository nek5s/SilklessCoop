using System;
using System.Collections.Generic;
using HarmonyLib;
using SilklessCoopVisual.Components;
using SilklessLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace SilklessCoopVisual.Syncs;

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
    public GameObject cachedHornetObject;
    public tk2dSprite cachedHornetSprite;
    public tk2dSpriteAnimator cachedHornetAnimator;
    public Rigidbody2D cachedHornetRigidbody;
    public readonly Dictionary<string, tk2dSpriteCollectionData> CachedCollections = new();
    
    // others
    public readonly Dictionary<string, GameObject> PlayerObjects = new();
    public readonly Dictionary<string, tk2dSprite> PlayerSprites = new();
    public readonly Dictionary<string, tk2dSpriteAnimator> PlayerAnimators = new();
    public readonly Dictionary<string, SimpleInterpolator> PlayerInterpolators = new();
    
    protected override void OnEnable()
    {
        SilklessAPI.AddHandler<HornetPositionPacket>(OnHornetPositionPacket);
        SilklessAPI.AddHandler<HornetAnimationPacket>(OnHornetAnimationPacket);
    }

    protected override void OnDisable()
    {
        SilklessAPI.RemoveHandler<HornetPositionPacket>(OnHornetPositionPacket);
        SilklessAPI.RemoveHandler<HornetAnimationPacket>(OnHornetAnimationPacket);
    }

    protected override void OnPlayerJoin(string id)
    {
        
    }

    protected override void OnPlayerLeave(string id)
    {
        if (PlayerObjects.TryGetValue(id, out GameObject playerObject) && playerObject)
            Destroy(playerObject);
    }
    
    protected override void Update()
    {
        try
        {
            base.Update();
        
            if (!cachedHornetObject) cachedHornetObject = GameObject.Find("Hero_Hornet");
            if (!cachedHornetObject) cachedHornetObject = GameObject.Find("Hero_Hornet(Clone)");
            if (cachedHornetObject && !cachedHornetRigidbody) cachedHornetRigidbody = cachedHornetObject.GetComponent<Rigidbody2D>();
            if (cachedHornetObject && !cachedHornetSprite) cachedHornetSprite = cachedHornetObject.GetComponent<tk2dSprite>();
            if (cachedHornetObject && !cachedHornetAnimator) cachedHornetAnimator = cachedHornetObject.GetComponent<tk2dSpriteAnimator>();

            if (cachedHornetSprite && CachedCollections.Count == 0)
                foreach (tk2dSpriteCollectionData c in Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>())
                    CachedCollections[c.spriteCollectionGUID] = c;
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
            foreach (GameObject playerObject in PlayerObjects.Values) if (playerObject) Destroy(playerObject);
            PlayerObjects.Clear();
            PlayerSprites.Clear();
            PlayerAnimators.Clear();
            PlayerInterpolators.Clear();
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
            if (!cachedHornetObject || !cachedHornetRigidbody) return;

            SilklessAPI.SendPacket(new HornetPositionPacket
            {
                Scene = SceneManager.GetActiveScene().name,
                PositionX = cachedHornetObject.transform.position.x,
                PositionY = cachedHornetObject.transform.position.y,
                ScaleX = cachedHornetObject.transform.localScale.x,
                VelocityX = cachedHornetRigidbody.linearVelocity.x * Time.timeScale,
                VelocityY = cachedHornetRigidbody.linearVelocity.y * Time.timeScale,
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
            if (!cachedHornetObject || !cachedHornetAnimator) return;

            PlayerObjects.TryGetValue(packet.ID, out GameObject playerObject);
            PlayerSprites.TryGetValue(packet.ID, out tk2dSprite playerSprite);
            PlayerAnimators.TryGetValue(packet.ID, out tk2dSpriteAnimator playerAnimator);
            PlayerInterpolators.TryGetValue(packet.ID, out SimpleInterpolator playerInterpolator);

            if (!playerObject || !playerSprite || !playerAnimator || !playerInterpolator)
            {
                LogUtil.LogDebug($"Creating new player object for player {packet.ID}");
                
                playerObject = new GameObject();
                playerObject.name = $"SilklessCooperator - {packet.ID}";
                playerObject.transform.SetParent(transform);
                playerObject.transform.position = new Vector3(packet.PositionX, packet.PositionY, cachedHornetObject.transform.position.z + 0.001f);
                playerObject.transform.localScale = new Vector3(packet.ScaleX, 1, 1);
            
                playerSprite = tk2dSprite.AddComponent(playerObject, cachedHornetSprite.Collection, cachedHornetSprite.spriteId);
                playerSprite.color = new Color(1, 1, 1, ModConfig.PlayerOpacity);

                playerAnimator = playerObject.AddComponent<tk2dSpriteAnimator>();
                playerAnimator.Library = cachedHornetAnimator.Library;
                playerAnimator.Play(cachedHornetAnimator.CurrentClip);

                playerInterpolator = playerObject.AddComponent<SimpleInterpolator>();
                playerInterpolator.SetVelocity(new Vector3(packet.VelocityX, packet.VelocityY, 0));
            
                LogUtil.LogDebug($"Created new player object for player {packet.ID}.");

                PlayerObjects[packet.ID] = playerObject;
                PlayerSprites[packet.ID] = playerSprite;
                PlayerAnimators[packet.ID] = playerAnimator;
                PlayerInterpolators[packet.ID] = playerInterpolator;
            }

            playerObject.transform.position = new Vector3(packet.PositionX, packet.PositionY, cachedHornetObject.transform.position.z + 0.001f);
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
            if (!cachedHornetObject) return;
            if (!PlayerAnimators.TryGetValue(packet.ID, out tk2dSpriteAnimator playerAnimator) || !playerAnimator) return;
        
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
public class HornetAnimationPatch
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