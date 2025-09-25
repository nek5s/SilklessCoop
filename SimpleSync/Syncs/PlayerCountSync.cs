using System;
using System.Collections.Generic;
using SilklessLib;
using UnityEngine;

namespace SimpleSync.Syncs;

public class PlayerCountSync : Sync
{
    // self
    private GameObject _map;
    private GameObject _mainQuests;
    private GameObject _compass;
    
    // others
    private readonly Stack<GameObject> _playerCountPins = new();

    protected override void OnConnect()
    {
        
    }

    protected override void OnDisconnect()
    {
        
    }

    protected override void OnPlayerJoin(string id)
    {
        
    }

    protected override void OnPlayerLeave(string id)
    {
        if (_playerCountPins.Count > 0) Destroy(_playerCountPins.Pop());
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
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }
    
    protected override void Tick()
    {
        try
        {
            if (!_compass || !_map || !_mainQuests) return;

            if (_playerCountPins.Count < SilklessAPI.PlayerIDs.Count)
            {
                GameObject pin = Instantiate(_compass, _map.transform);
                pin.SetActive(_mainQuests.activeSelf);
                pin.SetName("SilklessPlayerCount");
                pin.transform.position = new Vector3(-14.8f + 0.6f * SilklessAPI.PlayerIDs.Count, -8.2f, 0);
                pin.transform.localScale = new Vector3(0.6f, 0.6f, 1);
                _playerCountPins.Push(pin);
            }

            if (_playerCountPins.Count > SilklessAPI.PlayerIDs.Count && _playerCountPins.Count > 0)
            {
                Destroy(_playerCountPins.Pop());
            }

            int i = 0;
            foreach (GameObject pin in _playerCountPins)
            {
                pin.transform.position = new Vector3(-14.8f + 0.6f * (i++), -8.2f, 0);
                pin.SetActive(_mainQuests.activeSelf);
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }

    protected override void Reset()
    {
        
    }
}