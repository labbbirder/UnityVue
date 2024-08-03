using System;

namespace BBBirder.UnityVue
{
    public enum LoggerLevel
    {
        Verbose,
        Info,
        Warning,
        Error,
    }
    public static class Logger
    {
        private static bool InUnityEnv;
        internal static LoggerLevel loggerLevel = LoggerLevel.Verbose;
        static Logger()
        {
            InUnityEnv = AppDomain.CurrentDomain.FriendlyName.Contains("Unity", StringComparison.OrdinalIgnoreCase);
            InUnityEnv = true;
        }

#if UNITY_2021_3_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void Verbose(params object[] args)
        {
            if (loggerLevel > LoggerLevel.Verbose) return;
            if (InUnityEnv)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log("[UnityVue] " + string.Join(" ", args));
#endif
            }
            else
            {
                using var scp = new ConsoleColorScope(ConsoleColor.Gray);
                Console.WriteLine("[UnityVue] " + string.Join(" ", args));
            }
        }

#if UNITY_2021_3_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void Info(params object[] args)
        {
            if (loggerLevel > LoggerLevel.Info) return;
            if (InUnityEnv)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log("[UnityVue] " + string.Join(" ", args));
#endif
            }
            else
            {
                using var scp = new ConsoleColorScope(ConsoleColor.Cyan);
                Console.WriteLine("[UnityVue] " + string.Join(" ", args));
            }
        }

#if UNITY_2021_3_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void Warning(params object[] args)
        {
            if (loggerLevel > LoggerLevel.Warning) return;
            if (InUnityEnv)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning("[UnityVue] " + string.Join(" ", args));
#endif
            }
            else
            {
                using var scp = new ConsoleColorScope(ConsoleColor.Yellow);
                Console.WriteLine("[UnityVue] " + string.Join(" ", args));
            }
        }

#if UNITY_2021_3_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void Error(params object[] args)
        {
            if (loggerLevel > LoggerLevel.Error) return;
            if (InUnityEnv)
            {
#if UNITY_EDITOR
                if (args.Length == 1 && args[0] is Exception e)
                {
                    UnityEngine.Debug.LogException(e);

                }
                else
                {
                    UnityEngine.Debug.LogError("[UnityVue] " + string.Join(" ", args));
                }
#endif
            }
            else
            {
                Console.Error.WriteLine("[UnityVue] " + string.Join(" ", args));
            }
        }

        struct ConsoleColorScope : IDisposable
        {
            ConsoleColor prevColor;
            public ConsoleColorScope(ConsoleColor color)
            {
                prevColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
            }

            public void Dispose()
            {
                Console.ForegroundColor = prevColor;
            }
        }
    }
}
