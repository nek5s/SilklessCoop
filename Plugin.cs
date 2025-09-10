using BepInEx;
using BepInEx.Configuration;
using System;
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

    private ConfigEntry<string> _multiplayerToggleKey;
    private ConfigEntry<ConnectionType> _connectionType;
    private ConfigEntry<int> _tickRate;
    private ConfigEntry<bool> _syncCompasses;
    private ConfigEntry<bool> _printDebugOutput;
    private ConfigEntry<string> _echoServerIP;
    private ConfigEntry<int> _echoServerPort;

    private void Awake()
    {
        _multiplayerToggleKey = Config.Bind<string>("General", "Toggle Key", "F5", "Key used to toggle multiplayer.");
        _connectionType = Config.Bind<ConnectionType>("General", "Connection Type", ConnectionType.ECHOSERVER, "Choose echoserver for standalone or steam_p2p for Steam.");
        _tickRate = Config.Bind<int>("General", "Tick Rate", 20, "Messages per second sent to the server.");
        _syncCompasses = Config.Bind<bool>("General", "Sync Compasses", true, "Enables seeing other players compasses on your map.");
        _printDebugOutput = Config.Bind<bool>("General", "Print Debug Output", false, "Enables advanced logging to help find bugs.");

        _echoServerIP = Config.Bind<string>("Standalone", "Server IP Address", "127.0.0.1", "IP Address of the standalone server.");
        _echoServerPort = Config.Bind<int>("Standalone", "Server Port", 45565, "Port of the standalone server.");

        Logger.LogInfo($"Loading {MyPluginInfo.PLUGIN_GUID}...");

        GameObject persistentObject = new GameObject("SilklessCoop");
        DontDestroyOnLoad(persistentObject);

        ConnectorToggler ct = persistentObject.AddComponent<ConnectorToggler>();
        try
        {
            ct.MultiplayerToggleKey = Enum.Parse<KeyCode>(_multiplayerToggleKey.Value);
        }
        catch (Exception)
        {
            Logger.LogError("Could not set keycode, reverting to F5!");
            ct.MultiplayerToggleKey = KeyCode.F5;
        }

        GameSync sync = persistentObject.AddComponent<GameSync>();
        sync.Logger = Logger;
        sync.SyncCompasses = _syncCompasses.Value;
        sync.PrintDebugOutput = _printDebugOutput.Value;

        if (_connectionType.Value == ConnectionType.ECHOSERVER)
        {
            StandaloneConnector sc = persistentObject.AddComponent<StandaloneConnector>();
            sc.Logger = Logger;
            sc.IPAddress = _echoServerIP.Value;
            sc.Port = _echoServerPort.Value;
            sc.TickRate = _tickRate.Value;
            sc.PrintDebugOutput = _printDebugOutput.Value;

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
            sc.PrintDebugOutput = _printDebugOutput.Value;

            if (!sc.Init())
            {
                Logger.LogError("Steam connector has failed to initialize!");
                return;
            }
        }

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} has initialized successfully.");
        Logger.LogInfo($"Press {_multiplayerToggleKey.Value} to enable multiplayer.");
    }
}
