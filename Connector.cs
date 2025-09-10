using BepInEx.Logging;
using UnityEngine;

namespace SilklessCoop
{
    internal abstract class Connector : MonoBehaviour
    {
        protected GameSync _sync;
        public ManualLogSource Logger;
        public int TickRate;
        public bool PrintDebugOutput;

        public bool Initialized;
        public bool Active;

        protected float _tickTimeout;

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
                    _tickTimeout = 1.0f / TickRate;
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
