using System;
using System.IO;

namespace PricisApp
{
    public static class StartupDebug
    {
        public static void LogStartup(string message)
        {
            try
            {
                string logPath = Path.Combine(Directory.GetCurrentDirectory(), "startup_log.txt");
                using (StreamWriter writer = new StreamWriter(logPath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch
            {
            }
        }
    }
} 