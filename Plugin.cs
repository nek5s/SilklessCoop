using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;

namespace SilklessCoop;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private ConfigEntry<string> _configServerIP;
    private ConfigEntry<int> _configServerPort;
    private ConfigEntry<int> _configTickRate;
    private ConfigEntry<float> _configPlayerOpacity;

    private void Awake()
    {
        // config
        _configServerIP = Config.Bind("General", "Server IP", "127.0.0.1", "Address of the SilklessCoop server.");
        _configServerPort = Config.Bind("General", "Server Port", 45565, "Port of the SilklessCoop server.");
        _configTickRate = Config.Bind("General", "Tick Rate", 20, "How many times per second the client sends updates to the server. (please keep this synced with the Tick Rate of all other clients)");
        _configPlayerOpacity = Config.Bind("General", "Player Opacity", 0.7f, "How opaque other players are rendered. (0.0 = invisible, 1.0 = fully visible)");

        ManualLogSource logger = base.Logger;

        logger.LogInfo($"Loading {MyPluginInfo.PLUGIN_GUID}...");

        // persistent object
        GameObject silklessManager = new GameObject("SilklessManager");
        DontDestroyOnLoad(silklessManager);

        // connection toggler
        SilklessManager manager = silklessManager.AddComponent<SilklessManager>();
        manager.Logger = logger;
        manager.ServerIP = _configServerIP.Value;
        manager.ServerPort = _configServerPort.Value;
        manager.TickRate = _configTickRate.Value;
        manager.PlayerOpacity = _configPlayerOpacity.Value;

        logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} has loaded successfully.");
    }
}
