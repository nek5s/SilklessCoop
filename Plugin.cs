using BepInEx;
using UnityEngine;

namespace SilklessCoop;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        // bind configs
        ModConfig config = new ModConfig();
        config.MultiplayerToggleKey = Config.Bind<KeyCode>("General", "Toggle Key", KeyCode.F5, "Key used to toggle multiplayer.").Value;
        config.ConnectionType = Config.Bind<ConnectionType>("General", "Connection Type", ConnectionType.STEAM_P2P, "Choose echoserver for standalone or steam_p2p for Steam.").Value;
        config.TickRate = Config.Bind<int>("General", "Tick Rate", 20, "Messages per second sent to the server.").Value;
        config.SyncCompasses = Config.Bind<bool>("General", "Sync Compasses", true, "Enables seeing other players compasses on your map.").Value;

        config.PrintDebugOutput = Config.Bind<bool>("General", "Print Debug Output", true, "Enables advanced logging to help find bugs.").Value;

        config.EchoServerIP = Config.Bind<string>("Standalone", "Server IP Address", "127.0.0.1", "IP Address of the standalone server.").Value;
        config.EchoServerPort = Config.Bind<int>("Standalone", "Server Port", 45565, "Port of the standalone server.").Value;

        config.PlayerOpacity = Config.Bind<float>("Visuals", "Player Opacity", 0.7f, "Opacity of other players (0.0f = invisible, 1.0f = as opaque as yourself).").Value;
        config.CompassOpacity = Config.Bind<float>("Visuals", "Active Compass Opacity", 0.7f, "Opacity of other players' compasses while they have their map open.").Value;

        // set up mod
        Logger.LogInfo($"Loading {MyPluginInfo.PLUGIN_GUID}...");

        GameObject persistentObject = new GameObject("SilklessCoop");
        DontDestroyOnLoad(persistentObject);

        GameSync sync = persistentObject.AddComponent<GameSync>();
        sync.Logger = Logger;
        sync.Config = config;

        UIAdder ua = persistentObject.AddComponent<UIAdder>();
        ua.Logger = Logger;
        ua.Config = config;

        Connector c = null;
        if (config.ConnectionType == ConnectionType.ECHOSERVER) c = persistentObject.AddComponent<StandaloneConnector>();
        if (config.ConnectionType == ConnectionType.STEAM_P2P) c = persistentObject.AddComponent<SteamConnector>();
        c.Logger = Logger;
        c.Config = config;

        if (!c.Init())
        {
            Logger.LogError($"{c.GetConnectorName()} has failed to initialize!");
            return;
        }

        Logger.LogInfo($"{c.GetConnectorName()} has initialized successfully.");

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} has initialized successfully.");
    }
}
