using BepInEx;
using SilklessLib;
using SimpleSync.Components;
using UnityEngine;

namespace SimpleSync;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("SilklessLib")]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        LogUtil.ConsoleLogger = Logger;

        LogUtil.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded.");

        gameObject.name = "SilklessCoop";

        // read mod config from file
        ModConfig.Bind(Config);

        // copy lib-specific mod config parts
        SilklessConfig.PrintDebugOutput = ModConfig.PrintDebugOutput;
        SilklessConfig.ConnectionType = ModConfig.ConnectionType;
        SilklessConfig.ConnectionTimeout = ModConfig.ConnectionTimeout;
        SilklessConfig.StandaloneIP = ModConfig.EchoServerIP;
        SilklessConfig.StandalonePort = ModConfig.EchoServerPort;
        SilklessConfig.Version = MyPluginInfo.PLUGIN_VERSION;

        // set up api
        if (!SilklessAPI.Init(Logger))
        {
            LogUtil.LogError("SilklessAPI failed to initialize!");
            return;
        }
        
        // set up menu button and popups
        gameObject.AddComponent<UIAdder>();
        
        Canvas cv = gameObject.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceCamera;
        
        PopupManager pm = gameObject.AddComponent<PopupManager>();
        LogUtil.OnPopupDebug += s => pm.SpawnPopup(s, Color.gray);
        LogUtil.OnPopupInfo += s => pm.SpawnPopup(s);
        LogUtil.OnPopupError += _ => pm.SpawnPopup("Encountered error, check logs for details.", Color.red);

        // enable simple sync script
        gameObject.AddComponent<SimpleSync>();
    }

    private void Update()
    {
        SilklessAPI.Update(Time.unscaledDeltaTime);

        if (Input.GetKeyDown(ModConfig.MultiplayerToggleKey))
        {
            if (SilklessAPI.Ready) SilklessAPI.Disable();
            else SilklessAPI.Enable();
        }
    }
}
