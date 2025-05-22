namespace p_data_receive
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
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
      this.timer1 = new System.Windows.Forms.Timer(this.components);
      this.menu = new System.Windows.Forms.MenuStrip();
      this.файлToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.menuFileExit = new System.Windows.Forms.ToolStripMenuItem();
      this.Log = new System.Windows.Forms.TextBox();
      this.tab1 = new System.Windows.Forms.TabControl();
      this.tabControl = new System.Windows.Forms.TabPage();
      this.cbDataPollEnable = new System.Windows.Forms.CheckBox();
      this.bControlStop = new System.Windows.Forms.Button();
      this.bControlStart = new System.Windows.Forms.Button();
      this.tableControlDeviceProperties = new System.Windows.Forms.DataGridView();
      this.ip = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.localPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.rdValueHex = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.tabPage2 = new System.Windows.Forms.TabPage();
      this.cbSaveData = new System.Windows.Forms.CheckBox();
      this.menu.SuspendLayout();
      this.tab1.SuspendLayout();
      this.tabControl.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.tableControlDeviceProperties)).BeginInit();
      this.SuspendLayout();
      // 
      // timer1
      // 
      this.timer1.Interval = 50;
      this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
      // 
      // menu
      // 
      this.menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.файлToolStripMenuItem});
      this.menu.Location = new System.Drawing.Point(0, 0);
      this.menu.Name = "menu";
      this.menu.Size = new System.Drawing.Size(915, 25);
      this.menu.TabIndex = 0;
      this.menu.Text = "menuStrip1";
      // 
      // файлToolStripMenuItem
      // 
      this.файлToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFileExit});
      this.файлToolStripMenuItem.Name = "файлToolStripMenuItem";
      this.файлToolStripMenuItem.Size = new System.Drawing.Size(50, 21);
      this.файлToolStripMenuItem.Text = "Файл";
      // 
      // menuFileExit
      // 
      this.menuFileExit.Name = "menuFileExit";
      this.menuFileExit.Size = new System.Drawing.Size(113, 22);
      this.menuFileExit.Text = "Выход";
      this.menuFileExit.Click += new System.EventHandler(this.menuFileExit_Click);
      // 
      // Log
      // 
      this.Log.AcceptsReturn = true;
      this.Log.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.Log.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
      this.Log.Location = new System.Drawing.Point(4, 400);
      this.Log.Multiline = true;
      this.Log.Name = "Log";
      this.Log.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.Log.Size = new System.Drawing.Size(906, 416);
      this.Log.TabIndex = 4;
      // 
      // tab1
      // 
      this.tab1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.tab1.Controls.Add(this.tabControl);
      this.tab1.Controls.Add(this.tabPage2);
      this.tab1.Location = new System.Drawing.Point(4, 28);
      this.tab1.Name = "tab1";
      this.tab1.SelectedIndex = 0;
      this.tab1.Size = new System.Drawing.Size(906, 366);
      this.tab1.TabIndex = 5;
      // 
      // tabControl
      // 
      this.tabControl.Controls.Add(this.cbSaveData);
      this.tabControl.Controls.Add(this.cbDataPollEnable);
      this.tabControl.Controls.Add(this.bControlStop);
      this.tabControl.Controls.Add(this.bControlStart);
      this.tabControl.Controls.Add(this.tableControlDeviceProperties);
      this.tabControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
      this.tabControl.Location = new System.Drawing.Point(4, 24);
      this.tabControl.Name = "tabControl";
      this.tabControl.Padding = new System.Windows.Forms.Padding(3);
      this.tabControl.Size = new System.Drawing.Size(898, 338);
      this.tabControl.TabIndex = 0;
      this.tabControl.Text = "Управление";
      this.tabControl.UseVisualStyleBackColor = true;
      // 
      // cbDataPollEnable
      // 
      this.cbDataPollEnable.AutoSize = true;
      this.cbDataPollEnable.Checked = true;
      this.cbDataPollEnable.CheckState = System.Windows.Forms.CheckState.Checked;
      this.cbDataPollEnable.Location = new System.Drawing.Point(511, 29);
      this.cbDataPollEnable.Name = "cbDataPollEnable";
      this.cbDataPollEnable.Size = new System.Drawing.Size(163, 20);
      this.cbDataPollEnable.TabIndex = 13;
      this.cbDataPollEnable.Text = "Запросы данных ВКЛ";
      this.cbDataPollEnable.UseVisualStyleBackColor = true;
      this.cbDataPollEnable.CheckedChanged += new System.EventHandler(this.cbDataPollEnable_CheckedChanged);
      // 
      // bControlStop
      // 
      this.bControlStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.bControlStop.Location = new System.Drawing.Point(760, 58);
      this.bControlStop.Name = "bControlStop";
      this.bControlStop.Size = new System.Drawing.Size(98, 28);
      this.bControlStop.TabIndex = 12;
      this.bControlStop.Text = "Стоп";
      this.bControlStop.UseVisualStyleBackColor = true;
      this.bControlStop.Click += new System.EventHandler(this.bControlStop_Click);
      // 
      // bControlStart
      // 
      this.bControlStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.bControlStart.Location = new System.Drawing.Point(760, 24);
      this.bControlStart.Name = "bControlStart";
      this.bControlStart.Size = new System.Drawing.Size(98, 28);
      this.bControlStart.TabIndex = 11;
      this.bControlStart.Text = "Пуск";
      this.bControlStart.UseVisualStyleBackColor = true;
      this.bControlStart.Click += new System.EventHandler(this.bControlStart_Click);
      // 
      // tableControlDeviceProperties
      // 
      this.tableControlDeviceProperties.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
      this.tableControlDeviceProperties.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.tableControlDeviceProperties.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ip,
            this.localPort,
            this.rdValueHex});
      dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
      dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
      dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
      dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
      dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
      dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
      dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
      this.tableControlDeviceProperties.DefaultCellStyle = dataGridViewCellStyle2;
      this.tableControlDeviceProperties.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
      this.tableControlDeviceProperties.Location = new System.Drawing.Point(6, 6);
      this.tableControlDeviceProperties.Name = "tableControlDeviceProperties";
      this.tableControlDeviceProperties.RowHeadersVisible = false;
      this.tableControlDeviceProperties.Size = new System.Drawing.Size(304, 326);
      this.tableControlDeviceProperties.TabIndex = 10;
      // 
      // ip
      // 
      this.ip.HeaderText = "IP";
      this.ip.MaxInputLength = 15;
      this.ip.MinimumWidth = 80;
      this.ip.Name = "ip";
      this.ip.ReadOnly = true;
      this.ip.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.ip.Width = 120;
      // 
      // localPort
      // 
      this.localPort.HeaderText = "Local port";
      this.localPort.MaxInputLength = 6;
      this.localPort.Name = "localPort";
      this.localPort.ReadOnly = true;
      this.localPort.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.localPort.Width = 90;
      // 
      // rdValueHex
      // 
      this.rdValueHex.HeaderText = "Remote port";
      this.rdValueHex.Name = "rdValueHex";
      this.rdValueHex.ReadOnly = true;
      this.rdValueHex.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.rdValueHex.Width = 90;
      // 
      // tabPage2
      // 
      this.tabPage2.Location = new System.Drawing.Point(4, 24);
      this.tabPage2.Name = "tabPage2";
      this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
      this.tabPage2.Size = new System.Drawing.Size(898, 338);
      this.tabPage2.TabIndex = 1;
      this.tabPage2.Text = "tabPage2";
      this.tabPage2.UseVisualStyleBackColor = true;
      // 
      // cbSaveData
      // 
      this.cbSaveData.AutoSize = true;
      this.cbSaveData.Location = new System.Drawing.Point(511, 58);
      this.cbSaveData.Name = "cbSaveData";
      this.cbSaveData.Size = new System.Drawing.Size(196, 20);
      this.cbSaveData.TabIndex = 14;
      this.cbSaveData.Text = "Сохранять данные в файл";
      this.cbSaveData.UseVisualStyleBackColor = true;
      this.cbSaveData.CheckedChanged += new System.EventHandler(this.cbSaveData_CheckedChanged);
      // 
      // Form1
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(915, 820);
      this.Controls.Add(this.tab1);
      this.Controls.Add(this.Log);
      this.Controls.Add(this.menu);
      this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
      this.MainMenuStrip = this.menu;
      this.Name = "Form1";
      this.Text = "DataIO/UDP data reciever";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
      this.Load += new System.EventHandler(this.Form1_Load);
      this.menu.ResumeLayout(false);
      this.menu.PerformLayout();
      this.tab1.ResumeLayout(false);
      this.tabControl.ResumeLayout(false);
      this.tabControl.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)(this.tableControlDeviceProperties)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Timer timer1;
    private System.Windows.Forms.MenuStrip menu;
    private System.Windows.Forms.ToolStripMenuItem файлToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem menuFileExit;
    private System.Windows.Forms.TextBox Log;
    private System.Windows.Forms.TabControl tab1;
    private System.Windows.Forms.TabPage tabControl;
    private System.Windows.Forms.TabPage tabPage2;
    private System.Windows.Forms.Button bControlStop;
    private System.Windows.Forms.Button bControlStart;
    private System.Windows.Forms.DataGridView tableControlDeviceProperties;
    private System.Windows.Forms.DataGridViewTextBoxColumn ip;
    private System.Windows.Forms.DataGridViewTextBoxColumn localPort;
    private System.Windows.Forms.DataGridViewTextBoxColumn rdValueHex;
    private System.Windows.Forms.CheckBox cbDataPollEnable;
    private System.Windows.Forms.CheckBox cbSaveData;
  }
}

