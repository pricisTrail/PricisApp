using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace PricisApp
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer timer;
        private Button btnStart;
        private Button btnStop;
        private Button btnReset;
        private Button btnNewTask;
        private ComboBox themeSelector;
        private ListBox listTasks;
        private TextBox txtTaskName;
        private DataGridView dataSessions;
        private Label lblTime;
        private ComboBox cboCategories;
        private Button btnNewCategory;
        private TextBox txtTags;
        private Label lblCategory;
        private Label lblTags;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem tasksToolStripMenuItem;
        private ToolStripMenuItem newTaskToolStripMenuItem;
        private ToolStripMenuItem newCategoryToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem summaryToolStripMenuItem;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripMenuItem themesToolStripMenuItem;
        private ImageList taskImageList;
        private TableLayoutPanel mainLayout;
        private TableLayoutPanel taskInputLayout;
        private TableLayoutPanel timerLayout;
        private TableLayoutPanel taskListLayout;
        private TableLayoutPanel sessionsLayout;
        private GroupBox timerGroup;
        private GroupBox taskInputGroup;
        private GroupBox taskListGroup;
        private GroupBox sessionsGroup;
        private Label lblTaskName;
        private Label lblTimer;

        private void InitializeComponent()
        {
            this.timer = new System.Windows.Forms.Timer();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnNewTask = new System.Windows.Forms.Button();
            this.themeSelector = new System.Windows.Forms.ComboBox();
            this.listTasks = new System.Windows.Forms.ListBox();
            this.txtTaskName = new System.Windows.Forms.TextBox();
            this.dataSessions = new System.Windows.Forms.DataGridView();
            this.lblTime = new System.Windows.Forms.Label();
            this.cboCategories = new System.Windows.Forms.ComboBox();
            this.btnNewCategory = new System.Windows.Forms.Button();
            this.txtTags = new System.Windows.Forms.TextBox();
            this.lblCategory = new System.Windows.Forms.Label();
            this.lblTags = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tasksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newTaskToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newCategoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.summaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.themesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.taskImageList = new System.Windows.Forms.ImageList();
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.timerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.taskInputLayout = new System.Windows.Forms.TableLayoutPanel();
            this.taskListLayout = new System.Windows.Forms.TableLayoutPanel();
            this.sessionsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.timerGroup = new System.Windows.Forms.GroupBox();
            this.taskInputGroup = new System.Windows.Forms.GroupBox();
            this.taskListGroup = new System.Windows.Forms.GroupBox();
            this.sessionsGroup = new System.Windows.Forms.GroupBox();
            this.lblTaskName = new System.Windows.Forms.Label();
            this.lblTimer = new System.Windows.Forms.Label();

            // Configure ImageList
            this.taskImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.taskImageList.ImageSize = new System.Drawing.Size(16, 16);
            this.taskImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.taskImageList.Images.Add("Task", SystemIcons.Application);
            this.taskImageList.Images.Add("Complete", SystemIcons.Shield);
            this.taskImageList.Images.Add("Category", SystemIcons.Application);

            // Configure timer
            this.timer.Interval = 1000;

            // Configure main layout
            this.mainLayout.Dock = DockStyle.Fill;
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.RowCount = 4;
            this.mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            this.mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
            this.mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
            this.mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            this.mainLayout.Padding = new Padding(10);
            this.mainLayout.Margin = new Padding(10);

            // Configure timer group
            this.timerGroup.Text = "Timer Controls";
            this.timerGroup.Dock = DockStyle.Fill;
            this.timerGroup.Padding = new Padding(10);

            // Configure timer layout
            this.timerLayout.Dock = DockStyle.Fill;
            this.timerLayout.ColumnCount = 7;
            this.timerLayout.RowCount = 2;
            this.timerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            this.timerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            this.timerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            this.timerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            this.timerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            this.timerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            this.timerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            this.timerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            this.timerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            this.timerLayout.Padding = new Padding(5);

            // Configure task input group
            this.taskInputGroup.Text = "Task Details";
            this.taskInputGroup.Dock = DockStyle.Fill;
            this.taskInputGroup.Padding = new Padding(10);

            // Configure task input layout
            this.taskInputLayout.Dock = DockStyle.Fill;
            this.taskInputLayout.ColumnCount = 2;
            this.taskInputLayout.RowCount = 4;
            this.taskInputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            this.taskInputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            this.taskInputLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            this.taskInputLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            this.taskInputLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            this.taskInputLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            this.taskInputLayout.Padding = new Padding(5);

            // Configure task list group
            this.taskListGroup.Text = "Task List";
            this.taskListGroup.Dock = DockStyle.Fill;
            this.taskListGroup.Padding = new Padding(10);

            // Configure task list layout
            this.taskListLayout.Dock = DockStyle.Fill;
            this.taskListLayout.ColumnCount = 1;
            this.taskListLayout.RowCount = 1;
            this.taskListLayout.Padding = new Padding(5);

            // Configure sessions group
            this.sessionsGroup.Text = "Session History";
            this.sessionsGroup.Dock = DockStyle.Fill;
            this.sessionsGroup.Padding = new Padding(10);

            // Configure sessions layout
            this.sessionsLayout.Dock = DockStyle.Fill;
            this.sessionsLayout.ColumnCount = 1;
            this.sessionsLayout.RowCount = 1;
            this.sessionsLayout.Padding = new Padding(5);

            // Configure controls
            this.btnStart.Text = "Start";
            this.btnStart.Height = 30;
            this.btnStop.Text = "Stop";
            this.btnStop.Height = 30;
            this.btnReset.Text = "Reset";
            this.btnReset.Height = 30;
            this.btnNewTask.Text = "New Task";
            this.btnNewTask.Height = 30;
            this.btnNewCategory.Text = "New Category";
            this.btnNewCategory.Height = 30;
            this.lblTime.Text = "00:00:00";
            this.lblTime.Font = new Font(this.lblTime.Font.FontFamily, 14, FontStyle.Bold);
            this.lblTime.TextAlign = ContentAlignment.MiddleCenter;
            this.lblCategory.Text = "Category:";
            this.lblTags.Text = "Tags (comma sep):";
            this.lblTaskName.Text = "Task Name:";
            this.lblTimer.Text = "Elapsed Time:";

            this.themeSelector.Items.AddRange(new object[] {
                "System Default",
                "Light",
                "Dark",
                "Blue",
                "Green",
                "High Contrast"
            });
            this.themeSelector.Height = 30;

            // Configure DataGridView
            this.dataSessions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dataSessions.AllowUserToAddRows = false;
            this.dataSessions.AllowUserToDeleteRows = false;
            this.dataSessions.ReadOnly = true;
            this.dataSessions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dataSessions.MultiSelect = false;
            this.dataSessions.RowHeadersVisible = false;
            this.dataSessions.BackgroundColor = SystemColors.Window;

            // Add controls to timer layout
            this.timerLayout.Controls.Add(this.btnStart, 0, 0);
            this.timerLayout.Controls.Add(this.btnStop, 1, 0);
            this.timerLayout.Controls.Add(this.btnReset, 2, 0);
            this.timerLayout.Controls.Add(this.btnNewTask, 3, 0);
            this.timerLayout.Controls.Add(this.themeSelector, 4, 0);
            this.timerLayout.Controls.Add(this.lblTimer, 5, 0);
            this.timerLayout.Controls.Add(this.lblTime, 6, 0);

            // Add controls to task input layout
            this.taskInputLayout.Controls.Add(this.lblTaskName, 0, 0);
            this.taskInputLayout.Controls.Add(this.txtTaskName, 1, 0);
            this.taskInputLayout.Controls.Add(this.lblCategory, 0, 1);
            this.taskInputLayout.Controls.Add(this.cboCategories, 1, 1);
            this.taskInputLayout.Controls.Add(this.lblTags, 0, 2);
            this.taskInputLayout.Controls.Add(this.txtTags, 1, 2);
            this.taskInputLayout.Controls.Add(this.btnNewCategory, 1, 3);

            // Add controls to task list layout
            this.taskListLayout.Controls.Add(this.listTasks, 0, 0);

            // Add controls to sessions layout
            this.sessionsLayout.Controls.Add(this.dataSessions, 0, 0);

            // Add layouts to groups
            this.timerGroup.Controls.Add(this.timerLayout);
            this.taskInputGroup.Controls.Add(this.taskInputLayout);
            this.taskListGroup.Controls.Add(this.taskListLayout);
            this.sessionsGroup.Controls.Add(this.sessionsLayout);

            // Add groups to main layout
            this.mainLayout.Controls.Add(this.timerGroup, 0, 0);
            this.mainLayout.Controls.Add(this.taskInputGroup, 0, 1);
            this.mainLayout.Controls.Add(this.taskListGroup, 0, 2);
            this.mainLayout.Controls.Add(this.sessionsGroup, 0, 3);

            // Configure menu
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.fileToolStripMenuItem,
                this.tasksToolStripMenuItem,
                this.viewToolStripMenuItem,
                this.settingsToolStripMenuItem
            });

            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.exitToolStripMenuItem
            });
            this.fileToolStripMenuItem.Text = "&File";
            this.exitToolStripMenuItem.Text = "&Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);

            this.tasksToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.newTaskToolStripMenuItem,
                this.newCategoryToolStripMenuItem
            });
            this.tasksToolStripMenuItem.Text = "&Tasks";
            this.newTaskToolStripMenuItem.Text = "&New Task";
            this.newTaskToolStripMenuItem.Click += new System.EventHandler(this.BtnNewTask_Click);
            this.newCategoryToolStripMenuItem.Text = "New &Category";
            this.newCategoryToolStripMenuItem.Click += new System.EventHandler(this.BtnNewCategory_Click);

            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.summaryToolStripMenuItem
            });
            this.viewToolStripMenuItem.Text = "&View";
            this.summaryToolStripMenuItem.Text = "&Summary";
            this.summaryToolStripMenuItem.Click += new System.EventHandler(this.BtnSummary_Click);

            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.themesToolStripMenuItem
            });
            this.settingsToolStripMenuItem.Text = "&Settings";
            this.themesToolStripMenuItem.Text = "&Themes";
            this.themesToolStripMenuItem.Click += new System.EventHandler(this.ThemeSelector_SelectedIndexChanged);

            // Configure form
            this.ClientSize = new System.Drawing.Size(900, 700);
            this.Controls.Add(this.mainLayout);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "Form1";
            this.Text = "PricisApp";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F);

            ((System.ComponentModel.ISupportInitialize)(this.dataSessions)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}