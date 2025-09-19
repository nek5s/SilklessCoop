using BepInEx.Configuration;
using UnityEngine;

namespace SilklessCoop.Global
{
    public enum ConnectionType
    {
        ECHOSERVER,
        STEAM_P2P,
        DEBUG
    };

    internal class ModConfig
    {
        public static KeyCode MultiplayerToggleKey;
        public static ConnectionType ConnectionType;
        public static int TickRate;
        public static bool SyncCompasses;
        public static bool PrintDebugOutput;

        public static float ConnectionTimeout;
        public static float PopupTimeout;

        public static string EchoServerIP;
        public static int EchoServerPort;

        public static float PlayerOpacity;
        public static float CompassOpacity;

        public static string Version;

        public static void Bind(ConfigFile Config)
        {
            ModConfig.MultiplayerToggleKey = Config.Bind<KeyCode>("General", "Toggle Key", KeyCode.F5, "Key used to toggle multiplayer.").Value;
            ModConfig.ConnectionType = Config.Bind<ConnectionType>("General", "Connection Type", ConnectionType.STEAM_P2P, "Method used to connect with other players.").Value;
            ModConfig.TickRate = Config.Bind<int>("General", "Tick Rate", 20, "Messages per second sent to the server.").Value;
            ModConfig.SyncCompasses = Config.Bind<bool>("General", "Sync Compasses", true, "Enables seeing other players compasses on your map.").Value;

            ModConfig.PrintDebugOutput = Config.Bind<bool>("General", "Print Debug Output", true, "Enables advanced logging to help find bugs.").Value;
            ModConfig.ConnectionTimeout = Config.Bind<float>("General", "Connection Timeout", 5, "Set after how many seconds inactive users are kicked.").Value;
            ModConfig.PopupTimeout = Config.Bind<float>("General", "Popup Timeout", 5, "Set after how many seconds popups are closed. (-1 to hide popups)").Value;

            ModConfig.EchoServerIP = Config.Bind<string>("Standalone", "Server IP Address", "127.0.0.1", "IP Address of the standalone server.").Value;
            ModConfig.EchoServerPort = Config.Bind<int>("Standalone", "Server Port", 45565, "Port of the standalone server.").Value;

            ModConfig.PlayerOpacity = Config.Bind<float>("Visuals", "Player Opacity", 0.7f, "Opacity of other players (0.0f = invisible, 1.0f = as opaque as yourself).").Value;
            ModConfig.CompassOpacity = Config.Bind<float>("Visuals", "Compass Opacity", 0.7f, "Opacity of other players' compasses.").Value;

            ModConfig.Version = MyPluginInfo.PLUGIN_VERSION;
        }
    };
}
