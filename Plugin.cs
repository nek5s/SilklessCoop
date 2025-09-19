using BepInEx;
using SilklessCoop.Components;
using SilklessCoop.Connectors;
using SilklessCoop.Global;
using UnityEngine;

namespace SilklessCoop;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        LogUtil.ConsoleLogger = Logger;

        gameObject.SetName("SilklessCoop");
        DontDestroyOnLoad(gameObject);

        // bind configs
        ModConfig.Bind(Config);

        // set up mod
        LogUtil.LogInfo($"Loading {MyPluginInfo.PLUGIN_GUID}...");

        GameSync gs = gameObject.AddComponent<GameSync>();

        NetworkInterface ni = gameObject.AddComponent<NetworkInterface>();

        UIAdder ua = gameObject.AddComponent<UIAdder>();

        Canvas cv = gameObject.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceCamera;

        PopupManager pm = gameObject.AddComponent<PopupManager>();
        LogUtil.PopupManager = pm;

        Connector co = null;
        if (ModConfig.ConnectionType == ConnectionType.STEAM_P2P) co = gameObject.AddComponent<SteamConnector>();
        if (ModConfig.ConnectionType == ConnectionType.ECHOSERVER) co = gameObject.AddComponent<StandaloneConnector>();
        if (ModConfig.ConnectionType == ConnectionType.DEBUG) co = gameObject.AddComponent<DebugConnector>();
        if (co == null) { LogUtil.LogError($"Connector could not be selected!"); return; }

        if (!co.Init()) { LogUtil.LogError($"{co.GetConnectorName()} failed to initialize!"); return; }

        LogUtil.LogInfo($"{MyPluginInfo.PLUGIN_GUID} has initialized successfully.");
    }
}
