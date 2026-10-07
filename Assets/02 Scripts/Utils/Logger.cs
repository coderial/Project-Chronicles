using System.Diagnostics;

namespace Project_Chronicles.Utils
{
    public static class Logging
    {
        [Conditional("ENABLE_LOG")]
        public static void Log(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        [Conditional("ENABLE_LOG")]
        public static void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        [Conditional("ENABLE_LOG")]
        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}