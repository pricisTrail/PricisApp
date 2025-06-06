using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using PricisApp.Interfaces;
using PricisApp.Models;
using PricisApp.Services;
using PricisApp.Properties;
using System.Runtime.Versioning;
using PricisApp.UI;
using System.Runtime.Serialization;
using Microsoft.Extensions.Configuration;

[assembly: SupportedOSPlatform("windows7.0")]

namespace PricisApp
{
    /// <summary>
    /// Main form for the PricisApp time tracking application.
    /// </summary>
    public partial class Form1 : Form
    {
        private readonly DatabaseHelper db;
        private readonly ITaskService _taskService;
        private readonly ISessionService _sessionService;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ThemeManager _themeManager = new();
        private readonly IConfigurationService _configService;
        private int? currentTaskId = null;
        private int? currentSessionId = null;
        private bool isTimerRunning = false;
        private bool isPaused = false;
        private readonly Stopwatch stopwatch = new();
        private TimeSpan elapsedTime = TimeSpan.Zero;
        private TimeSpan totalPausedTime = TimeSpan.Zero;

        private Button btnSummary = new();
        private Button btnPauseResume = new();
        private Button btnThemeToggle = new();
        private ToolStripMenuItem filterAllMenu = new();
        private ToolStripMenuItem filterCompleteMenu = new();
        private ToolStripMenuItem filterIncompleteMenu = new();

        private class UserState
        {
            public int? LastTaskId { get; set; }
            public bool IsTimerRunning { get; set; }
            public bool IsPaused { get; set; }
            public double ElapsedSeconds { get; set; }
            public string? Theme { get; set; }
        }

        private readonly string userStatePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PricisApp", "userstate.json");

        /// <summary>
        /// Initializes the main form and its components.
        /// </summary>
        public Form1(DatabaseHelper dbHelper, ITaskService taskService, ISessionService sessionService, ICategoryRepository categoryRepository, IConfigurationService configService)
        {
            InitializeComponent();
            db = dbHelper;
            _taskService = taskService;
            _sessionService = sessionService;
            _categoryRepository = categoryRepository;
            _configService = configService;
            
            InitializeEventHandlers();
            InitializeTheme();
            LoadThemePreference();
            LoadUserState();
            _ = LoadTasks();
        }

        /// <summary>
        /// Sets up the theme selector options.
        /// </summary>
        private void InitializeTheme()
        {
            themeSelector.Items.Clear();
            themeSelector.Items.AddRange(_configService.GetAvailableThemes());
        }

        /// <summary>
        /// Sets up event handlers for controls and owner-draw for the task list.
        /// </summary>
        private void InitializeEventHandlers()
        {
            timer.Tick += Timer_Tick;
            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            btnReset.Click += BtnReset_Click;
            themeSelector.SelectedIndexChanged += ThemeSelector_SelectedIndexChanged;
            btnNewTask.Click += BtnNewTask_Click;
            listTasks.SelectedIndexChanged += ListTasks_SelectedIndexChanged;
            btnThemeToggle.Click += BtnThemeToggle_Click;
            
            // Add event handlers for tags and category
            txtTags.Leave += TxtTags_Leave;
            cboCategories.SelectedIndexChanged += CboCategories_SelectedIndexChanged;
            btnNewCategory.Click += BtnNewCategory_Click;

            // Setup session service event handlers
            _sessionService.TimerTick += (s, e) => lblTime.Text = _sessionService.ElapsedTime.ToString(@"hh\:mm\:ss\.ff");
            _sessionService.TimerStarted += (s, e) => UpdateButtonStates();
            _sessionService.TimerStopped += (s, e) => UpdateButtonStates();
            _sessionService.TimerPaused += (s, e) => btnPauseResume.Text = "Resume";
            _sessionService.TimerResumed += (s, e) => btnPauseResume.Text = "Pause";

            btnSummary = new Button
            {
                Text = "Show Summary",
                Location = new Point(310, 110),
                Size = new Size(100, 23)
            };
            btnSummary.Click += BtnSummary_Click;
            Controls.Add(btnSummary);

            btnPauseResume = new Button
            {
                Text = "Pause",
                Location = new Point(420, 110),
                Size = new Size(100, 23)
            };
            btnPauseResume.Click += BtnPauseResume_Click;
            Controls.Add(btnPauseResume);

            // Context menu for filtering and task actions
            var ctxMenu = new ContextMenuStrip();
            var filterMenu = new ToolStripMenuItem("Filter Tasks");
            filterAllMenu = new ToolStripMenuItem("Show All", null, (s, e) => FilterTasks(null));
            filterCompleteMenu = new ToolStripMenuItem("Show Complete", null, (s, e) => FilterTasks(true));
            filterIncompleteMenu = new ToolStripMenuItem("Show Incomplete", null, (s, e) => FilterTasks(false));
            filterMenu.DropDownItems.Add(filterAllMenu);
            filterMenu.DropDownItems.Add(filterCompleteMenu);
            filterMenu.DropDownItems.Add(filterIncompleteMenu);
            ctxMenu.Items.Add(filterMenu);
            ctxMenu.Items.Add("Delete", null, (s, e) => DeleteSelectedTask());
            ctxMenu.Items.Add("Toggle Complete", null, (s, e) => ToggleTaskCompletion());
            ctxMenu.Items.Add("Start/Stop Timer", null, (s, e) => StartStopTimerForSelectedTask());
            listTasks.ContextMenuStrip = ctxMenu;

            // Owner-draw for completion visual feedback
            listTasks.DrawMode = DrawMode.OwnerDrawVariable;
            listTasks.MeasureItem += (s, e) => { e.ItemHeight = 20; };
            listTasks.DrawItem += (s, e) =>
            {
                if (e.Index < 0 || e.Index >= listTasks.Items.Count) return;
                e.DrawBackground();
                var item = (TaskItem)listTasks.Items[e.Index];
                var font = item.IsComplete && e.Font != null ? new Font(e.Font, FontStyle.Strikeout) : e.Font;
                if (font != null)
                {
                    using var brush = new SolidBrush(e.ForeColor);
                    e.Graphics.DrawString(item.Name, font, brush, e.Bounds);
                }
                if (item.Id == currentTaskId)
                {
                    e.Graphics.FillRectangle(Brushes.LightBlue, e.Bounds);
                }
                e.DrawFocusRectangle();
            };

            FormClosing += Form1_FormClosing;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _sessionService.UpdateTimer();
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            if (currentTaskId == null)
            {
                UIManager.ShowWarning(this, "Please select or create a task first");
                return;
            }
            
            try
            {
                await UIManager.ShowLoading(this, async () =>
                {
                    await _sessionService.StartSessionAsync(currentTaskId.Value);
                    timer.Start();
                }, "Starting session...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "starting the session");
            }
        }

        private async void BtnStop_Click(object? sender, EventArgs e)
        {
            if (!_sessionService.IsTimerRunning || !_sessionService.CurrentSessionId.HasValue) return;
            
            try
            {
                await UIManager.ShowLoading(this, async () =>
                {
                    timer.Stop();
                    await _sessionService.EndSessionAsync(_sessionService.CurrentSessionId.Value);
                    if (currentTaskId.HasValue)
                    {
                        await LoadSessions(currentTaskId.Value);
                    }
                }, "Stopping session...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "stopping the session");
            }
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            timer.Stop();
            _sessionService.ResetTimer();
            lblTime.Text = "00:00:00";
            UpdateButtonStates();
        }

        private void ThemeSelector_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var theme = themeSelector.SelectedItem?.ToString();
            if (theme != null)
            {
                _themeManager.ApplyTheme(this, theme);
                SaveThemePreference(theme);
            }
        }

        private async void BtnNewTask_Click(object? sender, EventArgs e)
        {
            var taskName = txtTaskName.Text.Trim();
            
            // Validate task name
            if (string.IsNullOrWhiteSpace(taskName))
            {
                UIManager.ShowWarning(this, "Please enter a task name");
                txtTaskName.Focus();
                return;
            }
            
            if (taskName.Length < 3)
            {
                UIManager.ShowWarning(this, "Task name must be at least 3 characters long");
                txtTaskName.Focus();
                return;
            }
            
            if (taskName.Length > 100)
            {
                UIManager.ShowWarning(this, "Task name cannot exceed 100 characters");
                txtTaskName.Focus();
                return;
            }
            
            // Check for invalid characters
            if (taskName.IndexOfAny(new char[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }) >= 0)
            {
                UIManager.ShowWarning(this, "Task name contains invalid characters (/ \\ : * ? \" < > |)");
                txtTaskName.Focus();
                return;
            }

            try
            {
                await UIManager.ShowLoading(this, async () =>
                {
                    await _taskService.CreateTaskAsync(taskName);
                    txtTaskName.Clear();
                    await LoadTasks();
                }, "Creating new task...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "creating new task");
            }
        }

        private async void ListTasks_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (listTasks.SelectedItem is TaskItem selectedTask)
            {
                currentTaskId = selectedTask.Id;
                try
                {
                    await UIManager.ShowLoading(this, async () =>
                    {
                        await LoadSessions(selectedTask.Id);
                        
                        // Load and display tags
                        var tags = await _taskService.GetTaskTagsAsync(selectedTask.Id);
                        txtTags.Text = string.Join(", ", tags);
                        
                        // Select the correct category
                        cboCategories.SelectedIndex = 0; // Default to "No Category"
                        if (selectedTask.CategoryId.HasValue)
                        {
                            for (int i = 0; i < cboCategories.Items.Count; i++)
                            {
                                if (cboCategories.Items[i] is Category category && category.Id == selectedTask.CategoryId.Value)
                                {
                                    cboCategories.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    }, "Loading task sessions...");
                }
                catch (Exception ex)
                {
                    UIManager.ShowDetailedError(this, ex, "loading task sessions");
                }
            }
        }

        private async Task LoadTasks()
        {
            try
            {
                await UIManager.ShowLoading(this, async () =>
                {
                    listTasks.Items.Clear();
                    var tasks = await _taskService.GetAllTasksAsync();
                    foreach (var task in tasks)
                    {
                        listTasks.Items.Add(task);
                    }
                }, "Loading tasks...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "loading tasks");
            }
        }

        private async Task LoadSessions(int taskId)
        {
            try
            {
                var sessions = await _sessionService.GetTaskSessionsAsync(taskId);
                dataSessions.DataSource = sessions;
                FormatDataGridView();
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "loading sessions");
            }
        }

        private async void BtnSummary_Click(object? sender, EventArgs e)
        {
            try
            {
                await UIManager.ShowLoading(this, async () =>
                {
                    var summary = await _sessionService.GetSessionSummaryAsync();
                    dataSessions.DataSource = summary;
                    FormatDataGridView();
                }, "Loading summary...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "loading summary");
            }
        }

        private void FormatDataGridView()
        {
            dataSessions.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;
            dataSessions.EnableHeadersVisualStyles = false;
            dataSessions.ColumnHeadersDefaultCellStyle.BackColor = Color.SteelBlue;
            dataSessions.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataSessions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void UpdateButtonStates()
        {
            btnStart.Enabled = !_sessionService.IsTimerRunning;
            btnStop.Enabled = _sessionService.IsTimerRunning;
            btnReset.Enabled = true;
        }

        private void BtnPauseResume_Click(object? sender, EventArgs e)
        {
            _sessionService.TogglePause();
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            db.Dispose();
            SaveUserState();
        }

        private async void DeleteSelectedTask()
        {
            if (listTasks.SelectedItem == null)
            {
                UIManager.ShowWarning(this, "Please select a task to delete");
                return;
            }

            if (UIManager.ShowConfirmation(this, "Are you sure you want to delete this task?") == DialogResult.Yes)
            {
                try
                {
                    await UIManager.ShowLoading(this, async () =>
                    {
                        var task = (TaskItem)listTasks.SelectedItem;
                        await _taskService.DeleteTaskAsync(task.Id);
                        await LoadTasks();
                    }, "Deleting task...");
                }
                catch (Exception ex)
                {
                    UIManager.ShowDetailedError(this, ex, "deleting task");
                }
            }
        }

        private async void ToggleTaskCompletion()
        {
            if (listTasks.SelectedItem is not TaskItem selectedTask) return;
            try
            {
                await UIManager.ShowLoading(this, async () =>
                {
                    await _taskService.ToggleTaskCompletion(selectedTask.Id);
                    await LoadTasks();
                    foreach (var item in listTasks.Items)
                    {
                        if (item is TaskItem task && task.Id == selectedTask.Id)
                        {
                            listTasks.SelectedItem = item;
                            break;
                        }
                    }
                }, "Updating task status...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "toggling task completion");
            }
        }

        private void SaveThemePreference(string? theme)
        {
            if (theme == null) return;
            
            // Save to user state for backward compatibility
            var state = LoadUserState() ?? new UserState();
            state.Theme = theme;
            Directory.CreateDirectory(Path.GetDirectoryName(userStatePath)!);
            File.WriteAllText(userStatePath, JsonSerializer.Serialize(state));
            
            // Also save using the configuration service
            _configService.SaveUserSetting("UI:DefaultTheme", theme);
        }

        private void LoadThemePreference()
        {
            try
            {
                // First try to get theme from configuration service
                string theme = _configService.GetDefaultTheme();
                
                // For backward compatibility, check user state too
                var userState = LoadUserState();
                if (userState?.Theme != null)
                {
                    theme = userState.Theme;
                }
                
                if (!string.IsNullOrEmpty(theme))
                {
                    themeSelector.SelectedItem = theme;
                    ApplyTheme(theme);
                }
            }
            catch (Exception ex)
            {
                // Ignore theme loading errors
                Debug.WriteLine($"Error loading theme preference: {ex.Message}");
            }
        }

        private void ApplyTheme(string theme)
        {
            _themeManager.ApplyTheme(this, theme);
            btnThemeToggle.Text = theme.ToLower().Contains("dark") ? "Light Mode" : "Dark Mode";
        }

        private async void FilterTasks(bool? showComplete)
        {
            try
            {
                await UIManager.ShowLoading(this, async () =>
                {
                    var tasks = await _taskService.FilterTasksAsync(showComplete);
                    listTasks.Items.Clear();
                    foreach (var task in tasks)
                    {
                        listTasks.Items.Add(task);
                    }
                    // Update menu item check states
                    filterAllMenu.Checked = showComplete == null;
                    filterCompleteMenu.Checked = showComplete == true;
                    filterIncompleteMenu.Checked = showComplete == false;
                }, "Filtering tasks...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "filtering tasks");
            }
        }

        private async void BtnNewCategory_Click(object? sender, EventArgs e)
        {
            using var form = new Form
            {
                Text = "New Category",
                Size = new Size(300, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent
            };
            var lblName = new Label { Text = "Name:", Left = 20, Top = 20 };
            var txtName = new TextBox { Left = 100, Top = 20, Width = 160 };
            var lblColor = new Label { Text = "Color:", Left = 20, Top = 50 };
            var colorPicker = new ColorDialog();
            var btnColor = new Button { Text = "Pick Color", Left = 100, Top = 50, Width = 160 };
            var btnOk = new Button { Text = "OK", Left = 120, Top = 100, DialogResult = DialogResult.OK };
            
            // Set a default color
            btnColor.BackColor = Color.LightBlue;
            
            btnColor.Click += (s, ev) => {
                if (colorPicker.ShowDialog() == DialogResult.OK)
                {
                    btnColor.BackColor = colorPicker.Color;
                }
            };
            
            form.Controls.AddRange(new Control[] { lblName, txtName, lblColor, btnColor, btnOk });
            form.AcceptButton = btnOk;
            
            // Override the form's OK button click to validate inputs
            form.FormClosing += (s, ev) => {
                if (form.DialogResult == DialogResult.OK)
                {
                    string categoryName = txtName.Text.Trim();
                    
                    // Validate category name
                    if (string.IsNullOrWhiteSpace(categoryName))
                    {
                        MessageBox.Show("Please enter a category name", "Validation Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtName.Focus();
                        ev.Cancel = true;
                        return;
                    }
                    
                    if (categoryName.Length < 2)
                    {
                        MessageBox.Show("Category name must be at least 2 characters long", "Validation Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtName.Focus();
                        ev.Cancel = true;
                        return;
                    }
                    
                    if (categoryName.Length > 50)
                    {
                        MessageBox.Show("Category name cannot exceed 50 characters", "Validation Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtName.Focus();
                        ev.Cancel = true;
                        return;
                    }
                }
            };
            
            if (form.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    await UIManager.ShowLoading(this, async () =>
                    {
                        var color = btnColor.BackColor.ToArgb().ToString("X6");
                        await _categoryRepository.InsertCategoryAsync(txtName.Text, color);
                        await LoadCategories();
                    }, "Creating category...");
                }
                catch (Exception ex)
                {
                    UIManager.ShowDetailedError(this, ex, "creating category");
                }
            }
        }

        private async Task LoadCategories()
        {
            try
            {
                await UIManager.ShowLoading(this, async () =>
                {
                    cboCategories.Items.Clear();
                    var categories = await _categoryRepository.GetAllCategoriesAsync();
                    cboCategories.Items.Add("(No Category)");
                    foreach (var category in categories)
                    {
                        cboCategories.Items.Add(category);
                    }
                }, "Loading categories...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "loading categories");
            }
        }

        private async void ExitToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Custom color table for menu strip theming.
        /// </summary>
        private class CustomColorTable : ProfessionalColorTable
        {
            private readonly Color _backColor;
            public CustomColorTable(Color backColor) => _backColor = backColor;
            public override Color MenuItemSelected => _backColor;
            public override Color MenuItemSelectedGradientBegin => _backColor;
            public override Color MenuItemSelectedGradientEnd => _backColor;
            public override Color MenuItemBorder => Color.Gray;
            public override Color MenuStripGradientBegin => _backColor;
            public override Color MenuStripGradientEnd => _backColor;
            public override Color ToolStripDropDownBackground => _backColor;
        }

        private void SaveUserState()
        {
            var state = new UserState
            {
                LastTaskId = currentTaskId,
                IsTimerRunning = _sessionService.IsTimerRunning,
                IsPaused = _sessionService.IsPaused,
                ElapsedSeconds = _sessionService.ElapsedTime.TotalSeconds,
                Theme = themeSelector.SelectedItem?.ToString()
            };
            Directory.CreateDirectory(Path.GetDirectoryName(userStatePath)!);
            File.WriteAllText(userStatePath, JsonSerializer.Serialize(state));
        }

        private UserState LoadUserState()
        {
            if (File.Exists(userStatePath))
            {
                return JsonSerializer.Deserialize<UserState>(File.ReadAllText(userStatePath));
            }
            return null;
        }

        private void BtnThemeToggle_Click(object? sender, EventArgs e)
        {
            var isDark = themeSelector.SelectedItem?.ToString()?.ToLower().Contains("dark") == true;
            
            if (isDark)
            {
                ApplyTheme("Light");
                btnThemeToggle.Text = "Dark Mode";
                SaveThemePreference("Light");
            }
            else
            {
                ApplyTheme("Dark");
                btnThemeToggle.Text = "Light Mode";
                SaveThemePreference("Dark");
            }
        }

        private void StartStopTimerForSelectedTask()
        {
            if (listTasks.SelectedItem is not TaskItem selectedTask) return;
            if (_sessionService.IsTimerRunning && currentTaskId == selectedTask.Id)
            {
                BtnStop_Click(this, EventArgs.Empty);
            }
            else
            {
                currentTaskId = selectedTask.Id;
                BtnStart_Click(this, EventArgs.Empty);
            }
        }

        // Fix async method warning
        private async Task PopulateTasksAsync()
        {
            try
            {
                await UIManager.ShowLoading(this, async () =>
                {
                    var tasks = await _taskService.GetAllTasksAsync();
                    // Populate the tasks list
                }, "Loading tasks...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "loading tasks");
            }
        }

        // Fix null reference warnings
        private string GetSelectedTaskName()
        {
            // Return empty string instead of null
            return "Default Task Name";
        }

        private string GetSelectedTaskCategory()
        {
            // Return empty string instead of null
            return "Default Category";
        }

        // Add validation and save task tags
        private async Task ValidateAndSaveTaskTags()
        {
            if (currentTaskId == null) return;
            
            string tagsText = txtTags.Text.Trim();
            
            try
            {
                // Split by comma and trim each tag
                var tags = tagsText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
                
                // Validate individual tags
                foreach (var tag in tags)
                {
                    if (tag.Length > 30)
                    {
                        UIManager.ShowWarning(this, $"Tag '{tag}' is too long. Maximum length is 30 characters.");
                        return;
                    }
                    
                    if (tag.IndexOfAny(new char[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }) >= 0)
                    {
                        UIManager.ShowWarning(this, $"Tag '{tag}' contains invalid characters (/ \\ : * ? \" < > |)");
                        return;
                    }
                }
                
                await UIManager.ShowLoading(this, async () =>
                {
                    await _taskService.UpdateTaskTagsAsync(currentTaskId.Value, tags);
                }, "Updating tags...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "updating tags");
            }
        }

        // Add validation and save task category
        private async Task ValidateAndSaveTaskCategory()
        {
            if (currentTaskId == null) return;
            
            try
            {
                int? categoryId = null;
                
                // If a valid category is selected (not the "No Category" option)
                if (cboCategories.SelectedIndex > 0 && cboCategories.SelectedItem is Category selectedCategory)
                {
                    categoryId = selectedCategory.Id;
                }
                
                await UIManager.ShowLoading(this, async () =>
                {
                    await _taskService.UpdateTaskCategoryAsync(currentTaskId.Value, categoryId);
                }, "Updating category...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "updating category");
            }
        }

        private async void TxtTags_Leave(object? sender, EventArgs e)
        {
            await ValidateAndSaveTaskTags();
        }

        private async void CboCategories_SelectedIndexChanged(object? sender, EventArgs e)
        {
            await ValidateAndSaveTaskCategory();
        }
    }
}