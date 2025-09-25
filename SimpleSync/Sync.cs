using SilklessLib;
using UnityEngine;

namespace SimpleSync;

public abstract class Sync : MonoBehaviour
{
    private float _tickTimeout;
    
    private void Awake()
    {
        if (!SilklessAPI.Init())
        {
            Destroy(this);
            return;
        }
        
        SilklessAPI.OnConnect += OnConnect;
        SilklessAPI.OnDisconnect += Reset;
        SilklessAPI.OnDisconnect += OnDisconnect;
        SilklessAPI.OnPlayerJoin += OnPlayerJoin;
        SilklessAPI.OnPlayerLeave += OnPlayerLeave;
    }

    private void OnDestroy()
    {
        SilklessAPI.OnConnect -= OnConnect;
        SilklessAPI.OnDisconnect -= Reset;
        SilklessAPI.OnDisconnect -= OnDisconnect;
        SilklessAPI.OnPlayerJoin -= OnPlayerJoin;
        SilklessAPI.OnPlayerLeave -= OnPlayerLeave;
    }

    protected abstract void OnConnect();
    protected abstract void OnDisconnect();

    protected abstract void OnPlayerJoin(string id);

    protected abstract void OnPlayerLeave(string id);
    
    protected virtual void Update()
    {
        // tick
        if (SilklessAPI.Ready)
        {
            _tickTimeout -= Time.unscaledDeltaTime;
            while (_tickTimeout <= 0)
            {
                Tick();
                
                _tickTimeout += 1.0f / ModConfig.TickRate;
            }
        }
    }

    protected abstract void Tick();

    protected abstract void Reset();
}