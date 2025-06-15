using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using System.Runtime.Versioning;

namespace PricisApp
{
    public static class DebugHelper
    {
        public static void RunDiagnostics()
        {
            try
            {
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "debug_log.txt");
                using var writer = new StreamWriter(logPath, true);
                writer.WriteLine($"=== Diagnostic run at {DateTime.Now} ===");
                
                writer.WriteLine($"OS Version: {Environment.OSVersion}");
                writer.WriteLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
                writer.WriteLine($"64-bit Process: {Environment.Is64BitProcess}");
                writer.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
                writer.WriteLine($"Has Write Access: {TestWriteAccess()}");
                
                var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "TimeTracking.db");
                writer.WriteLine($"Database Path: {dbPath}");
                writer.WriteLine($"Database Exists: {File.Exists(dbPath)}");
                
                if (File.Exists(dbPath))
                {
                    writer.WriteLine($"Database Size: {new FileInfo(dbPath).Length} bytes");
                    
                    try
                    {
                        var connectionString = new SqliteConnectionStringBuilder
                        {
                            DataSource = dbPath,
                            Mode = SqliteOpenMode.ReadOnly
                        }.ToString();
                        
                        using var connection = new SqliteConnection(connectionString);
                        connection.Open();
                        writer.WriteLine("Database can be opened successfully");
                        
                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
                        using var reader = cmd.ExecuteReader();
                        
                        writer.WriteLine("Tables in database:");
                        while (reader.Read())
                        {
                            writer.WriteLine($"  - {reader.GetString(0)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine($"Error opening database: {ex.Message}");
                    }
                }
                
                var walPath = dbPath + "-wal";
                var shmPath = dbPath + "-shm";
                writer.WriteLine($"WAL file exists: {File.Exists(walPath)}");
                writer.WriteLine($"SHM file exists: {File.Exists(shmPath)}");
                
                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);
                writer.WriteLine($"Number of instances: {processes.Length}");
                
                foreach (var process in processes)
                {
                    if (process.Id != currentProcess.Id)
                    {
                        writer.WriteLine($"Another instance running: PID {process.Id}, Started at {process.StartTime}");
                    }
                }
                
                writer.WriteLine("Diagnostic completed successfully");
                
                ShowDiagnosticCompletedMessage(logPath);
            }
            catch (Exception ex)
            {
                ShowDiagnosticErrorMessage(ex);
            }
        }
        
        [SupportedOSPlatform("windows")]
        private static void ShowDiagnosticCompletedMessage(string logPath)
        {
            MessageBox.Show(
                $"Diagnostic completed. Log file created at:\n{logPath}",
                "Diagnostic Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        
        [SupportedOSPlatform("windows")]
        private static void ShowDiagnosticErrorMessage(Exception ex)
        {
            MessageBox.Show(
                $"Error running diagnostics: {ex.Message}\n\n{ex.StackTrace}",
                "Diagnostic Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        
        private static bool TestWriteAccess()
        {
            try
            {
                var testFile = Path.Combine(Directory.GetCurrentDirectory(), "write_test.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}