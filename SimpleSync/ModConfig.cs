using BepInEx.Configuration;
using UnityEngine;
using static SilklessLib.SilklessConfig;

namespace SimpleSync;

internal static class ModConfig
{
    // simplesync
    public static int TickRate;
    public static float PlayerOpacity;
    public static float CompassOpacity;

    // misc
    public static KeyCode MultiplayerToggleKey;
    public static EConnectionType ConnectionType;
    public static bool PrintDebugOutput;
    public static float PopupTimeout;

    // connection
    public static float ConnectionTimeout;

    // standalone
    public static string EchoServerIP;
    public static int EchoServerPort;

    public static string Version;

    public static void Bind(ConfigFile config)
    {
        TickRate = config.Bind("General", "Tick Rate", 20, "Messages per second sent to the server.").Value;
        PlayerOpacity = config.Bind("Visuals", "Player Opacity", 0.7f, "Opacity of other players (0.0f = invisible, 1.0f = as opaque as yourself).").Value;
        CompassOpacity = config.Bind("Visuals", "Compass Opacity", 0.7f, "Opacity of other players' compasses.").Value;
        
        MultiplayerToggleKey = config.Bind("General", "Toggle Key", KeyCode.F5, "Key used to toggle multiplayer.").Value;
        ConnectionType = config.Bind("General", "Connection Type", EConnectionType.SteamP2P, "Method used to connect with other players.").Value;
        PrintDebugOutput = config.Bind("General", "Print Debug Output", true, "Enables advanced logging to help find bugs.").Value;
        PopupTimeout = config.Bind("General", "Popup Timeout", 5.0f, "Time until popup messages hide.").Value;
        
        ConnectionTimeout = config.Bind<float>("General", "Connection Timeout", 5, "Set after how many seconds inactive users are kicked.").Value;

        EchoServerIP = config.Bind("Standalone", "Server IP Address", "127.0.0.1", "IP Address of the standalone server.").Value;
        EchoServerPort = config.Bind("Standalone", "Server Port", 45565, "Port of the standalone server.").Value;

        Version = MyPluginInfo.PLUGIN_VERSION;
    }
}