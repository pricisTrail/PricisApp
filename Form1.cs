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
using PricisApp.Core.Interfaces;
using PricisApp.Core.Entities;
using PricisApp.Services;
using PricisApp.Properties;
using System.Runtime.Versioning;
using PricisApp.UI;
using System.Runtime.Serialization;
using Microsoft.Extensions.Configuration;
using PricisApp.ViewModels;
using MaterialSkin;
using MaterialSkin.Controls;
using ScottPlot;
using PricisApp.Interfaces;

namespace PricisApp
{
    /// <summary>
    /// Main form for the PricisApp time tracking application.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class Form1 : MaterialForm
    {
        private readonly MainViewModel _viewModel;
        private readonly MaterialSkinManager _materialSkinManager;
        private readonly BindingSource _categoriesBindingSource = new();
        private readonly BindingSource _sessionsBindingSource = new();
        private ComboBox? cboIncompleteSessions;
        private MaterialButton? btnResumeSession;

        /// <summary>
        /// Initializes the main form and its components.
        /// </summary>
        public Form1(DatabaseHelper dbHelper, ITaskService taskService, ISessionService sessionService, 
                    ICategoryRepository categoryRepository, IConfigurationService configService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            
            // Initialize MaterialSkinManager
            _materialSkinManager = MaterialSkinManager.Instance;
            _materialSkinManager.AddFormToManage(this);
            
            // Initialize the view model
            _viewModel = new MainViewModel(taskService, sessionService, categoryRepository, configService, serviceProvider, this);
            
            // Subscribe to collection changes for live updates
            _viewModel.Tasks.CollectionChanged += (s, e) => UpdateTaskList();
            
            // Setup UI
            InitializeControls();
            InitializeEventHandlers();
            InitializeDataBindings();

            // Apply the initial theme
            ApplyTheme();

            CenterTimerPanel();
            this.Resize += (s, e) => CenterTimerPanel();
        }

        /// <summary>
        /// Sets up the UI controls that are created dynamically.
        /// </summary>
        private void InitializeControls()
        {
            // Initialize binding sources
            _categoriesBindingSource.DataSource = _viewModel.Categories;
            _sessionsBindingSource.DataSource = _viewModel.Sessions;
            
            // Initialize theme selector
            themeSelector.DataSource = _viewModel.AvailableThemes;
            themeSelector.SelectedItem = _viewModel.CurrentTheme;

            // Create a FlowLayoutPanel for action buttons
            var actionButtonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 70, // Adjusted for potential two rows or taller controls
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true, // Allow wrapping
                Padding = new Padding(5)
            };

            // Create additional buttons and add them to the actionButtonsPanel
            var btnSummary = new MaterialButton
            {
                Text = "Show Summary",
                Size = new Size(120, 36) // Standard MaterialButton size
            };
            btnSummary.Click += (s, e) => _viewModel.ShowSummaryCommand.Execute(null);
            actionButtonsPanel.Controls.Add(btnSummary);

            var btnDashboard = new MaterialButton
            {
                Text = "Dashboard",
                Size = new Size(120, 36)
            };
            btnDashboard.Click += (s, e) => _viewModel.ShowDashboardCommand.Execute(null);
            actionButtonsPanel.Controls.Add(btnDashboard);

            var btnPauseResume = new MaterialButton
            {
                Text = "Pause", // Initial text, will be updated
                Size = new Size(100, 36)
            };
            btnPauseResume.Click += (s, e) => _viewModel.PauseResumeCommand.Execute(null);
            actionButtonsPanel.Controls.Add(btnPauseResume);

            var btnThemeToggle = new MaterialButton
            {
                Text = "Toggle Theme",
                Size = new Size(130, 36)
            };
            btnThemeToggle.Click += (s, e) => _viewModel.ToggleThemeCommand.Execute(null);
            actionButtonsPanel.Controls.Add(btnThemeToggle);
            
            // Add ComboBox for incomplete sessions to the actionButtonsPanel
            cboIncompleteSessions = new ComboBox
            {
                Size = new Size(210, 23), // Standard ComboBox height
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            actionButtonsPanel.Controls.Add(cboIncompleteSessions);

            // Add Resume button to the actionButtonsPanel
            btnResumeSession = new MaterialButton
            {
                Text = "Resume Session",
                Size = new Size(140, 36)
            };
            btnResumeSession.Click += (s, e) => 
            {
                if (_viewModel.ResumeSessionCommand.CanExecute(null))
                    _viewModel.ResumeSessionCommand.Execute(null);
            };
            actionButtonsPanel.Controls.Add(btnResumeSession);

            // Add the actionButtonsPanel to tabPage1
            this.tabPage1.Controls.Add(actionButtonsPanel);

            // Setup context menu for the task list
            var ctxMenu = new ContextMenuStrip();
            var filterMenu = new ToolStripMenuItem("Filter Tasks");
            var filterAllMenu = new ToolStripMenuItem("Show All", null, (s, e) => _viewModel.FilterAllCommand.Execute(null));
            var filterCompleteMenu = new ToolStripMenuItem("Show Complete", null, (s, e) => _viewModel.FilterCompleteCommand.Execute(null));
            var filterIncompleteMenu = new ToolStripMenuItem("Show Incomplete", null, (s, e) => _viewModel.FilterIncompleteCommand.Execute(null));
            filterMenu.DropDownItems.Add(filterAllMenu);
            filterMenu.DropDownItems.Add(filterCompleteMenu);
            filterMenu.DropDownItems.Add(filterIncompleteMenu);
            ctxMenu.Items.Add(filterMenu);
            ctxMenu.Items.Add("Delete", null, (s, e) => _viewModel.DeleteTaskCommand.Execute(null));
            ctxMenu.Items.Add("Toggle Complete", null, (s, e) => StartStopTimerForSelectedTask());
            listTasks.ContextMenuStrip = ctxMenu;

            // Update button states based on current view model state
            UpdateButtonStates();

            // The ComboBox and Resume button are now part of actionButtonsPanel
            // So, the direct .Controls.Add calls for them are removed from here.
        }

        /// <summary>
        /// Sets up event handlers for controls.
        /// </summary>
        private void InitializeEventHandlers()
        {
            // Button event handlers
            btnStart.Click += (s, e) => _viewModel.StartCommand.Execute(null);
            btnStop.Click += (s, e) => _viewModel.StopCommand.Execute(null);
            btnReset.Click += (s, e) => _viewModel.ResetCommand.Execute(null);
            btnNewTask.Click += (s, e) => _viewModel.NewTaskCommand.Execute(null);
            btnNewCategory.Click += (s, e) => _viewModel.NewCategoryCommand.Execute(null);
            
            // List selection event handlers
            listTasks.SelectedIndexChanged += (sender, item) => ListTasks_SelectedIndexChanged(sender, item);
            
            // Input controls event handlers
            txtTags.Leave += (s, e) => _viewModel.SaveTagsCommand.Execute(null);
            cboCategories.SelectedIndexChanged += (s, e) => _viewModel.SaveCategoryCommand.Execute(null);
            themeSelector.SelectedIndexChanged += ThemeSelector_SelectedIndexChanged;
            
            // Ensure TaskName is updated as user types
            txtTaskName.TextChanged += (s, e) =>
            {
                Debug.WriteLine("TextChanged: " + txtTaskName.Text);
                _viewModel.TaskName = txtTaskName.Text;
                UpdateButtonStates();
            };
            
            // Form closing event
            // FormClosing += (s, e) => _viewModel.SaveUserStateCommand?.Execute(null);
            
            // Subscribe to property changes from the view model
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            // _viewModel.Tasks.CollectionChanged += (s, e) => UpdateTaskList();
        }

        /// <summary>
        /// Sets up data bindings between the view model and UI controls.
        /// </summary>
        private void InitializeDataBindings()
        {
            // Bind categories combobox
            cboCategories.DataSource = _categoriesBindingSource;
            cboCategories.DisplayMember = "Name";
            cboCategories.ValueMember = "Id";
            
            // Bind text fields using two-way binding
            txtTags.DataBindings.Add(new Binding("Text", _viewModel, "Tags", true, DataSourceUpdateMode.OnPropertyChanged));
            
            // Bind sessions grid
            dataGridSessions.DataSource = _sessionsBindingSource;
            
            // Format the grid
            FormatDataGridView();
            
            // Setup chart data
            LoadChartData();

            // Populate task list for the first time
            UpdateTaskList();

            // Bind incomplete sessions ComboBox
            cboIncompleteSessions.DataSource = _viewModel.IncompleteSessions;
            cboIncompleteSessions.DisplayMember = "StartTime";
            cboIncompleteSessions.ValueMember = "Id";
            cboIncompleteSessions.SelectedIndexChanged += (s, e) =>
            {
                if (cboIncompleteSessions.SelectedItem is PricisApp.Core.Entities.Session session)
                    _viewModel.SelectedIncompleteSession = session;
            };
        }

        /// <summary>
        /// Populates the listTasks control with tasks from the view model.
        /// </summary>
        private void UpdateTaskList()
        {
            listTasks.Items.Clear();
            foreach (var task in _viewModel.Tasks)
            {
                var item = new MaterialListBoxItem(task.Name) { Tag = task };
                listTasks.Items.Add(item);
            }
        }

        /// <summary>
        /// Loads chart data from the view model.
        /// </summary>
        private void LoadChartData()
        {
            formsPlot1.Plot.Clear();
            formsPlot1.Plot.AddScatter(_viewModel.ChartDataX, _viewModel.ChartDataY);
            formsPlot1.Refresh();
        }

        /// <summary>
        /// Handles theme selector changes.
        /// </summary>
        private void ThemeSelector_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (themeSelector.SelectedItem is string selectedTheme)
            {
                _viewModel.CurrentTheme = selectedTheme;
            }
        }

        /// <summary>
        /// Formats the data grid view columns.
        /// </summary>
        private void FormatDataGridView()
        {
            if (dataGridSessions.Columns.Count > 0)
            {
                dataGridSessions.Columns["Id"].Visible = false;
                dataGridSessions.Columns["TaskId"].Visible = false;
                
                if (dataGridSessions.Columns.Contains("StartTime"))
                    dataGridSessions.Columns["StartTime"].HeaderText = "Start Time";
                
                if (dataGridSessions.Columns.Contains("EndTime"))
                    dataGridSessions.Columns["EndTime"].HeaderText = "End Time";
                
                if (dataGridSessions.Columns.Contains("Duration"))
                    dataGridSessions.Columns["Duration"].HeaderText = "Duration (hh:mm:ss)";
            }
        }

        /// <summary>
        /// Handles property changes from the view model.
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.ElapsedTime):
                    lblTime.Text = _viewModel.ElapsedTime.ToString(@"hh\:mm\:ss\.ff");
                    break;
                case nameof(MainViewModel.IsTimerRunning):
                case nameof(MainViewModel.IsPaused):
                    UpdateButtonStates();
                    UpdateSessionStateLabel();
                    break;
                case nameof(MainViewModel.SelectedTask):
                    UpdateCurrentTaskLabel();
                    break;
                case nameof(MainViewModel.Categories):
                    _categoriesBindingSource.DataSource = _viewModel.Categories;
                    break;
                    
                case nameof(MainViewModel.Sessions):
                    _sessionsBindingSource.DataSource = _viewModel.Sessions;
                    FormatDataGridView();
                    break;
                    
                case nameof(MainViewModel.Tasks):
                    UpdateTaskList();
                    break;
                    
                case nameof(MainViewModel.ChartDataX):
                case nameof(MainViewModel.ChartDataY):
                    LoadChartData();
                    break;
                case nameof(MainViewModel.CurrentTheme):
                    ApplyTheme();
                    break;
                case nameof(MainViewModel.IncompleteSessions):
                    cboIncompleteSessions.DataSource = null;
                    cboIncompleteSessions.DataSource = _viewModel.IncompleteSessions;
                    cboIncompleteSessions.DisplayMember = "StartTime";
                    cboIncompleteSessions.ValueMember = "Id";
                    break;
            }
        }

        /// <summary>
        /// Updates the enabled state of buttons based on the current state.
        /// </summary>
        private void UpdateButtonStates()
        {
            btnStart.Enabled = _viewModel.StartCommand.CanExecute(null);
            btnStop.Enabled = _viewModel.StopCommand.CanExecute(null);
            btnReset.Enabled = true; // Reset is always available
            btnNewTask.Enabled = _viewModel.NewTaskCommand.CanExecute(null);
            
            // Update pause/resume button text
            var pauseResumeButton = Controls.OfType<MaterialButton>().FirstOrDefault(b => b.Text == "Pause" || b.Text == "Resume");
            if (pauseResumeButton != null)
            {
                pauseResumeButton.Text = _viewModel.IsPaused ? "Resume" : "Pause";
            }
        }

        /// <summary>
        /// Handles the selection change in the task list.
        /// </summary>
        private void ListTasks_SelectedIndexChanged(object? sender, MaterialListBoxItem selectedItem)
        {
            if (selectedItem?.Tag is TaskItem selectedTask)
            {
                _viewModel.SelectedTask = selectedTask;
                UpdateCurrentTaskLabel();
            }
            else
            {
                _viewModel.SelectedTask = null;
                UpdateCurrentTaskLabel();
            }
        }

        /// <summary>
        /// Starts or stops the timer for the selected task.
        /// </summary>
        private void StartStopTimerForSelectedTask()
        {
            if (_viewModel.IsTimerRunning)
            {
                _viewModel.StopCommand.Execute(null);
            }
            else
            {
                _viewModel.StartCommand.Execute(null);
            }
        }

        /// <summary>
        /// Applies the current theme from the view model.
        /// </summary>
        private void ApplyTheme()
        {
            if (_viewModel.CurrentTheme == "Dark")
            {
                _materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
                _materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey800, Primary.BlueGrey900, Primary.BlueGrey500, Accent.LightBlue200, TextShade.WHITE);
            }
            else
            {
                _materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
                _materialSkinManager.ColorScheme = new ColorScheme(Primary.Indigo500, Primary.Indigo700, Primary.Indigo100, Accent.Pink200, TextShade.WHITE);
            }
            Invalidate();
        }

        private void btnResetDatabase_Click(object sender, EventArgs e)
        {
            // Ask for confirmation
            DialogResult result = MessageBox.Show("Are you sure you want to reset the database schema? This action cannot be undone.", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                try
                {
                    // Call the fix database method instead of reset
                    Task.Run(async () => await DatabaseFix.FixDatabaseAsync()).Wait();
                    MessageBox.Show("Database has been reset successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Reload data
                    _viewModel.LoadDataCommand.Execute(null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error resetting database: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateCurrentTaskLabel()
        {
            if (_viewModel.SelectedTask != null)
                lblCurrentTask.Text = $"Current Task: {_viewModel.SelectedTask.Name}";
            else
                lblCurrentTask.Text = "Current Task: (none)";
        }

        private void UpdateSessionStateLabel()
        {
            if (_viewModel.IsTimerRunning)
            {
                if (_viewModel.IsPaused)
                {
                    lblSessionState.Text = "State: Paused";
                    lblSessionState.ForeColor = System.Drawing.Color.Orange;
                }
                else
                {
                    lblSessionState.Text = "State: Running";
                    lblSessionState.ForeColor = System.Drawing.Color.Green;
                }
            }
            else
            {
                lblSessionState.Text = "State: Stopped";
                lblSessionState.ForeColor = System.Drawing.Color.Gray;
            }
        }

        private void CenterTimerPanel()
        {
            // Center the timerPanel horizontally at the top
            int panelWidth = timerPanel.Width;
            int formWidth = this.ClientSize.Width;
            timerPanel.Left = (formWidth - panelWidth) / 2;
            timerPanel.Top = 60; // vertical offset from top

            // Stack labels vertically with spacing
            int y = 20;
            lblCurrentTask.AutoSize = true;
            lblCurrentTask.Location = new System.Drawing.Point((timerPanel.Width - lblCurrentTask.Width) / 2, y);
            y += lblCurrentTask.Height + 10;
            lblTime.AutoSize = true;
            lblTime.Location = new System.Drawing.Point((timerPanel.Width - lblTime.Width) / 2, y);
            y += lblTime.Height + 10;
            lblSessionState.AutoSize = true;
            lblSessionState.Location = new System.Drawing.Point((timerPanel.Width - lblSessionState.Width) / 2, y);

            // Center the buttonRow at the bottom
            buttonRow.Width = timerPanel.Width - 40;
            buttonRow.Left = (timerPanel.Width - buttonRow.Width) / 2;
            buttonRow.Top = timerPanel.Height - buttonRow.Height - 20;
        }
    }
}