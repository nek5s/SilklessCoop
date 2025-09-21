using System;
using System.Diagnostics;
using BepInEx.Logging;
using JetBrains.Annotations;

namespace SilklessLib
{
    public static class LogUtil
    {
        public static ManualLogSource ConsoleLogger;

        [UsedImplicitly] public static Action<string> OnLog;
        [UsedImplicitly] public static Action<string> OnPopup;

        [UsedImplicitly] public static Action<string> OnDebug;
        [UsedImplicitly] public static Action<string> OnPopupDebug;
        [UsedImplicitly] public static Action<string> OnInfo;
        [UsedImplicitly] public static Action<string> OnPopupInfo;
        [UsedImplicitly] public static Action<string> OnError;
        [UsedImplicitly] public static Action<string> OnPopupError;

        public static void LogDebug(string message, bool popup = false)
        {
            if (!SilklessConfig.PrintDebugOutput) return;

            string caller = new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "unknown";
            string s = $"[DEBUG] {caller}:  {message}";

            ConsoleLogger?.LogInfo(s);
            
            OnLog?.Invoke(s);
            OnDebug?.Invoke(s);
            if (popup) OnPopup?.Invoke(s);
            if (popup) OnPopupDebug?.Invoke(s);
        }

        public static void LogInfo(string message, bool popup = false)
        {
            string caller = new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "unknown";
            string s = $"[INFO] {caller}:  {message}";

            ConsoleLogger?.LogInfo(s);
            
            OnLog?.Invoke(s);
            OnInfo?.Invoke(s);
            if (popup) OnPopup?.Invoke(s);
            if (popup) OnPopupInfo?.Invoke(s);
        }

        public static void LogError(string message, bool popup = true)
        {
            string caller = new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "unknown";
            string method = new StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "unknown";
            string s = $"[ERROR] {caller}/{method}:  {message}";

            ConsoleLogger?.LogError(s);
            
            OnLog?.Invoke(s);
            OnError?.Invoke(s);
            if (popup) OnPopup?.Invoke(s);
            if (popup) OnPopupError?.Invoke(s);
        }
    }
}
