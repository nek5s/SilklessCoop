using BepInEx.Logging;
using UnityEngine;

namespace SilklessCoop
{
    internal abstract class Connector : MonoBehaviour
    {
        public ManualLogSource Logger;
        public ModConfig Config;

        public bool Initialized;
        public bool Active;

        protected GameSync _sync;

        protected float _tickTimeout;

        public abstract string GetConnectorName();

        protected void Start()
        {
            _sync = gameObject.GetComponent<GameSync>();
            _tickTimeout = 0;
        }

        public virtual bool Init()
        {
            Initialized = true;
            return true;
        }

        protected virtual void Update()
        {
            if (!Active) return;

            if (_tickTimeout >= 0)
            {
                _tickTimeout -= Time.unscaledDeltaTime;

                if (_tickTimeout <= 0)
                {
                    Tick();
                    _tickTimeout = 1.0f / Config.TickRate;
                }
            }
        }

        public virtual void Enable()
        {
            Active = true;
        }
        public virtual void Disable()
        {
            Active = false;
            _sync.Reset();
        }

        protected virtual void Tick()
        {

        }
    }
}
