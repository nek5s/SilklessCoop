using UnityEngine;

namespace SilklessCoop
{
    public enum ConnectionType
    {
        ECHOSERVER,
        STEAM_P2P
    };

    internal class ModConfig
    {
        public KeyCode MultiplayerToggleKey;
        public ConnectionType ConnectionType;
        public int TickRate;
        public bool SyncCompasses;
        public bool PrintDebugOutput;

        public string EchoServerIP;
        public int EchoServerPort;

        public float PlayerOpacity;
        public float ActiveCompassOpacity;
        public float InactiveCompassOpacity;
    };
}
