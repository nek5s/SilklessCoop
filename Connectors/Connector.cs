using SilklessCoop.Components;
using SilklessCoop.Global;
using System;
using UnityEngine;

namespace SilklessCoop.Connectors
{
    internal abstract class Connector : MonoBehaviour
    {
        public bool Initialized = false;
        public bool Enabled = false;
        public bool Connected = false;

        public Action<byte[]> OnData;

        protected GameSync _sync;
        protected NetworkInterface _interface;

        public abstract string GetConnectorName();

        public abstract string GetId();

        private float _tickTimeout;

        protected virtual void Start()
        {
            _sync = gameObject.GetComponent<GameSync>();
            _interface = gameObject.GetComponent<NetworkInterface>();
        }

        protected virtual void Update()
        {
            if (_tickTimeout >= 0) _tickTimeout -= Time.unscaledDeltaTime;
            else { Tick(); _tickTimeout = 1.0f / ModConfig.TickRate; }
        }

        protected virtual void Tick() { }

        public virtual bool Init()
        {
            Initialized = true;
            return true;
        }

        public virtual void Enable()
        {
            Enabled = true;
        }

        public virtual void Disable()
        {
            Enabled = false;
        }

        public abstract void SendData(byte[] data);
    }
}
