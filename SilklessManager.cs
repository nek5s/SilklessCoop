using BepInEx.Logging;
using UnityEngine;

namespace SilklessCoop
{
    internal class SilklessManager : MonoBehaviour
    {
        public ManualLogSource Logger = null;
        public string ServerIP = "127.0.0.1";
        public int ServerPort = 45565;
        public int TickRate = 20;
        public float PlayerOpacity = 0.7f;

        private OnlineConnection _oc;

        public void Start()
        {
            _oc = gameObject.AddComponent<OnlineConnection>();
        }
        public void Update()
        {
            if (Logger == null) return;

            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (!_oc.Connected)
                {
                    _oc.TickRate = TickRate;
                    _oc.PlayerOpacity = PlayerOpacity;
                    _oc.Connect(ServerIP, ServerPort, Logger);
                }
                else
                {
                    _oc.Disconnect();
                }
            }
        }
    }
}
