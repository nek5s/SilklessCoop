using BepInEx;
using HarmonyLib;
using SilklessCoopVisual.Components;
using SilklessCoopVisual.Syncs;
using SilklessLib;
using UnityEngine;

namespace SilklessCoopVisual;

public class ModVersionPacket : SilklessPacket
{
    public string Version;
}

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
        
        // set up api
        if (!SilklessAPI.Init(Logger))
        {
            LogUtil.LogError("SilklessAPI failed to initialize!");
            return;
        }
        
        // check versions
        SilklessAPI.OnPlayerJoin += _ => SilklessAPI.SendPacket(new ModVersionPacket {Version = MyPluginInfo.PLUGIN_VERSION});
        SilklessAPI.AddHandler<ModVersionPacket>(packet =>
        {
            if (packet.Version == MyPluginInfo.PLUGIN_VERSION)
            {
                LogUtil.LogInfo($"Version {MyPluginInfo.PLUGIN_VERSION} matched.", true);
                return;
            }
            
            LogUtil.LogInfo($"Version mismatch! You: {MyPluginInfo.PLUGIN_VERSION}, {packet.ID}: {packet.Version}", true);
            SilklessAPI.Disable();
        });

        // patch
        new Harmony("com.nek5.silklesscoopvisual").PatchAll();
        
        // set up menu button and popups
        gameObject.AddComponent<UIAdder>();
        
        Canvas cv = gameObject.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceCamera;
        
        PopupManager pm = gameObject.AddComponent<PopupManager>();
        LogUtil.OnPopupDebug += s => pm.SpawnPopup(s, Color.gray);
        LogUtil.OnPopupInfo += s => pm.SpawnPopup(s);
        LogUtil.OnPopupError += _ => pm.SpawnPopup("Encountered error, check logs for details.", Color.red);

        // add syncs
        gameObject.AddComponent<HornetVisualSync>();
        gameObject.AddComponent<CompassSync>();
        gameObject.AddComponent<PlayerCountSync>();
    }

    private void OnDestroy()
    {
        foreach (Sync s in gameObject.GetComponents<Sync>()) Destroy(s);
        
        new Harmony("com.nek5.silklesscoopvisual").UnpatchSelf();
    }

    private void Update()
    {
        if (Input.GetKeyDown(ModConfig.MultiplayerToggleKey))
        {
            SilklessAPI.Toggle();
        }
    }
}
