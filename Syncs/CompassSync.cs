using System;
using System.Collections.Generic;
using SilklessLib;
using UnityEngine;
using UnityEngine.Serialization;

namespace SilklessCoopVisual.Syncs;

public class CompassPositionPacket : SilklessPacket
{
    public bool Active;
    public float PosX;
    public float PosY;
}

public class CompassSync : Sync
{
    // self
    public GameObject cachedMap;
    public GameMap cachedGameMap;
    public GameObject cachedMainQuests;
    public GameObject cachedCompassIcon;

    // others
    public readonly Dictionary<string, GameObject> PlayerCompasses = new();
    
    protected override void OnEnable()
    {
        SilklessAPI.AddHandler<CompassPositionPacket>(OnCompassPositionPacket);
    }

    protected override void OnDisable()
    {
        SilklessAPI.RemoveHandler<CompassPositionPacket>(OnCompassPositionPacket);
    }

    protected override void OnPlayerJoin(string id)
    {
        
    }

    protected override void OnPlayerLeave(string id)
    {
        if (PlayerCompasses.TryGetValue(id, out GameObject playerCompass) && playerCompass) Destroy(playerCompass);
    }

    protected override void Update()
    {
        try
        {
            base.Update();
        
            if (!cachedMap) cachedMap = GameObject.Find("Game_Map_Hornet");
            if (!cachedMap) cachedMap = GameObject.Find("Game_Map_Hornet(Clone)");
            if (cachedMap && !cachedMainQuests) cachedMainQuests = cachedMap.transform.Find("Main Quest Pins")?.gameObject;
            if (cachedMap && !cachedCompassIcon) cachedCompassIcon = cachedMap.transform.Find("Compass Icon")?.gameObject;
            if (cachedMap && !cachedGameMap) cachedGameMap = cachedMap.GetComponent<GameMap>();
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }

    protected override void Tick()
    {
        SendCompassPositionPacket();
    }
    
    protected override void Reset()
    {
        try
        {
            foreach (GameObject playerCompass in PlayerCompasses.Values) if(playerCompass) Destroy(playerCompass);
            PlayerCompasses.Clear();
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }
    
    // compass position
    private void SendCompassPositionPacket()
    {
        try
        {
            if (!cachedMap || !cachedGameMap || !cachedCompassIcon) return;

            cachedGameMap.PositionCompassAndCorpse();

            SilklessAPI.SendPacket(new CompassPositionPacket
            {
                Active = cachedCompassIcon.activeSelf,
                PosX = cachedCompassIcon.transform.localPosition.x,
                PosY = cachedCompassIcon.transform.localPosition.y,
            });
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }
    private void OnCompassPositionPacket(CompassPositionPacket packet)
    {
        try
        {
            if (!cachedMap || !cachedCompassIcon || !cachedMainQuests) return;

            if (!PlayerCompasses.TryGetValue(packet.ID, out GameObject playerCompass) || !playerCompass)
            {
                playerCompass = Instantiate(cachedCompassIcon, cachedMap.transform);
                playerCompass.name = $"SilklessCompass - {packet.ID}";
                playerCompass.SetActive(packet.Active);
                playerCompass.transform.localPosition = new Vector2(packet.PosX, packet.PosY);

                tk2dSprite newSprite = playerCompass.GetComponent<tk2dSprite>();
                newSprite.color = new Color(1, 1, 1, ModConfig.CompassOpacity);
                
                PlayerCompasses[packet.ID] = playerCompass;
            }
        
            playerCompass.SetActive(packet.Active);
            playerCompass.transform.localPosition = new Vector2(packet.PosX, packet.PosY);
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }
}