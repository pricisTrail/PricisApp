using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Threading;

namespace PricisApp
{
    /// <summary>
    /// Provides database operations for PricisApp with improved error handling and resilience.
    /// </summary>
    public class DatabaseHelper : IDisposable, IAsyncDisposable
    {
        private readonly string _dbPath;
        private SqliteConnection? _connection;
        private bool _disposed = false;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 100;

        public SqliteConnection Connection
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(DatabaseHelper));
                return _connection ?? throw new InvalidOperationException("Database connection is not initialized.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseHelper"/> class.
        /// </summary>
        public DatabaseHelper(string dbFileName = "TimeTracking.db")
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Directory.CreateDirectory(folder);
            _dbPath = Path.Combine(folder, dbFileName);
            InitializeDatabase().GetAwaiter().GetResult();
        }

        private async Task InitializeDatabase()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = true
            }.ToString();

            _connection = new SqliteConnection(connectionString);
            await _connection.OpenAsync();

            await ExecuteWithRetryAsync<bool>(async () =>
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = @"
                    PRAGMA journal_mode = 'wal';
                    PRAGMA foreign_keys = ON;
                    PRAGMA synchronous = NORMAL;
                    PRAGMA temp_store = MEMORY;
                    PRAGMA mmap_size = 30000000000;
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
                    );";
                await cmd.ExecuteNonQueryAsync();
                return true;
            });
        }

        /// <summary>
        /// Executes a database operation with retry logic for transient errors.
        /// </summary>
        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int retries = MaxRetries)
        {
            while (true)
            {
                try
                {
                    await _connectionLock.WaitAsync();
                    try
                    {
                        return await operation();
                    }
                    finally
                    {
                        _connectionLock.Release();
                    }
                }
                catch (SqliteException ex) when (retries > 0 && IsTransientError(ex))
                {
                    retries--;
                    await Task.Delay(RetryDelayMs);
                }
                catch (Exception ex) when (retries > 0 && IsConnectionError(ex))
                {
                    retries--;
                    await Task.Delay(RetryDelayMs);
                    await EnsureConnectionAsync();
                }
            }
        }

        private bool IsTransientError(SqliteException ex)
        {
            return ex.SqliteErrorCode == 5 || // SQLITE_BUSY
                   ex.SqliteErrorCode == 6 || // SQLITE_LOCKED
                   ex.SqliteErrorCode == 8 || // SQLITE_READONLY
                   ex.SqliteErrorCode == 11;  // SQLITE_CORRUPT
        }

        private bool IsConnectionError(Exception ex)
        {
            return ex is InvalidOperationException ||
                   ex is SqliteException sqlEx && sqlEx.SqliteErrorCode == 14; // SQLITE_CANTOPEN
        }

        private async Task EnsureConnectionAsync()
        {
            if (_connection?.State != ConnectionState.Open)
            {
                await _connection?.OpenAsync();
            }
        }

        /// <summary>
        /// Inserts a new task asynchronously with retry logic.
        /// </summary>
        public async Task<int> InsertTaskAsync(string taskName)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Tasks (Name) 
                    VALUES ($name) 
                    ON CONFLICT(Name) DO UPDATE SET Name=excluded.Name 
                    RETURNING Id;";
                cmd.Parameters.AddWithValue("$name", taskName);
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            });
        }

        /// <summary>
        /// Starts a new session asynchronously with retry logic.
        /// </summary>
        public async Task<int> StartSessionAsync(int taskId, DateTime startTime, string? notes = null)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Sessions (TaskId, StartTime, Notes) 
                    VALUES ($taskId, $startTime, $notes) 
                    RETURNING Id;";
                cmd.Parameters.AddWithValue("$taskId", taskId);
                cmd.Parameters.AddWithValue("$startTime", startTime.ToString("o"));
                cmd.Parameters.AddWithValue("$notes", string.IsNullOrEmpty(notes) ? DBNull.Value : notes);
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            });
        }

        /// <summary>
        /// Ends a session asynchronously with retry logic.
        /// </summary>
        public async Task EndSessionAsync(int sessionId, DateTime endTime, string? notes = null)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Sessions 
                    SET EndTime = $endTime, 
                        Notes = COALESCE($notes, Notes) 
                    WHERE Id = $id";
                cmd.Parameters.AddWithValue("$endTime", endTime.ToString("o"));
                cmd.Parameters.AddWithValue("$notes", string.IsNullOrEmpty(notes) ? DBNull.Value : notes);
                cmd.Parameters.AddWithValue("$id", sessionId);
                await cmd.ExecuteNonQueryAsync();
                return true;
            });
        }

        /// <summary>
        /// Gets all tasks asynchronously with retry logic.
        /// </summary>
        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var tasks = new List<TaskItem>();
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT t.Id, t.Name, t.IsComplete, c.Id, c.Name, c.Color
                    FROM Tasks t
                    LEFT JOIN Categories c ON t.CategoryId = c.Id
                    ORDER BY t.Name";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var category = reader.IsDBNull(3) ? null : new Category(
                        reader.GetInt32(3),
                        reader.GetString(4),
                        reader.GetString(5)
                    );
                    tasks.Add(new TaskItem(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetInt32(2) == 1,
                        category
                    ));
                }
                return tasks;
            });
        }

        /// <summary>
        /// Gets all sessions for a task asynchronously with retry logic.
        /// </summary>
        public async Task<DataTable> GetSessionsForTaskAsync(int taskId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var table = new DataTable();
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT 
                        datetime(StartTime) AS 'Start Time',
                        datetime(EndTime) AS 'End Time',
                        CASE 
                            WHEN EndTime IS NULL THEN 'In Progress'
                            ELSE time((julianday(EndTime) - julianday(StartTime)), 'unixepoch')
                        END AS 'Duration',
                        Notes AS 'Notes'
                    FROM Sessions
                    WHERE TaskId = $taskId
                    ORDER BY StartTime DESC";
                cmd.Parameters.AddWithValue("$taskId", taskId);
                using var reader = await cmd.ExecuteReaderAsync();
                table.Load(reader);
                return table;
            });
        }

        /// <summary>
        /// Gets a summary of all sessions asynchronously with retry logic.
        /// </summary>
        public async Task<DataTable> GetSessionSummaryAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var table = new DataTable();
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT 
                        t.Name AS 'Task',
                        c.Name AS 'Category',
                        COUNT(s.Id) AS 'Sessions',
                        SUM(CASE WHEN s.EndTime IS NULL THEN 0 
                            ELSE (julianday(s.EndTime) - julianday(s.StartTime)) * 86400 
                            END) AS 'TotalSeconds',
                        time(SUM(CASE WHEN s.EndTime IS NULL THEN 0 
                            ELSE (julianday(s.EndTime) - julianday(s.StartTime)) * 86400 
                            END), 'unixepoch') AS 'TotalTime'
                    FROM Tasks t
                    LEFT JOIN Categories c ON t.CategoryId = c.Id
                    LEFT JOIN Sessions s ON t.Id = s.TaskId
                    GROUP BY t.Id, t.Name, c.Name
                    ORDER BY t.Name";
                using var reader = await cmd.ExecuteReaderAsync();
                table.Load(reader);
                return table;
            });
        }

        /// <summary>
        /// Gets all categories asynchronously with retry logic.
        /// </summary>
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var categories = new List<Category>();
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = "SELECT Id, Name, Color FROM Categories ORDER BY Name";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    categories.Add(new Category(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.IsDBNull(2) ? "#FFFFFF" : reader.GetString(2)
                    ));
                }
                return categories;
            });
        }

        /// <summary>
        /// Inserts a new category asynchronously with retry logic.
        /// </summary>
        public async Task<int> InsertCategoryAsync(string name, string color)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Categories (Name, Color) 
                    VALUES ($name, $color) 
                    RETURNING Id;";
                cmd.Parameters.AddWithValue("$name", name);
                cmd.Parameters.AddWithValue("$color", color);
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            });
        }

        /// <summary>
        /// Updates a task's category asynchronously with retry logic.
        /// </summary>
        public async Task UpdateTaskCategoryAsync(int taskId, int? categoryId)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Tasks 
                    SET CategoryId = $categoryId 
                    WHERE Id = $taskId";
                cmd.Parameters.AddWithValue("$categoryId", categoryId);
                cmd.Parameters.AddWithValue("$taskId", taskId);
                await cmd.ExecuteNonQueryAsync();
                return true;
            });
        }

        /// <summary>
        /// Updates a task's tags asynchronously with retry logic.
        /// </summary>
        public async Task UpdateTaskTagsAsync(int taskId, IEnumerable<string> tags)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                using var transaction = Connection.BeginTransaction();
                try
                {
                    using var deleteCmd = Connection.CreateCommand();
                    deleteCmd.CommandText = "DELETE FROM TaskTags WHERE TaskId = $taskId";
                    deleteCmd.Parameters.AddWithValue("$taskId", taskId);
                    await deleteCmd.ExecuteNonQueryAsync();

                    foreach (var tag in tags)
                    {
                        using var insertCmd = Connection.CreateCommand();
                        insertCmd.CommandText = @"
                            INSERT INTO TaskTags (TaskId, Tag) 
                            VALUES ($taskId, $tag)";
                        insertCmd.Parameters.AddWithValue("$taskId", taskId);
                        insertCmd.Parameters.AddWithValue("$tag", tag.Trim());
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });
        }

        /// <summary>
        /// Gets all tags for a task asynchronously with retry logic.
        /// </summary>
        public async Task<List<string>> GetTagsForTaskAsync(int taskId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var tags = new List<string>();
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = "SELECT Tag FROM TaskTags WHERE TaskId = $taskId ORDER BY Tag";
                cmd.Parameters.AddWithValue("$taskId", taskId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tags.Add(reader.GetString(0));
                }
                return tags;
            });
        }

        /// <summary>
        /// Disposes the database connection synchronously.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _connection?.Dispose();
                _connectionLock.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Disposes the database connection asynchronously.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                if (_connection != null)
                {
                    await _connection.CloseAsync();
                    await _connection.DisposeAsync();
                }
                _connectionLock.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
