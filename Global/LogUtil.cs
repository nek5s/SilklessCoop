using BepInEx.Logging;
using SilklessCoop.Components;
using System.Diagnostics;
using UnityEngine;

namespace SilklessCoop.Global
{
    internal class LogUtil
    {
        public static ManualLogSource ConsoleLogger;
        public static PopupManager PopupManager;

        public static void LogDebug(string message)
        {
            if (!ModConfig.PrintDebugOutput) return;

            string caller = new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "unknown";
            string s = $"[DEBUG] {caller}:  {message}";

            ConsoleLogger?.LogInfo(s);
        }

        public static void LogInfo(string message, bool popup = false)
        {
            string caller = new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "unknown";
            string s = $"[INFO] {caller}:  {message}";

            ConsoleLogger?.LogInfo(s);
            if (popup && ModConfig.PopupTimeout != -1) PopupManager?.SpawnPopup(s);
        }

        public static void LogError(string message)
        {
            string caller = new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "unknown";
            string method = new StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "unknown";
            string s = $"[ERROR] {caller}/{method}:  {message}";

            ConsoleLogger?.LogError(s);
            if (ModConfig.PopupTimeout != -1) PopupManager?.SpawnPopup($"Encountered error in {caller}/{method}. Check log for details.", Color.red);
        }
    }
}
