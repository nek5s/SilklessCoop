using BepInEx.Logging;
using UnityEngine;

namespace SilklessCoop
{
    internal abstract class Connector : MonoBehaviour
    {
        protected GameSync _sync;
        public ManualLogSource Logger;

        public bool Initialized;
        public bool Active;

        protected void Start()
        {
            _sync = gameObject.GetComponent<GameSync>();
        }

        public virtual bool Init()
        {
            Initialized = true;
            return true;
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
