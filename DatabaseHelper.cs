using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Polly;
using PricisApp.Core.Entities;
using PricisApp.Repositories;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Data.SQLite;
using Dapper;
using PricisApp.Models;
using PricisApp.Core.Enums;

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

        private UnitOfWork? _unitOfWork;

        public UnitOfWork UnitOfWork => _unitOfWork ?? throw new InvalidOperationException("Database not initialized");

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
            
            // Use current directory instead of LocalApplicationData to avoid permission issues
            var folder = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(folder);
            _dbPath = Path.Combine(folder, _appSettings.Database.FileName);
            InitializeDatabase().GetAwaiter().GetResult();
        }

        private async Task InitializeDatabase()
        {
            Console.WriteLine("Initializing database...");
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = GetSqliteOpenMode(_appSettings.Database.ConnectionString.Mode),
                Cache = GetSqliteCacheMode(_appSettings.Database.ConnectionString.Cache),
                Pooling = _appSettings.Database.ConnectionString.Pooling
            }.ToString();
            
            Console.WriteLine($"Database path: {_dbPath}");
            Console.WriteLine($"Connection string: {connectionString}");
            
            try
            {
                _connection = new SqliteConnection(connectionString);
                await _connection.OpenAsync();
                Console.WriteLine("Database connection opened");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR opening database connection: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }

            // Initialize schema
            Console.WriteLine("Creating database schema...");
            try
            {
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
                            State TEXT DEFAULT 'Stopped',
                            FOREIGN KEY(TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
                        );";
                    await cmd.ExecuteNonQueryAsync();
                }
                Console.WriteLine("Database schema created");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR creating database schema: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }

            // Apply migrations
            try
            {
                Console.WriteLine("Applying database migrations...");
                var serviceProvider = Program.CreateServices(connectionString);
                using var scope = serviceProvider.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<FluentMigrator.Runner.IMigrationRunner>();
                runner.MigrateUp();
                Console.WriteLine("Migrations applied successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying migrations: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error applying migrations: {ex.Message}");
                // Continue even if migrations fail - the basic schema is already created
            }

            // Initialize UnitOfWork
            Console.WriteLine("Initializing UnitOfWork...");
            _unitOfWork = new UnitOfWork(_connection);
            Console.WriteLine("UnitOfWork initialized");
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
        public async Task<T> ExecuteInTransactionAsync<T>(Func<UnitOfWork, Task<T>> func)
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
        public async Task ExecuteInTransactionAsync(Func<UnitOfWork, Task> action)
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
            var task = new PricisApp.Core.Entities.TaskItem { Name = taskName };
            return await Tasks.CreateAsync(task);
        }

        /// <summary>
        /// Starts a new session asynchronously.
        /// </summary>
        public async Task<int> StartSessionAsync(int taskId, DateTime startTime, string? notes = null, SessionState state = SessionState.Running)
        {
            return await Sessions.InsertSessionAsync(taskId, startTime, notes, state.ToString());
        }

        /// <summary>
        /// Ends a session asynchronously.
        /// </summary>
        public async Task EndSessionAsync(int sessionId, DateTime endTime, string? notes = null)
        {
            await Sessions.UpdateSessionAsync(sessionId, endTime, notes, SessionState.Stopped.ToString());
        }

        public async Task PauseSessionAsync(int sessionId)
        {
            await Sessions.UpdateSessionStateAsync(sessionId, SessionState.Paused.ToString());
        }

        public async Task ResumeSessionAsync(int sessionId)
        {
            await Sessions.UpdateSessionStateAsync(sessionId, SessionState.Running.ToString());
        }

        /// <summary>
        /// Gets all tasks asynchronously.
        /// </summary>
        public async Task<List<PricisApp.Core.Entities.TaskItem>> GetAllTasksAsync()
        {
            return (await Tasks.GetAllAsync()).ToList();
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

        public async Task<List<PricisApp.Models.Session>> GetAllSessionsAsync()
        {
            return await Sessions.GetAllSessionsAsync();
        }

        /// <summary>
        /// Gets all categories asynchronously.
        /// </summary>
        public async Task<List<PricisApp.Core.Entities.Category>> GetAllCategoriesAsync()
        {
            return (await Categories.GetAllAsync()).ToList();
        }

        /// <summary>
        /// Inserts a new category asynchronously.
        /// </summary>
        public async Task<int> InsertCategoryAsync(string name, string color)
        {
            var category = new PricisApp.Core.Entities.Category { Name = name, Color = color };
            return await Categories.CreateAsync(category);
        }

        /// <summary>
        /// Updates a task's category asynchronously.
        /// </summary>
        public async Task UpdateTaskCategoryAsync(int taskId, int? categoryId)
        {
            var task = await Tasks.GetByIdAsync(taskId);
            if (task != null)
            {
                task.CategoryId = categoryId;
                await Tasks.UpdateAsync(task);
            }
        }

        /// <summary>
        /// Updates a task's tags asynchronously.
        /// </summary>
        public async Task UpdateTaskTagsAsync(int taskId, IEnumerable<string> tags)
        {
            var task = await Tasks.GetByIdAsync(taskId);
            if (task != null)
            {
                task.Tags = tags.ToList();
                await Tasks.UpdateAsync(task);
            }
        }

        /// <summary>
        /// Gets all tags for a task asynchronously.
        /// </summary>
        public async Task<List<string>> GetTagsForTaskAsync(int taskId)
        {
            var task = await Tasks.GetByIdAsync(taskId);
            return task?.Tags?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// Gets tasks filtered by completion status.
        /// </summary>
        public async Task<List<PricisApp.Core.Entities.TaskItem>> GetTasksByCompletionAsync(bool isComplete)
        {
            var allTasks = await Tasks.GetAllAsync();
            return allTasks.Where(t => t.IsComplete == isComplete).ToList();
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

        public void ExecuteWithRetry(Action<SqliteConnection> action)
        {
            var policy = Policy
                .Handle<SqliteException>(ex => ex.SqliteErrorCode == 5 || // SQLITE_BUSY
                                              ex.SqliteErrorCode == 6)    // SQLITE_LOCKED
                .WaitAndRetry(3, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            policy.Execute(() =>
            {
                using (var conn = new SqliteConnection(_connection?.ConnectionString))
                {
                    conn.Open();
                    action(conn);
                }
            });
        }

        public List<PricisApp.Models.Product> GetAllProducts()
        {
            var products = new List<PricisApp.Models.Product>();
            ExecuteWithRetry(conn => 
            {
                using (var cmd = new SqliteCommand("SELECT * FROM Products", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new PricisApp.Models.Product
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            // ... other properties
                        });
                    }
                }
            });
            return products;
        }

        public SqliteConnection GetSqliteConnection()
        {
            var connection = new SqliteConnection(_connection?.ConnectionString);
            // Now open the connection with retry
            OpenSqliteConnectionWithRetry(connection);
            return connection;
        }

        private void OpenSqliteConnectionWithRetry(SqliteConnection connection)
        {
            int maxRetries = 3;
            int baseDelay = 1000; // 1 second

            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    connection.Open();
                    return;
                }
                catch (SqliteException ex)
                {
                    if (retry == maxRetries - 1)
                        throw;

                    int delay = baseDelay * (int)Math.Pow(2, retry);
                    Thread.Sleep(delay);
                }
            }
        }

        public void UseDapper()
        {
            using (var connection = new SqliteConnection("Data Source=TimeTracking.db"))
            {
                var tasks = Dapper.SqlMapper.Query<PricisApp.Core.Entities.TaskItem>(connection, "SELECT * FROM Tasks").ToList();
            }
        }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<PricisApp.Core.Entities.Category> Categories { get; set; }
        public DbSet<PricisApp.Core.Entities.TaskItem> Tasks { get; set; }
        public DbSet<PricisApp.Core.Entities.Session> Sessions { get; set; }
        // You may need a TaskTag entity for TaskTags table

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PricisApp.Core.Entities.Category>().ToTable("Categories");
            modelBuilder.Entity<PricisApp.Core.Entities.TaskItem>().ToTable("Tasks");
            modelBuilder.Entity<PricisApp.Core.Entities.Session>().ToTable("Sessions");
            // Configure TaskTags composite key, etc.
        }
    }
}
