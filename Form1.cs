using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using PricisApp.Properties;
using System.Runtime.Versioning;
using PricisApp.UI;

[assembly: SupportedOSPlatform("windows7.0")]

namespace PricisApp
{
    /// <summary>
    /// Main form for the PricisApp time tracking application.
    /// </summary>
    public partial class Form1 : Form
    {
        private readonly DatabaseHelper db;
        private readonly ThemeManager _themeManager = new();
        private int? currentTaskId = null;
        private int? currentSessionId = null;
        private bool isTimerRunning = false;
        private bool isPaused = false;
        private readonly Stopwatch stopwatch = new();
        private TimeSpan elapsedTime = TimeSpan.Zero;
        private TimeSpan totalPausedTime = TimeSpan.Zero;

        private Button btnSummary = new();
        private Button btnPauseResume = new();
        private ToolStripMenuItem filterAllMenu = new();
        private ToolStripMenuItem filterCompleteMenu = new();
        private ToolStripMenuItem filterIncompleteMenu = new();

        /// <summary>
        /// Initializes the main form and its components.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            db = new DatabaseHelper();
            InitializeEventHandlers();
            InitializeTheme();
            LoadThemePreference();
            _ = LoadTasks();
        }

        /// <summary>
        /// Sets up the theme selector options.
        /// </summary>
        private void InitializeTheme()
        {
            themeSelector.Items.Clear();
            themeSelector.Items.AddRange(new object[] {
                "System Default",
                "Light",
                "Dark",
                "Blue",
                "Green",
                "High Contrast"
            });
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
                e.DrawFocusRectangle();
            };

            FormClosing += Form1_FormClosing;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (isTimerRunning && !isPaused)
            {
                elapsedTime = stopwatch.Elapsed - totalPausedTime;
                lblTime.Text = elapsedTime.ToString(@"hh\:mm\:ss\.ff");
            }
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
                    stopwatch.Restart();
                    totalPausedTime = TimeSpan.Zero;
                    isPaused = false;
                    timer.Start();
                    isTimerRunning = true;
                    currentSessionId = await db.StartSessionAsync(currentTaskId.Value, DateTime.Now);
                    UpdateButtonStates();
                }, "Starting session...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "starting the session");
            }
        }

        private async void BtnStop_Click(object? sender, EventArgs e)
        {
            if (!isTimerRunning || !currentSessionId.HasValue) return;
            try
            {
                await UIManager.ShowLoading(this, async () =>
                {
                    timer.Stop();
                    isTimerRunning = false;
                    await db.EndSessionAsync(currentSessionId.Value, DateTime.Now);
                    currentSessionId = null;
                    UpdateButtonStates();
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
            isTimerRunning = false;
            elapsedTime = TimeSpan.Zero;
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
            if (string.IsNullOrWhiteSpace(taskName))
            {
                UIManager.ShowWarning(this, "Please enter a task name.");
                return;
            }

            try
            {
                await UIManager.ShowLoading(this, async () =>
                {
                    await db.AddTaskAsync(taskName);
                    txtTaskName.Clear();
                    await LoadTasks();
                }, "Creating new task...");
            }
            catch (Exception ex)
            {
                UIManager.ShowDetailedError(this, ex, "creating a new task");
            }
        }

        private async void ListTasks_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (listTasks.SelectedItem is TaskItem selectedTask)
            {
                currentTaskId = selectedTask.Id;
                await LoadSessions(selectedTask.Id);
            }
        }

        private async Task LoadTasks()
        {
            try
            {
                listTasks.Items.Clear();
                var tasks = await db.GetAllTasksAsync();
                foreach (var task in tasks)
                {
                    listTasks.Items.Add(task);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tasks: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadSessions(int taskId)
        {
            try
            {
                var sessions = await db.GetSessionsForTaskAsync(taskId);
                dataSessions.DataSource = sessions;
                FormatDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sessions: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnSummary_Click(object? sender, EventArgs e)
        {
            try
            {
                var summary = await db.GetSessionSummaryAsync();
                dataSessions.DataSource = summary;
                FormatDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading summary: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            btnStart.Enabled = !isTimerRunning;
            btnStop.Enabled = isTimerRunning;
            btnReset.Enabled = true;
        }

        private void BtnPauseResume_Click(object? sender, EventArgs e)
        {
            if (!isTimerRunning) return;
            if (!isPaused)
            {
                stopwatch.Stop();
                isPaused = true;
                btnPauseResume.Text = "Resume";
            }
            else
            {
                stopwatch.Start();
                isPaused = false;
                btnPauseResume.Text = "Pause";
            }
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            db.Dispose();
        }

        private async void DeleteSelectedTask()
        {
            if (listTasks.SelectedItem is not TaskItem selectedTask) return;
            var confirm = MessageBox.Show($"Delete '{selectedTask.Name}' and all its sessions?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;
            try
            {
                using var cmd = db.Connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Tasks WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", selectedTask.Id);
                await cmd.ExecuteNonQueryAsync();
                listTasks.Items.Remove(selectedTask);
                dataSessions.DataSource = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Deletion failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ToggleTaskCompletion()
        {
            if (listTasks.SelectedItem is not TaskItem selectedTask) return;
            try
            {
                var newState = !await IsTaskComplete(selectedTask.Id);
                using var cmd = db.Connection.CreateCommand();
                cmd.CommandText = "UPDATE Tasks SET IsComplete = $state WHERE Id = $id";
                cmd.Parameters.AddWithValue("$state", newState ? 1 : 0);
                cmd.Parameters.AddWithValue("$id", selectedTask.Id);
                await cmd.ExecuteNonQueryAsync();
                await LoadTasks();
                foreach (var item in listTasks.Items)
                {
                    if (item is TaskItem task && task.Id == selectedTask.Id)
                    {
                        listTasks.SelectedItem = item;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling completion: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<bool> IsTaskComplete(int taskId)
        {
            using var cmd = db.Connection.CreateCommand();
            cmd.CommandText = "SELECT IsComplete FROM Tasks WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", taskId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 1;
        }

        private void SaveThemePreference(string? theme)
        {
            if (theme == null) return;
            Settings.Default.Theme = theme;
            Settings.Default.Save();
        }

        private void LoadThemePreference()
        {
            if (!string.IsNullOrEmpty(Settings.Default.Theme) && themeSelector.Items.Contains(Settings.Default.Theme))
            {
                themeSelector.SelectedItem = Settings.Default.Theme;
                // Apply the theme immediately
                switch (Settings.Default.Theme)
                {
                    case "Light":
                        ApplyTheme(SystemColors.Control, Color.Black);
                        break;
                    case "Dark":
                        ApplyTheme(Color.FromArgb(32, 32, 32), Color.White);
                        break;
                    default:
                        ApplyTheme(DefaultBackColor, DefaultForeColor);
                        break;
                }
            }
            else
            {
                themeSelector.SelectedIndex = 0; // Default to "System Default"
            }
        }

        private async void FilterTasks(bool? showComplete)
        {
            try
            {
                var tasks = await db.GetAllTasksAsync();
                listTasks.Items.Clear();
                foreach (var task in tasks)
                {
                    if (showComplete == null || task.IsComplete == showComplete)
                    {
                        listTasks.Items.Add(task);
                    }
                }
                // Update menu item check states
                filterAllMenu.Checked = showComplete == null;
                filterCompleteMenu.Checked = showComplete == true;
                filterIncompleteMenu.Checked = showComplete == false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering tasks: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            btnColor.Click += (s, ev) => {
                if (colorPicker.ShowDialog() == DialogResult.OK)
                {
                    btnColor.BackColor = colorPicker.Color;
                }
            };
            form.Controls.AddRange(new Control[] { lblName, txtName, lblColor, btnColor, btnOk });
            form.AcceptButton = btnOk;
            if (form.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var color = btnColor.BackColor.ToArgb().ToString("X6");
                    await db.InsertCategoryAsync(txtName.Text, color);
                    await LoadCategories();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating category: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task LoadCategories()
        {
            try
            {
                cboCategories.Items.Clear();
                var categories = await db.GetAllCategoriesAsync();
                cboCategories.Items.Add("(No Category)");
                foreach (var category in categories)
                {
                    cboCategories.Items.Add(category);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading categories: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }
}