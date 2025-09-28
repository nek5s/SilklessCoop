using System;
using System.Collections.Generic;
using SilklessLib;
using UnityEngine;

namespace SilklessCoopVisual.Syncs;

public class PlayerCountSync : Sync
{
    // self
    public GameObject cachedMap;
    public GameObject cachedMainQuests;
    public GameObject cachedCompassIcon;
    
    // others
    private readonly Stack<GameObject> _playerCountPins = new();

    protected override void OnEnable()
    {
        
    }

    protected override void OnDisable()
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
        
            if (!cachedMap) cachedMap = GameObject.Find("Game_Map_Hornet");
            if (!cachedMap) cachedMap = GameObject.Find("Game_Map_Hornet(Clone)");
            if (cachedMap && !cachedMainQuests) cachedMainQuests = cachedMap.transform.Find("Main Quest Pins")?.gameObject;
            if (cachedMap && !cachedCompassIcon) cachedCompassIcon = cachedMap.transform.Find("Compass Icon")?.gameObject;
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
            if (!cachedCompassIcon || !cachedMap || !cachedMainQuests) return;

            while (_playerCountPins.Count < SilklessAPI.PlayerIDs.Count)
            {
                GameObject pin = Instantiate(cachedCompassIcon, cachedMap.transform);
                pin.SetActive(cachedMainQuests.activeSelf);
                pin.name = "SilklessPlayerCount";
                pin.transform.position = new Vector3(-14.8f + 0.6f * SilklessAPI.PlayerIDs.Count, -8.2f, 0);
                pin.transform.localScale = new Vector3(0.6f, 0.6f, 1);
                
                LogUtil.LogDebug($"Created player count pin {_playerCountPins.Count}");
                
                _playerCountPins.Push(pin);
            }

            while (_playerCountPins.Count > SilklessAPI.PlayerIDs.Count && _playerCountPins.Count > 0)
            {
                Destroy(_playerCountPins.Pop());
                
                LogUtil.LogDebug($"Deleted player count pin {_playerCountPins.Count + 1}");
            }

            int i = 0;
            foreach (GameObject pin in _playerCountPins)
            {
                if (!pin) continue;
                
                pin.transform.position = new Vector3(-14.8f + 0.6f * (i++), -8.2f, 0);
                pin.SetActive(cachedMainQuests.activeSelf);
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }

    protected override void Reset()
    {
        try
        {
            _playerCountPins.Clear();
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }
}