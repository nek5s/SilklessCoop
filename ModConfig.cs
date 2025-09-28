using BepInEx.Configuration;
using UnityEngine;

namespace SilklessCoopVisual;

public static class ModConfig
{
    // silklesscoopvisual
    public static float PlayerOpacity;
    public static float CompassOpacity;

    // misc
    public static KeyCode MultiplayerToggleKey;
    public static float PopupTimeout;

    public static string Version;

    public static void Bind(ConfigFile config)
    {
        PlayerOpacity = config.Bind("Visuals", "Player Opacity", 0.7f, "Opacity of other players (0.0f = invisible, 1.0f = as opaque as yourself).").Value;
        CompassOpacity = config.Bind("Visuals", "Compass Opacity", 0.7f, "Opacity of other players' compasses.").Value;
        
        MultiplayerToggleKey = config.Bind("General", "Toggle Key", KeyCode.F5, "Key used to toggle multiplayer.").Value;
        PopupTimeout = config.Bind("General", "Popup Timeout", 5.0f, "Time until popup messages hide (set this to 0 to disable popups).").Value;
        
        Version = MyPluginInfo.PLUGIN_VERSION;
    }
}