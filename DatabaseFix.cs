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
                // Use current directory instead of LocalApplicationData
                var folder = Directory.GetCurrentDirectory();
                var dbPath = Path.Combine(folder, "TimeTracking.db");
                
                Console.WriteLine($"Database path: {dbPath}");
                Console.WriteLine($"Checking if database exists: {File.Exists(dbPath)}");
                
                // Check folder permissions
                try
                {
                    Directory.CreateDirectory(folder);
                    var testFile = Path.Combine(folder, "test_write.tmp");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    Console.WriteLine("Folder permissions: Write access confirmed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Folder permissions error: {ex.Message}");
                    throw new InvalidOperationException($"Cannot write to application folder: {ex.Message}", ex);
                }
                
                bool needToCreateDatabase = false;
                
                // Check if database exists and is valid
                if (File.Exists(dbPath))
                {
                    try
                    {
                        // Test if we can open the database
                        var testConnectionString = new SqliteConnectionStringBuilder
                        {
                            DataSource = dbPath,
                            Mode = SqliteOpenMode.ReadOnly
                        }.ToString();
                        
                        using var testConnection = new SqliteConnection(testConnectionString);
                        await testConnection.OpenAsync();
                        Console.WriteLine("Existing database is valid");
                        
                        // Test if tables exist
                        using var cmd = testConnection.CreateCommand();
                        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Tasks'";
                        var result = await cmd.ExecuteScalarAsync();
                        
                        if (result == null)
                        {
                            Console.WriteLine("Database exists but schema is missing");
                            needToCreateDatabase = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Database exists but is invalid: {ex.Message}");
                        needToCreateDatabase = true;
                        
                        // Delete the corrupted database
                        try
                        {
                            File.Delete(dbPath);
                            Console.WriteLine("Deleted corrupted database file");
                            
                            // Delete WAL and SHM files if they exist
                            var walPath = dbPath + "-wal";
                            var shmPath = dbPath + "-shm";
                            
                            if (File.Exists(walPath))
                            {
                                File.Delete(walPath);
                                Console.WriteLine("Deleted WAL file");
                            }
                            
                            if (File.Exists(shmPath))
                            {
                                File.Delete(shmPath);
                                Console.WriteLine("Deleted SHM file");
                            }
                        }
                        catch (Exception deleteEx)
                        {
                            Console.WriteLine($"Error deleting corrupted database: {deleteEx.Message}");
                            throw new InvalidOperationException($"Database is corrupted and cannot be deleted: {deleteEx.Message}", deleteEx);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Database does not exist, will create new one");
                    needToCreateDatabase = true;
                }
                
                // Create a new database if needed
                if (needToCreateDatabase)
                {
                    // Create a new database with the correct schema
                    var connectionString = new SqliteConnectionStringBuilder
                    {
                        DataSource = dbPath,
                        Mode = SqliteOpenMode.ReadWriteCreate
                    }.ToString();
                    
                    Console.WriteLine($"Connection string: {connectionString}");
                    
                    using var connection = new SqliteConnection(connectionString);
                    try
                    {
                        await connection.OpenAsync();
                        Console.WriteLine("Database connection opened successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error opening database connection: {ex.Message}");
                        throw;
                    }
                    
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
                        INSERT OR IGNORE INTO Categories (Name, Color) VALUES ('Work', '#FF5733');
                        INSERT OR IGNORE INTO Categories (Name, Color) VALUES ('Personal', '#33FF57');
                        INSERT OR IGNORE INTO Categories (Name, Color) VALUES ('Study', '#3357FF');
                        
                        INSERT OR IGNORE INTO Tasks (Name, IsComplete, CategoryId, CreatedAt) VALUES ('Sample Task 1', 0, 1, datetime('now'));
                        INSERT OR IGNORE INTO Tasks (Name, IsComplete, CategoryId, CreatedAt) VALUES ('Sample Task 2', 0, 2, datetime('now'));

                        -- Add State column if it does not exist
                        ALTER TABLE Sessions ADD COLUMN State TEXT DEFAULT 'Stopped';
                        UPDATE Sessions SET State = 'Stopped' WHERE State IS NULL;
                    ";
                    
                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                        Console.WriteLine("Database schema created successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error executing SQL: {ex.Message}");
                        throw;
                    }
                }
                else
                {
                    Console.WriteLine("Using existing database");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fixing database: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
} 