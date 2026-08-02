using UnityEngine;

namespace ImageScreens
{
    // Helper class for logging to the Unity/Stationeers log.
    internal static class Log
    {
        // Log an error.
        public static void Error(string message)
        {
            Debug.LogError($"[{Version.ModName}] {message}");
        }

        // Log a warning.
        public static void Warning(string message)
        {
            Debug.LogWarning($"[{Version.ModName}] {message}");
        }

        // Log an information message.
        public static void Info(string message)
        {
            Debug.Log($"[{Version.ModName}] {message}");
        }
    }
}
