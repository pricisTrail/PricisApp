namespace PricisApp
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.materialTabControl1 = new MaterialSkin.Controls.MaterialTabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.materialCard1 = new MaterialSkin.Controls.MaterialCard();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.btnStart = new MaterialSkin.Controls.MaterialButton();
            this.btnStop = new MaterialSkin.Controls.MaterialButton();
            this.btnReset = new MaterialSkin.Controls.MaterialButton();
            this.lblTime = new MaterialSkin.Controls.MaterialLabel();
            this.materialCard2 = new MaterialSkin.Controls.MaterialCard();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.txtTaskName = new MaterialSkin.Controls.MaterialTextBox();
            this.cboCategories = new MaterialSkin.Controls.MaterialComboBox();
            this.txtTags = new MaterialSkin.Controls.MaterialTextBox();
            this.btnNewCategory = new MaterialSkin.Controls.MaterialButton();
            this.btnNewTask = new MaterialSkin.Controls.MaterialButton();
            this.materialCard3 = new MaterialSkin.Controls.MaterialCard();
            this.listTasks = new MaterialSkin.Controls.MaterialListBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.dataGridSessions = new System.Windows.Forms.DataGridView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.formsPlot1 = new ScottPlot.FormsPlot();
            this.themeSelector = new MaterialSkin.Controls.MaterialComboBox();
            this.materialTabSelector1 = new MaterialSkin.Controls.MaterialTabSelector();
            this.tasksBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.categoriesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.sessionsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.timerPanel = new System.Windows.Forms.Panel();
            this.lblCurrentTask = new System.Windows.Forms.Label();
            this.lblSessionState = new System.Windows.Forms.Label();
            this.buttonRow = new System.Windows.Forms.FlowLayoutPanel();
            this.toolTip1 = new System.Windows.Forms.ToolTip();
            this.materialTabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.materialCard1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.materialCard2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.materialCard3.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridSessions)).BeginInit();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tasksBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.categoriesBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sessionsBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // materialTabControl1
            // 
            this.materialTabControl1.Controls.Add(this.tabPage1);
            this.materialTabControl1.Controls.Add(this.tabPage2);
            this.materialTabControl1.Controls.Add(this.tabPage3);
            this.materialTabControl1.Depth = 0;
            this.materialTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialTabControl1.Location = new System.Drawing.Point(3, 100);
            this.materialTabControl1.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialTabControl1.Multiline = true;
            this.materialTabControl1.Name = "materialTabControl1";
            this.materialTabControl1.SelectedIndex = 0;
            this.materialTabControl1.Size = new System.Drawing.Size(794, 347);
            this.materialTabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.tableLayoutPanel1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(786, 321);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Time Tracking";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.materialCard1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.materialCard2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.materialCard3, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(780, 315);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // materialCard1
            // 
            this.materialCard1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.materialCard1.Controls.Add(this.tableLayoutPanel2);
            this.materialCard1.Depth = 0;
            this.materialCard1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialCard1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.materialCard1.Location = new System.Drawing.Point(14, 14);
            this.materialCard1.Margin = new System.Windows.Forms.Padding(14);
            this.materialCard1.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialCard1.Name = "materialCard1";
            this.materialCard1.Padding = new System.Windows.Forms.Padding(14);
            this.materialCard1.Size = new System.Drawing.Size(362, 129);
            this.materialCard1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.btnStart, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnStop, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnReset, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.lblTime, 1, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(14, 14);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(334, 101);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // btnStart
            // 
            this.btnStart.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnStart.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.btnStart.Depth = 0;
            this.btnStart.HighEmphasis = true;
            this.btnStart.Icon = null;
            this.btnStart.Location = new System.Drawing.Point(4, 6);
            this.btnStart.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.btnStart.MouseState = MaterialSkin.MouseState.HOVER;
            this.btnStart.Name = "btnStart";
            this.btnStart.NoAccentTextColor = System.Drawing.Color.Empty;
            this.btnStart.Size = new System.Drawing.Size(64, 36);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "Start";
            this.btnStart.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.btnStart.UseAccentColor = false;
            this.btnStart.UseVisualStyleBackColor = true;
            // 
            // btnStop
            // 
            this.btnStop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnStop.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.btnStop.Depth = 0;
            this.btnStop.HighEmphasis = true;
            this.btnStop.Icon = null;
            this.btnStop.Location = new System.Drawing.Point(171, 6);
            this.btnStop.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.btnStop.MouseState = MaterialSkin.MouseState.HOVER;
            this.btnStop.Name = "btnStop";
            this.btnStop.NoAccentTextColor = System.Drawing.Color.Empty;
            this.btnStop.Size = new System.Drawing.Size(64, 36);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "Stop";
            this.btnStop.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.btnStop.UseAccentColor = false;
            this.btnStop.UseVisualStyleBackColor = true;
            // 
            // btnReset
            // 
            this.btnReset.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnReset.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.btnReset.Depth = 0;
            this.btnReset.HighEmphasis = true;
            this.btnReset.Icon = null;
            this.btnReset.Location = new System.Drawing.Point(4, 56);
            this.btnReset.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.btnReset.MouseState = MaterialSkin.MouseState.HOVER;
            this.btnReset.Name = "btnReset";
            this.btnReset.NoAccentTextColor = System.Drawing.Color.Empty;
            this.btnReset.Size = new System.Drawing.Size(64, 36);
            this.btnReset.TabIndex = 2;
            this.btnReset.Text = "Reset";
            this.btnReset.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.btnReset.UseAccentColor = false;
            this.btnReset.UseVisualStyleBackColor = true;
            // 
            // lblTime
            // 
            this.lblTime.AutoSize = true;
            this.lblTime.Depth = 0;
            this.lblTime.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.lblTime.Location = new System.Drawing.Point(170, 50);
            this.lblTime.MouseState = MaterialSkin.MouseState.HOVER;
            this.lblTime.Name = "lblTime";
            this.lblTime.Size = new System.Drawing.Size(71, 19);
            this.lblTime.TabIndex = 3;
            this.lblTime.Text = "00:00:00";
            // 
            // materialCard2
            // 
            this.materialCard2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.materialCard2.Controls.Add(this.tableLayoutPanel3);
            this.materialCard2.Depth = 0;
            this.materialCard2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialCard2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.materialCard2.Location = new System.Drawing.Point(404, 14);
            this.materialCard2.Margin = new System.Windows.Forms.Padding(14);
            this.materialCard2.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialCard2.Name = "materialCard2";
            this.materialCard2.Padding = new System.Windows.Forms.Padding(14);
            this.materialCard2.Size = new System.Drawing.Size(362, 129);
            this.materialCard2.TabIndex = 1;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Controls.Add(this.txtTaskName, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.cboCategories, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.txtTags, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.btnNewCategory, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.btnNewTask, 0, 2);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(14, 14);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 3;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(334, 101);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // txtTaskName
            // 
            this.txtTaskName.AnimateReadOnly = false;
            this.txtTaskName.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtTaskName.Depth = 0;
            this.txtTaskName.Font = new System.Drawing.Font("Roboto", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.txtTaskName.Hint = "Task Name";
            this.txtTaskName.LeadingIcon = null;
            this.txtTaskName.Location = new System.Drawing.Point(3, 3);
            this.txtTaskName.MaxLength = 50;
            this.txtTaskName.MouseState = MaterialSkin.MouseState.OUT;
            this.txtTaskName.Multiline = false;
            this.txtTaskName.Name = "txtTaskName";
            this.txtTaskName.Size = new System.Drawing.Size(161, 50);
            this.txtTaskName.TabIndex = 0;
            this.txtTaskName.Text = "";
            this.txtTaskName.TrailingIcon = null;
            // 
            // cboCategories
            // 
            this.cboCategories.AutoResize = false;
            this.cboCategories.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.cboCategories.Depth = 0;
            this.cboCategories.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.cboCategories.DropDownHeight = 174;
            this.cboCategories.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCategories.DropDownWidth = 121;
            this.cboCategories.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
            this.cboCategories.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.cboCategories.FormattingEnabled = true;
            this.cboCategories.Hint = "Category";
            this.cboCategories.IntegralHeight = false;
            this.cboCategories.ItemHeight = 43;
            this.cboCategories.Location = new System.Drawing.Point(3, 36);
            this.cboCategories.MaxDropDownItems = 4;
            this.cboCategories.MouseState = MaterialSkin.MouseState.OUT;
            this.cboCategories.Name = "cboCategories";
            this.cboCategories.Size = new System.Drawing.Size(161, 49);
            this.cboCategories.StartIndex = 0;
            this.cboCategories.TabIndex = 1;
            // 
            // txtTags
            // 
            this.txtTags.AnimateReadOnly = false;
            this.txtTags.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtTags.Depth = 0;
            this.txtTags.Font = new System.Drawing.Font("Roboto", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.txtTags.Hint = "Tags (comma separated)";
            this.txtTags.LeadingIcon = null;
            this.txtTags.Location = new System.Drawing.Point(170, 3);
            this.txtTags.MaxLength = 50;
            this.txtTags.MouseState = MaterialSkin.MouseState.OUT;
            this.txtTags.Multiline = false;
            this.txtTags.Name = "txtTags";
            this.txtTags.Size = new System.Drawing.Size(161, 50);
            this.txtTags.TabIndex = 2;
            this.txtTags.Text = "";
            this.txtTags.TrailingIcon = null;
            // 
            // btnNewCategory
            // 
            this.btnNewCategory.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnNewCategory.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.btnNewCategory.Depth = 0;
            this.btnNewCategory.HighEmphasis = true;
            this.btnNewCategory.Icon = null;
            this.btnNewCategory.Location = new System.Drawing.Point(171, 39);
            this.btnNewCategory.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.btnNewCategory.MouseState = MaterialSkin.MouseState.HOVER;
            this.btnNewCategory.Name = "btnNewCategory";
            this.btnNewCategory.NoAccentTextColor = System.Drawing.Color.Empty;
            this.btnNewCategory.Size = new System.Drawing.Size(124, 36);
            this.btnNewCategory.TabIndex = 3;
            this.btnNewCategory.Text = "New Category";
            this.btnNewCategory.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.btnNewCategory.UseAccentColor = false;
            this.btnNewCategory.UseVisualStyleBackColor = true;
            // 
            // btnNewTask
            // 
            this.btnNewTask.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnNewTask.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.btnNewTask.Depth = 0;
            this.btnNewTask.HighEmphasis = true;
            this.btnNewTask.Icon = null;
            this.btnNewTask.Location = new System.Drawing.Point(4, 72);
            this.btnNewTask.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.btnNewTask.MouseState = MaterialSkin.MouseState.HOVER;
            this.btnNewTask.Name = "btnNewTask";
            this.btnNewTask.NoAccentTextColor = System.Drawing.Color.Empty;
            this.btnNewTask.Size = new System.Drawing.Size(91, 36);
            this.btnNewTask.TabIndex = 4;
            this.btnNewTask.Text = "New Task";
            this.btnNewTask.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.btnNewTask.UseAccentColor = false;
            this.btnNewTask.UseVisualStyleBackColor = true;
            // 
            // materialCard3
            // 
            this.materialCard3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.tableLayoutPanel1.SetColumnSpan(this.materialCard3, 2);
            this.materialCard3.Controls.Add(this.listTasks);
            this.materialCard3.Depth = 0;
            this.materialCard3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialCard3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.materialCard3.Location = new System.Drawing.Point(14, 171);
            this.materialCard3.Margin = new System.Windows.Forms.Padding(14);
            this.materialCard3.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialCard3.Name = "materialCard3";
            this.materialCard3.Padding = new System.Windows.Forms.Padding(14);
            this.materialCard3.Size = new System.Drawing.Size(752, 130);
            this.materialCard3.TabIndex = 2;
            // 
            // listTasks
            // 
            this.listTasks.BackColor = System.Drawing.Color.White;
            this.listTasks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listTasks.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.listTasks.Location = new System.Drawing.Point(14, 14);
            this.listTasks.Name = "listTasks";
            this.listTasks.SelectedIndex = -1;
            this.listTasks.SelectedItem = null;
            this.listTasks.Size = new System.Drawing.Size(724, 102);
            this.listTasks.TabIndex = 0;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dataGridSessions);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(786, 321);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Sessions";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // dataGridSessions
            // 
            this.dataGridSessions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridSessions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridSessions.Location = new System.Drawing.Point(3, 3);
            this.dataGridSessions.Name = "dataGridSessions";
            this.dataGridSessions.Size = new System.Drawing.Size(780, 315);
            this.dataGridSessions.TabIndex = 0;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.formsPlot1);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(786, 321);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Dashboard";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // formsPlot1
            // 
            this.formsPlot1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.formsPlot1.Location = new System.Drawing.Point(0, 0);
            this.formsPlot1.Name = "formsPlot1";
            this.formsPlot1.Size = new System.Drawing.Size(786, 321);
            this.formsPlot1.TabIndex = 0;
            // 
            // themeSelector
            // 
            this.themeSelector.AutoResize = false;
            this.themeSelector.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.themeSelector.Depth = 0;
            this.themeSelector.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.themeSelector.DropDownHeight = 174;
            this.themeSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.themeSelector.DropDownWidth = 121;
            this.themeSelector.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
            this.themeSelector.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.themeSelector.FormattingEnabled = true;
            this.themeSelector.Hint = "Theme";
            this.themeSelector.IntegralHeight = false;
            this.themeSelector.ItemHeight = 43;
            this.themeSelector.Location = new System.Drawing.Point(634, 30);
            this.themeSelector.MaxDropDownItems = 4;
            this.themeSelector.MouseState = MaterialSkin.MouseState.OUT;
            this.themeSelector.Name = "themeSelector";
            this.themeSelector.Size = new System.Drawing.Size(151, 49);
            this.themeSelector.StartIndex = 0;
            this.themeSelector.TabIndex = 4;
            // 
            // materialTabSelector1
            // 
            this.materialTabSelector1.BaseTabControl = this.materialTabControl1;
            this.materialTabSelector1.CharacterCasing = MaterialSkin.Controls.MaterialTabSelector.CustomCharacterCasing.Normal;
            this.materialTabSelector1.Depth = 0;
            this.materialTabSelector1.Dock = System.Windows.Forms.DockStyle.Top;
            this.materialTabSelector1.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.materialTabSelector1.Location = new System.Drawing.Point(3, 64);
            this.materialTabSelector1.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialTabSelector1.Name = "materialTabSelector1";
            this.materialTabSelector1.Size = new System.Drawing.Size(794, 36);
            this.materialTabSelector1.TabIndex = 1;
            this.materialTabSelector1.Text = "materialTabSelector1";
            // 
            // tasksBindingSource
            // 
            this.tasksBindingSource.DataSource = typeof(PricisApp.Core.Entities.TaskItem);
            // 
            // categoriesBindingSource
            // 
            this.categoriesBindingSource.DataSource = typeof(PricisApp.Models.Category);
            // 
            // sessionsBindingSource
            // 
            this.sessionsBindingSource.DataSource = typeof(System.Data.DataTable);
            // 
            // timerPanel
            // 
            this.timerPanel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.timerPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.timerPanel.Location = new System.Drawing.Point(30, 70);
            this.timerPanel.Size = new System.Drawing.Size(420, 220);
            this.timerPanel.Padding = new System.Windows.Forms.Padding(20);
            this.timerPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            // 
            // lblCurrentTask
            // 
            this.lblCurrentTask.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblCurrentTask.ForeColor = System.Drawing.Color.FromArgb(33, 150, 243);
            this.lblCurrentTask.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCurrentTask.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblCurrentTask.Height = 32;
            this.lblCurrentTask.Text = "Current Task: (none)";
            // 
            // lblSessionState
            // 
            this.lblSessionState.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblSessionState.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblSessionState.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSessionState.Height = 28;
            this.lblSessionState.Text = "State: Stopped";
            this.lblSessionState.ForeColor = System.Drawing.Color.Gray;
            // 
            // buttonRow
            // 
            this.buttonRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.buttonRow.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonRow.Height = 60;
            this.buttonRow.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.buttonRow.WrapContents = false;
            // Add buttons to buttonRow (assume btnStart, btnStop, btnReset already exist)
            this.btnStart.Width = this.btnStop.Width = this.btnReset.Width = 100;
            this.btnStart.Height = this.btnStop.Height = this.btnReset.Height = 40;
            this.buttonRow.Controls.Add(this.btnStart);
            this.buttonRow.Controls.Add(this.btnStop);
            this.buttonRow.Controls.Add(this.btnReset);
            // Tooltips
            this.toolTip1.SetToolTip(this.btnStart, "Start the timer");
            this.toolTip1.SetToolTip(this.btnStop, "Stop the timer");
            this.toolTip1.SetToolTip(this.btnReset, "Reset the timer");
            // Add controls to timerPanel
            this.timerPanel.Controls.Add(this.lblSessionState);
            this.timerPanel.Controls.Add(this.lblTime);
            this.timerPanel.Controls.Add(this.lblCurrentTask);
            this.timerPanel.Controls.Add(this.buttonRow);
            // Add timerPanel to the form
            this.Controls.Add(this.timerPanel);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.materialTabSelector1);
            this.Controls.Add(this.materialTabControl1);
            this.Controls.Add(this.themeSelector);
            this.Name = "Form1";
            this.Text = "PricisApp - Time Tracking";
            this.materialTabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.materialCard1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.materialCard2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.materialCard3.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridSessions)).EndInit();
            this.tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.tasksBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.categoriesBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sessionsBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private MaterialSkin.Controls.MaterialTabControl materialTabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private MaterialSkin.Controls.MaterialTabSelector materialTabSelector1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private MaterialSkin.Controls.MaterialCard materialCard1;
        private MaterialSkin.Controls.MaterialCard materialCard2;
        private MaterialSkin.Controls.MaterialCard materialCard3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private MaterialSkin.Controls.MaterialButton btnStart;
        private MaterialSkin.Controls.MaterialButton btnStop;
        private MaterialSkin.Controls.MaterialButton btnReset;
        private MaterialSkin.Controls.MaterialLabel lblTime;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private MaterialSkin.Controls.MaterialTextBox txtTaskName;
        private MaterialSkin.Controls.MaterialComboBox cboCategories;
        private MaterialSkin.Controls.MaterialTextBox txtTags;
        private MaterialSkin.Controls.MaterialButton btnNewCategory;
        private MaterialSkin.Controls.MaterialButton btnNewTask;
        private MaterialSkin.Controls.MaterialListBox listTasks;
        private System.Windows.Forms.DataGridView dataGridSessions;
        private ScottPlot.FormsPlot formsPlot1;
        private MaterialSkin.Controls.MaterialComboBox themeSelector;
        private System.Windows.Forms.BindingSource tasksBindingSource;
        private System.Windows.Forms.BindingSource categoriesBindingSource;
        private System.Windows.Forms.BindingSource sessionsBindingSource;
        private System.Windows.Forms.Panel timerPanel;
        private System.Windows.Forms.Label lblCurrentTask;
        private System.Windows.Forms.Label lblSessionState;
        private System.Windows.Forms.FlowLayoutPanel buttonRow;
        private System.Windows.Forms.ToolTip toolTip1;
    }
} 