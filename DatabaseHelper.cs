using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Threading;
using PricisApp.Repositories;
using PricisApp.Models;
using PricisApp.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace PricisApp
{
    /// <summary>
    /// Provides database initialization and repository management for PricisApp.
    /// </summary>
    public class DatabaseHelper : IDisposable, IAsyncDisposable
    {
        private readonly string _dbPath;
        private SqliteConnection? _connection;
        private bool _disposed = false;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 100;
        private readonly IConfiguration _configuration;
        private readonly AppSettings _appSettings;

        private IUnitOfWork? _unitOfWork;

        public IUnitOfWork UnitOfWork => _unitOfWork ?? throw new InvalidOperationException("Database not initialized");

        // Expose connection for direct access when needed
        public SqliteConnection Connection => _connection ?? throw new InvalidOperationException("Database connection not initialized");

        // Keep these properties for backward compatibility
        public TaskRepository Tasks => UnitOfWork.Tasks;
        public SessionRepository Sessions => UnitOfWork.Sessions;
        public CategoryRepository Categories => UnitOfWork.Categories;

        /// <summary>
        /// Initializes a new instance of the DatabaseHelper class.
        /// </summary>
        public DatabaseHelper(IConfiguration configuration, AppSettings appSettings)
        {
            _configuration = configuration;
            _appSettings = appSettings;
            
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Directory.CreateDirectory(folder);
            _dbPath = Path.Combine(folder, _appSettings.Database.FileName);
            InitializeDatabase().GetAwaiter().GetResult();
        }

        private async Task InitializeDatabase()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = GetSqliteOpenMode(_appSettings.Database.ConnectionString.Mode),
                Cache = GetSqliteCacheMode(_appSettings.Database.ConnectionString.Cache),
                Pooling = _appSettings.Database.ConnectionString.Pooling
            }.ToString();

            _connection = new SqliteConnection(connectionString);
            await _connection.OpenAsync();

            // Initialize schema
            using (var cmd = _connection.CreateCommand())
            {
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
                    );";
                await cmd.ExecuteNonQueryAsync();
            }

            // Apply migrations
            try
            {
                var serviceProvider = Program.CreateServices(connectionString);
                using var scope = serviceProvider.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<FluentMigrator.Runner.IMigrationRunner>();
                runner.MigrateUp();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying migrations: {ex.Message}");
                // Continue even if migrations fail - the basic schema is already created
            }

            // Initialize UnitOfWork
            _unitOfWork = new UnitOfWork(_connection);
        }

        private SqliteOpenMode GetSqliteOpenMode(string mode)
        {
            return mode.ToLower() switch
            {
                "readonly" => SqliteOpenMode.ReadOnly,
                "readwrite" => SqliteOpenMode.ReadWrite,
                "readwritecreate" => SqliteOpenMode.ReadWriteCreate,
                "memory" => SqliteOpenMode.Memory,
                _ => SqliteOpenMode.ReadWriteCreate
            };
        }

        private SqliteCacheMode GetSqliteCacheMode(string cache)
        {
            return cache.ToLower() switch
            {
                "default" => SqliteCacheMode.Default,
                "private" => SqliteCacheMode.Private,
                "shared" => SqliteCacheMode.Shared,
                _ => SqliteCacheMode.Default
            };
        }

        /// <summary>
        /// Executes a function within a transaction.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="func">The function to execute.</param>
        /// <returns>The result of the function.</returns>
        public async Task<T> ExecuteInTransactionAsync<T>(Func<IUnitOfWork, Task<T>> func)
        {
            if (_unitOfWork is UnitOfWork unitOfWork)
            {
                return await unitOfWork.ExecuteInTransactionAsync(async () => await func(unitOfWork));
            }
            
            throw new InvalidOperationException("UnitOfWork is not properly initialized.");
        }

        /// <summary>
        /// Executes an action within a transaction.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public async Task ExecuteInTransactionAsync(Func<IUnitOfWork, Task> action)
        {
            if (_unitOfWork is UnitOfWork unitOfWork)
            {
                await unitOfWork.ExecuteInTransactionAsync(async () => await action(unitOfWork));
            }
            else
            {
                throw new InvalidOperationException("UnitOfWork is not properly initialized.");
            }
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
            if (_connection != null && _connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
        }

        /// <summary>
        /// Inserts a new task asynchronously.
        /// </summary>
        public async Task<int> InsertTaskAsync(string taskName)
        {
            return await Tasks.InsertTaskAsync(taskName);
        }

        /// <summary>
        /// Starts a new session asynchronously.
        /// </summary>
        public async Task<int> StartSessionAsync(int taskId, DateTime startTime, string? notes = null)
        {
            return await Sessions.InsertSessionAsync(taskId, startTime, notes);
        }

        /// <summary>
        /// Ends a session asynchronously.
        /// </summary>
        public async Task EndSessionAsync(int sessionId, DateTime endTime, string? notes = null)
        {
            await Sessions.UpdateSessionAsync(sessionId, endTime, notes);
        }

        /// <summary>
        /// Gets all tasks asynchronously.
        /// </summary>
        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            return await Tasks.GetAllTasksAsync();
        }

        /// <summary>
        /// Gets all sessions for a task asynchronously.
        /// </summary>
        public async Task<DataTable> GetSessionsForTaskAsync(int taskId)
        {
            var sessions = await Sessions.GetSessionsForTaskAsync(taskId);
            var table = new DataTable();
            
            // Create columns
            table.Columns.Add("Start Time", typeof(string));
            table.Columns.Add("End Time", typeof(string));
            table.Columns.Add("Duration", typeof(string));
            table.Columns.Add("Notes", typeof(string));
            
            // Add data
            foreach (var session in sessions)
            {
                var row = table.NewRow();
                row["Start Time"] = session.StartTime.ToString("g");
                row["End Time"] = session.EndTime.HasValue ? session.EndTime.Value.ToString("g") : "In Progress";
                row["Duration"] = session.Duration.HasValue ? session.Duration.Value.ToString(@"hh\:mm\:ss") : "In Progress";
                row["Notes"] = session.Notes ?? string.Empty;
                table.Rows.Add(row);
            }
            
            return table;
        }

        /// <summary>
        /// Gets a summary of all sessions asynchronously.
        /// </summary>
        public async Task<DataTable> GetSessionSummaryAsync()
        {
            return await Sessions.GetSessionSummaryAsync();
        }

        /// <summary>
        /// Gets all categories asynchronously.
        /// </summary>
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await Categories.GetAllCategoriesAsync();
        }

        /// <summary>
        /// Inserts a new category asynchronously.
        /// </summary>
        public async Task<int> InsertCategoryAsync(string name, string color)
        {
            return await Categories.InsertCategoryAsync(name, color);
        }

        /// <summary>
        /// Updates a task's category asynchronously.
        /// </summary>
        public async Task UpdateTaskCategoryAsync(int taskId, int? categoryId)
        {
            await Tasks.UpdateTaskCategoryAsync(taskId, categoryId);
        }

        /// <summary>
        /// Updates a task's tags asynchronously.
        /// </summary>
        public async Task UpdateTaskTagsAsync(int taskId, IEnumerable<string> tags)
        {
            await Tasks.UpdateTaskTagsAsync(taskId, tags);
        }

        /// <summary>
        /// Gets all tags for a task asynchronously.
        /// </summary>
        public async Task<List<string>> GetTagsForTaskAsync(int taskId)
        {
            return await Tasks.GetTagsForTaskAsync(taskId);
        }

        /// <summary>
        /// Gets tasks filtered by completion status.
        /// </summary>
        public async Task<List<TaskItem>> GetTasksByCompletionAsync(bool isComplete)
        {
            return await Tasks.GetTasksByCompletionAsync(isComplete);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _unitOfWork?.Dispose();
                    _connection?.Dispose();
                }
                _disposed = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_unitOfWork != null)
                await _unitOfWork.DisposeAsync();
            if (_connection != null)
                await _connection.DisposeAsync();
        }
    }
}
