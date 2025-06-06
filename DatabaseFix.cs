using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace PricisApp
{
    public static class DatabaseFix
    {
        public static async Task FixDatabaseAsync()
        {
            try
            {
                var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dbPath = Path.Combine(folder, "TimeTracking.db");
                
                // Delete existing database if it exists
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                    Console.WriteLine("Deleted existing database file");
                }
                
                // Delete WAL and SHM files if they exist
                var walPath = dbPath + "-wal";
                var shmPath = dbPath + "-shm";
                if (File.Exists(walPath)) File.Delete(walPath);
                if (File.Exists(shmPath)) File.Delete(shmPath);
                
                // Create a new database with the correct schema
                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = dbPath,
                    Mode = SqliteOpenMode.ReadWriteCreate
                }.ToString();
                
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();
                
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    PRAGMA journal_mode = 'wal';
                    PRAGMA foreign_keys = ON;
                    
                    CREATE TABLE IF NOT EXISTS Categories (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Color TEXT DEFAULT '#FFFFFF'
                    );
                    
                    CREATE TABLE IF NOT EXISTS Tasks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        IsComplete INTEGER DEFAULT 0,
                        CategoryId INTEGER,
                        CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                        FOREIGN KEY(CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL
                    );
                    
                    CREATE TABLE IF NOT EXISTS TaskTags (
                        TaskId INTEGER NOT NULL,
                        Tag TEXT NOT NULL,
                        PRIMARY KEY(TaskId, Tag),
                        FOREIGN KEY(TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
                    );
                    
                    CREATE TABLE IF NOT EXISTS Sessions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TaskId INTEGER NOT NULL,
                        StartTime TEXT NOT NULL,
                        EndTime TEXT,
                        Notes TEXT,
                        FOREIGN KEY(TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
                    );
                    
                    -- Create indexes
                    CREATE INDEX IF NOT EXISTS IX_Tasks_CategoryId ON Tasks(CategoryId);
                    CREATE INDEX IF NOT EXISTS IX_Tasks_IsComplete_CreatedAt ON Tasks(IsComplete, CreatedAt);
                    CREATE INDEX IF NOT EXISTS IX_Sessions_TaskId ON Sessions(TaskId);
                    CREATE INDEX IF NOT EXISTS IX_Sessions_TaskId_StartTime_EndTime ON Sessions(TaskId, StartTime, EndTime);
                    CREATE INDEX IF NOT EXISTS IX_TaskTags_TaskId ON TaskTags(TaskId);
                    
                    -- Insert sample data
                    INSERT INTO Categories (Name, Color) VALUES ('Work', '#FF5733');
                    INSERT INTO Categories (Name, Color) VALUES ('Personal', '#33FF57');
                    INSERT INTO Categories (Name, Color) VALUES ('Study', '#3357FF');
                    
                    INSERT INTO Tasks (Name, IsComplete, CategoryId, CreatedAt) VALUES ('Sample Task 1', 0, 1, datetime('now'));
                    INSERT INTO Tasks (Name, IsComplete, CategoryId, CreatedAt) VALUES ('Sample Task 2', 0, 2, datetime('now'));
                ";
                
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine("Database schema fixed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fixing database: {ex.Message}");
            }
        }
    }
} 