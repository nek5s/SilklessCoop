using BepInEx;
using SilklessCoop.Connectors;
using UnityEngine;

namespace SilklessCoop;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        gameObject.SetName("SilklessCoop");
        DontDestroyOnLoad(gameObject);

        // bind configs
        ModConfig config = new ModConfig();
        config.MultiplayerToggleKey = Config.Bind<KeyCode>("General", "Toggle Key", KeyCode.F5, "Key used to toggle multiplayer.").Value;
        config.ConnectionType = Config.Bind<ConnectionType>("General", "Connection Type", ConnectionType.STEAM_P2P, "Method used to connect with other players.").Value;
        config.TickRate = Config.Bind<int>("General", "Tick Rate", 20, "Messages per second sent to the server.").Value;
        config.SyncCompasses = Config.Bind<bool>("General", "Sync Compasses", true, "Enables seeing other players compasses on your map.").Value;

        config.PrintDebugOutput = Config.Bind<bool>("General", "Print Debug Output", true, "Enables advanced logging to help find bugs.").Value;

        config.EchoServerIP = Config.Bind<string>("Standalone", "Server IP Address", "127.0.0.1", "IP Address of the standalone server.").Value;
        config.EchoServerPort = Config.Bind<int>("Standalone", "Server Port", 45565, "Port of the standalone server.").Value;

        config.PlayerOpacity = Config.Bind<float>("Visuals", "Player Opacity", 0.7f, "Opacity of other players (0.0f = invisible, 1.0f = as opaque as yourself).").Value;
        config.CompassOpacity = Config.Bind<float>("Visuals", "Compass Opacity", 0.7f, "Opacity of other players' compasses.").Value;

        config.Version = MyPluginInfo.PLUGIN_VERSION;

        // set up mod
        Logger.LogInfo($"Loading {MyPluginInfo.PLUGIN_GUID}...");

        GameSync gs = gameObject.AddComponent<GameSync>();
        gs.Logger = Logger;
        gs.Config = config;

        NetworkInterface ni = gameObject.AddComponent<NetworkInterface>();
        ni.Logger = Logger;
        ni.Config = config;

        UIAdder ua = gameObject.AddComponent<UIAdder>();
        ua.Logger = Logger;
        ua.Config = config;

        Connector co = null;
        if (config.ConnectionType == ConnectionType.STEAM_P2P) co = gameObject.AddComponent<SteamConnector>();
        if (config.ConnectionType == ConnectionType.ECHOSERVER) co = gameObject.AddComponent<StandaloneConnector>();
        if (config.ConnectionType == ConnectionType.DEBUG) co = gameObject.AddComponent<DebugConnector>();
        if (co == null) { Logger.LogError($"Connector could not be selected!"); return; }
        co.Logger = Logger;
        co.Config = config;

        if (!co.Init()) { Logger.LogError($"{co.GetConnectorName()} failed to initialize!"); return; }

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} has initialized successfully.");
    }
}
