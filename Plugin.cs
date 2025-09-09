using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace SilklessCoop;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private enum ConnectionType
    {
        ECHOSERVER,
        STEAM_P2P
    };

    private ConfigEntry<ConnectionType> _connectionType;
    private ConfigEntry<int> _tickRate;
    private ConfigEntry<string> _echoServerIP;
    private ConfigEntry<int> _echoServerPort;

    private bool _active = false;

    private void Awake()
    {
        _connectionType = Config.Bind<ConnectionType>("General", "Connection Type", ConnectionType.ECHOSERVER, "Choose echoserver for standalone or steam_p2p for Steam.");
        _tickRate = Config.Bind<int>("General", "Tick Rate", 20, "Messages per second sent to the server.");
        _echoServerIP = Config.Bind<string>("Standalone", "Server IP Address", "127.0.0.1", "IP Address of the standalone server.");
        _echoServerPort = Config.Bind<int>("Standalone", "Server Port", 45565, "Port of the standalone server.");

        Logger.LogInfo($"Loading {MyPluginInfo.PLUGIN_GUID}...");

        GameObject persistentObject = new GameObject("SilklessCoop");
        DontDestroyOnLoad(persistentObject);

        ConnectorToggler ct = persistentObject.AddComponent<ConnectorToggler>();

        GameSync sync = persistentObject.AddComponent<GameSync>();
        sync.Logger = Logger;

        if (_connectionType.Value == ConnectionType.ECHOSERVER)
        {
            StandaloneConnector sc = persistentObject.AddComponent<StandaloneConnector>();
            sc.Logger = Logger;
            sc.IPAddress = _echoServerIP.Value;
            sc.Port = _echoServerPort.Value;
            sc.TickRate = _tickRate.Value;

            if (!sc.Init())
            {
                Logger.LogError("Standalone connector has failed to initialize!");
                return;
            }
        } else
        {
            SteamConnector sc = persistentObject.AddComponent<SteamConnector>();
            sc.Logger = Logger;
            sc.TickRate = _tickRate.Value;

            if (!sc.Init())
            {
                Logger.LogError("Steam connector has failed to initialize!");
                return;
            }
        }

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} has initialized successfully.");
        Logger.LogInfo("Press F5 to enable multiplayer.");
    }
}
