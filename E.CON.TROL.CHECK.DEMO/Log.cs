using System;

namespace E.CON.TROL.CHECK.DEMO
{
    static class LogHelper
    {
        public static event EventHandler<string> LogEventOccured;

        public static void Log(this object source, string message, int level = 1)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (level >= Config.Instance.LogLevel)
                {
                    LogEventOccured?.Invoke(source, $"{DateTime.Now.ToString("HH-mm-ss,fff")} - {message}");
                }
            }
        }
    }
}
