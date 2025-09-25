using System;
using System.Collections.Generic;
using SilklessLib;
using UnityEngine;

namespace SimpleSync.Syncs;

public class CompassPositionPacket : SilklessPacket
{
    public bool Active;
    public float PosX;
    public float PosY;
}

public class CompassSync : Sync
{
    // self
    private GameObject _map;
    private GameMap _gameMap;
    private GameObject _mainQuests;
    private GameObject _compass;

    // others
    private readonly Dictionary<string, GameObject> _playerCompasses = new();
    
    protected override void OnConnect()
    {
        SilklessAPI.AddHandler<CompassPositionPacket>(OnCompassPositionPacket);
    }

    protected override void OnDisconnect()
    {
        SilklessAPI.RemoveHandler<CompassPositionPacket>(OnCompassPositionPacket);
    }

    protected override void OnPlayerJoin(string id)
    {
        
    }

    protected override void OnPlayerLeave(string id)
    {
        if (_playerCompasses.TryGetValue(id, out GameObject playerCompass) && playerCompass) Destroy(playerCompass);
    }

    protected override void Update()
    {
        try
        {
            base.Update();
        
            if (!_map) _map = GameObject.Find("Game_Map_Hornet");
            if (!_map) _map = GameObject.Find("Game_Map_Hornet(Clone)");
            if (_map && !_mainQuests) _mainQuests = _map.transform.Find("Main Quest Pins")?.gameObject;
            if (_map && !_compass) _compass = _map.transform.Find("Compass Icon")?.gameObject;
            if (_map && !_gameMap) _gameMap = _map.GetComponent<GameMap>();
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
            foreach (GameObject playerCompass in _playerCompasses.Values) if(playerCompass) Destroy(playerCompass);
            _playerCompasses.Clear();
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
            if (!_map || !_gameMap || !_compass) return;

            _gameMap.PositionCompassAndCorpse();

            SilklessAPI.SendPacket(new CompassPositionPacket
            {
                Active = _compass.activeSelf,
                PosX = _compass.transform.localPosition.x,
                PosY = _compass.transform.localPosition.y,
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
            if (!_map || !_compass || !_mainQuests) return;

            if (!_playerCompasses.TryGetValue(packet.ID, out GameObject playerCompass) || !playerCompass)
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
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }
}