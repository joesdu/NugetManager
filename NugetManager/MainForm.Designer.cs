using NugetManager.UControls;

namespace NugetManager;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private readonly System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        var dataGridViewCellStyle1 = new DataGridViewCellStyle();
        var dataGridViewCellStyle2 = new DataGridViewCellStyle();
        txtPackage = new TextBox();
        btnQuery = new Button();
        dgvVersions = new DataGridView();
        colSelect = new DataGridViewCheckBoxColumn();
        colVersion = new DataGridViewTextBoxColumn();
        colStatus = new DataGridViewTextBoxColumn();
        chkSelectAll = new CheckBox();
        txtApiKey = new TextBox();
        lblApiKey = new Label();
        btnDelete = new Button();
        btnRelist = new Button();
        btnCancel = new Button();
        txtReason = new TextBox();
        lblReason = new Label();
        grpReason = new GroupBox();
        lblDeprecationDescription = new Label();
        chkLegacy = new CheckBox();
        chkCriticalBugs = new CheckBox();
        chkOther = new CheckBox();
        lblAltPackage = new Label();
        txtAltPackage = new TextBox();
        lblAltVersion = new Label();
        cmbAltVersion = new ComboBox();
        lblPackage = new Label();
        btnSelectListed = new Button();
        btnSelectUnlisted = new Button();
        lblDeprecation = new Label();
        lblListingDescription = new Label(); lblLoading = new Label();
        loadingSpinner = new LoadingSpinner();
        cmbQuerySource = new ComboBox();
        lblQuerySource = new Label();
        lblQuerySourceHelp = new Label();
        ((System.ComponentModel.ISupportInitialize)dgvVersions).BeginInit();
        grpReason.SuspendLayout();
        SuspendLayout();
        // 
        // txtPackage
        // 
        txtPackage.Location = new Point(126, 17);
        txtPackage.Name = "txtPackage";
        txtPackage.Size = new Size(319, 23);
        txtPackage.TabIndex = 1;
        // 
        // btnQuery
        // 
        btnQuery.BackColor = Color.FromArgb(0, 122, 204);
        btnQuery.ForeColor = Color.White;
        btnQuery.Location = new Point(455, 16);
        btnQuery.Name = "btnQuery";
        btnQuery.Size = new Size(75, 25);
        btnQuery.TabIndex = 2;
        btnQuery.Text = "Search";
        btnQuery.UseVisualStyleBackColor = false;
        btnQuery.Click += btnQuery_Click;
        // 
        // dgvVersions
        // 
        dgvVersions.AllowUserToAddRows = false;
        dgvVersions.AllowUserToDeleteRows = false;
        dgvVersions.AllowUserToResizeRows = false;
        dgvVersions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvVersions.BackgroundColor = Color.White;
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(240, 240, 240);
        dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
        dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
        dgvVersions.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        dgvVersions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvVersions.Columns.AddRange(new DataGridViewColumn[] { colSelect, colVersion, colStatus });
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = Color.White;
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle2.ForeColor = Color.Black;
        dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(0, 122, 204);
        dataGridViewCellStyle2.SelectionForeColor = Color.White;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
        dgvVersions.DefaultCellStyle = dataGridViewCellStyle2;
        dgvVersions.EnableHeadersVisualStyles = false;
        dgvVersions.Location = new Point(20, 120);
        dgvVersions.Name = "dgvVersions";
        dgvVersions.RowHeadersVisible = false;
        dgvVersions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvVersions.Size = new Size(520, 200);
        dgvVersions.TabIndex = 12;
        // 
        // colSelect
        // 
        colSelect.HeaderText = "Select";
        colSelect.Name = "colSelect";
        // 
        // colVersion
        // 
        colVersion.HeaderText = "Version";
        colVersion.Name = "colVersion";
        colVersion.ReadOnly = true;
        // 
        // colStatus
        // 
        colStatus.HeaderText = "Status";
        colStatus.Name = "colStatus";
        colStatus.ReadOnly = true;
        // 
        // chkSelectAll
        // 
        chkSelectAll.AutoSize = true;
        chkSelectAll.Location = new Point(23, 330);
        chkSelectAll.Name = "chkSelectAll";
        chkSelectAll.Size = new Size(72, 19);
        chkSelectAll.TabIndex = 3;
        chkSelectAll.Text = "Select all";
        chkSelectAll.CheckedChanged += chkSelectAll_CheckedChanged;
        // 
        // txtApiKey
        // 
        txtApiKey.Location = new Point(90, 360);
        txtApiKey.Name = "txtApiKey";
        txtApiKey.Size = new Size(440, 23);
        txtApiKey.TabIndex = 7;
        txtApiKey.UseSystemPasswordChar = true;
        // 
        // lblApiKey
        // 
        lblApiKey.AutoSize = true;
        lblApiKey.Location = new Point(20, 363);
        lblApiKey.Name = "lblApiKey";
        lblApiKey.Size = new Size(50, 15);
        lblApiKey.TabIndex = 6;
        lblApiKey.Text = "API Key:";
        // 
        // btnDelete
        // 
        btnDelete.BackColor = Color.FromArgb(220, 53, 69);
        btnDelete.ForeColor = Color.White;
        btnDelete.Location = new Point(144, 683);
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(120, 35);
        btnDelete.TabIndex = 9;
        btnDelete.Text = "Deprecate";
        btnDelete.UseVisualStyleBackColor = false;
        btnDelete.Click += btnDelete_Click;
        // 
        // btnRelist
        // 
        btnRelist.BackColor = Color.FromArgb(40, 167, 69);
        btnRelist.ForeColor = Color.White;
        btnRelist.Location = new Point(284, 683);
        btnRelist.Name = "btnRelist";
        btnRelist.Size = new Size(120, 35);
        btnRelist.TabIndex = 10;
        btnRelist.Text = "Unlist Selected";
        btnRelist.UseVisualStyleBackColor = false;
        btnRelist.Click += btnRelist_Click;
        // 
        // btnCancel
        // 
        btnCancel.BackColor = Color.FromArgb(108, 117, 125);
        btnCancel.ForeColor = Color.White;
        btnCancel.Location = new Point(213, 724);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(120, 35);
        btnCancel.TabIndex = 11;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = false;
        btnCancel.Visible = false;
        btnCancel.Click += btnCancel_Click;
        // 
        // txtReason
        // 
        txtReason.Location = new Point(19, 220);
        txtReason.Name = "txtReason";
        txtReason.Size = new Size(475, 23);
        txtReason.TabIndex = 9;
        // 
        // lblReason
        // 
        lblReason.AutoSize = true;
        lblReason.Location = new Point(19, 200);
        lblReason.Name = "lblReason";
        lblReason.Size = new Size(167, 15);
        lblReason.TabIndex = 8;
        lblReason.Text = "Custom deprecation message:";
        // 
        // grpReason
        // 
        grpReason.Controls.Add(lblDeprecationDescription);
        grpReason.Controls.Add(chkLegacy);
        grpReason.Controls.Add(chkCriticalBugs);
        grpReason.Controls.Add(chkOther);
        grpReason.Controls.Add(lblAltPackage);
        grpReason.Controls.Add(txtAltPackage);
        grpReason.Controls.Add(lblAltVersion);
        grpReason.Controls.Add(cmbAltVersion);
        grpReason.Controls.Add(lblReason);
        grpReason.Controls.Add(txtReason);
        grpReason.Location = new Point(20, 408);
        grpReason.Name = "grpReason";
        grpReason.Size = new Size(520, 260);
        grpReason.TabIndex = 13;
        grpReason.TabStop = false;
        grpReason.Text = "Deprecation";
        // 
        // lblDeprecationDescription
        // 
        lblDeprecationDescription.Font = new Font("Segoe UI", 9F);
        lblDeprecationDescription.ForeColor = Color.FromArgb(108, 117, 125);
        lblDeprecationDescription.Location = new Point(19, 18);
        lblDeprecationDescription.Name = "lblDeprecationDescription";
        lblDeprecationDescription.Size = new Size(475, 35);
        lblDeprecationDescription.TabIndex = 0;
        lblDeprecationDescription.Text = "Deprecating a package will warn all consumers of the package who use it in their projects.\r\n\r\nSelect reason(s)";
        // 
        // chkLegacy
        // 
        chkLegacy.AutoSize = true;
        chkLegacy.Location = new Point(19, 60);
        chkLegacy.Name = "chkLegacy";
        chkLegacy.Size = new Size(294, 19);
        chkLegacy.TabIndex = 1;
        chkLegacy.Text = "This package is legacy and is no longer maintained";
        // 
        // chkCriticalBugs
        // 
        chkCriticalBugs.AutoSize = true;
        chkCriticalBugs.Location = new Point(19, 85);
        chkCriticalBugs.Name = "chkCriticalBugs";
        chkCriticalBugs.Size = new Size(300, 19);
        chkCriticalBugs.TabIndex = 2;
        chkCriticalBugs.Text = "This package has critical bugs that make it unusable";
        // 
        // chkOther
        // 
        chkOther.AutoSize = true;
        chkOther.Location = new Point(19, 110);
        chkOther.Name = "chkOther";
        chkOther.Size = new Size(56, 19);
        chkOther.TabIndex = 3;
        chkOther.Text = "Other";
        chkOther.CheckedChanged += chkOther_CheckedChanged;
        // 
        // lblAltPackage
        // 
        lblAltPackage.AutoSize = true;
        lblAltPackage.Location = new Point(19, 140);
        lblAltPackage.Name = "lblAltPackage";
        lblAltPackage.Size = new Size(114, 15);
        lblAltPackage.TabIndex = 4;
        lblAltPackage.Text = "Alternative package:";
        // 
        // txtAltPackage
        // 
        txtAltPackage.Location = new Point(144, 137);
        txtAltPackage.Name = "txtAltPackage";
        txtAltPackage.Size = new Size(350, 23);
        txtAltPackage.TabIndex = 5;
        txtAltPackage.Leave += txtAltPackage_Leave;
        // 
        // lblAltVersion
        // 
        lblAltVersion.AutoSize = true;
        lblAltVersion.Location = new Point(19, 170);
        lblAltVersion.Name = "lblAltVersion";
        lblAltVersion.Size = new Size(48, 15);
        lblAltVersion.TabIndex = 6;
        lblAltVersion.Text = "Version:";
        // 
        // cmbAltVersion
        // 
        cmbAltVersion.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbAltVersion.Location = new Point(144, 167);
        cmbAltVersion.Name = "cmbAltVersion";
        cmbAltVersion.Size = new Size(350, 23);
        cmbAltVersion.TabIndex = 7;
        // 
        // lblPackage
        // 
        lblPackage.AutoSize = true;
        lblPackage.Location = new Point(20, 20);
        lblPackage.Name = "lblPackage";
        lblPackage.Size = new Size(87, 15);
        lblPackage.TabIndex = 0;
        lblPackage.Text = "Package name:";
        // 
        // btnSelectListed
        // 
        btnSelectListed.Location = new Point(130, 328);
        btnSelectListed.Name = "btnSelectListed";
        btnSelectListed.Size = new Size(100, 25);
        btnSelectListed.TabIndex = 4;
        btnSelectListed.Text = "Select Listed";
        btnSelectListed.UseVisualStyleBackColor = true;
        btnSelectListed.Click += btnSelectListed_Click;
        // 
        // btnSelectUnlisted
        // 
        btnSelectUnlisted.Location = new Point(240, 328);
        btnSelectUnlisted.Name = "btnSelectUnlisted";
        btnSelectUnlisted.Size = new Size(110, 25);
        btnSelectUnlisted.TabIndex = 5;
        btnSelectUnlisted.Text = "Select Unlisted";
        btnSelectUnlisted.UseVisualStyleBackColor = true;
        btnSelectUnlisted.Click += btnSelectUnlisted_Click;
        // 
        // lblDeprecation
        // 
        lblDeprecation.AutoSize = true;
        lblDeprecation.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblDeprecation.ForeColor = Color.FromArgb(73, 80, 87);
        lblDeprecation.Location = new Point(20, 388);
        lblDeprecation.Name = "lblDeprecation";
        lblDeprecation.Size = new Size(244, 20);
        lblDeprecation.TabIndex = 8;
        lblDeprecation.Text = "Package Management Operations";
        // 
        // lblListingDescription
        // 
        lblListingDescription.Font = new Font("Segoe UI", 9F);
        lblListingDescription.ForeColor = Color.FromArgb(108, 117, 125);
        lblListingDescription.Location = new Point(20, 769);
        lblListingDescription.Name = "lblListingDescription";
        lblListingDescription.Size = new Size(520, 60);
        lblListingDescription.TabIndex = 11;
        lblListingDescription.Text = "You can control how your packages are listed using the checkbox below. As per policy, permanent deletion is not supported as it would break every project depending on the availability of the package.";
        //        // lblLoading
        // 
        lblLoading.AutoSize = true;
        lblLoading.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblLoading.ForeColor = Color.FromArgb(0, 122, 204);
        lblLoading.Location = new Point(180, 240); // Below the spinner, centered
        lblLoading.Name = "lblLoading";
        lblLoading.Size = new Size(353, 21);
        lblLoading.TabIndex = 14;
        lblLoading.Visible = false;//        // loadingSpinner
        // 
        loadingSpinner.Location = new Point(264, 200); // Center of the grid: 20+520/2-16, 120+200/2-16
        loadingSpinner.Name = "loadingSpinner";
        loadingSpinner.Size = new Size(32, 32);
        loadingSpinner.TabIndex = 15;
        loadingSpinner.SpinnerColor = Color.FromArgb(0, 120, 212);
        loadingSpinner.Thickness = 3;
        loadingSpinner.IsSpinning = false;
        // 
        // cmbQuerySource
        // 
        cmbQuerySource.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbQuerySource.Font = new Font("Segoe UI", 9F);
        cmbQuerySource.FormattingEnabled = true;
        cmbQuerySource.Location = new Point(126, 56);
        cmbQuerySource.Name = "cmbQuerySource";
        cmbQuerySource.Size = new Size(319, 23);
        cmbQuerySource.TabIndex = 4;
        // 
        // lblQuerySource
        // 
        lblQuerySource.AutoSize = true;
        lblQuerySource.Font = new Font("Segoe UI", 9F);
        lblQuerySource.Location = new Point(26, 60);
        lblQuerySource.Name = "lblQuerySource";
        lblQuerySource.Size = new Size(81, 15);
        lblQuerySource.TabIndex = 3;
        lblQuerySource.Text = "Query Source:";
        // 
        // lblQuerySourceHelp
        // 
        lblQuerySourceHelp.AutoSize = true;
        lblQuerySourceHelp.Cursor = Cursors.Help;
        lblQuerySourceHelp.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblQuerySourceHelp.ForeColor = Color.FromArgb(0, 122, 204);
        lblQuerySourceHelp.Location = new Point(455, 60);
        lblQuerySourceHelp.Name = "lblQuerySourceHelp";
        lblQuerySourceHelp.Size = new Size(20, 15);
        lblQuerySourceHelp.TabIndex = 25;
        lblQuerySourceHelp.Text = "🤔";
        lblQuerySourceHelp.Click += lblQuerySourceHelp_Click;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(564, 838);
        Controls.Add(lblPackage);
        Controls.Add(txtPackage);
        Controls.Add(lblQuerySource);
        Controls.Add(cmbQuerySource);
        Controls.Add(lblQuerySourceHelp);
        Controls.Add(btnQuery);
        Controls.Add(chkSelectAll);
        Controls.Add(btnSelectListed);
        Controls.Add(btnSelectUnlisted);
        Controls.Add(lblApiKey);
        Controls.Add(txtApiKey);
        Controls.Add(lblDeprecation);
        Controls.Add(btnDelete);
        Controls.Add(btnRelist);
        Controls.Add(btnCancel);
        Controls.Add(lblListingDescription);
        Controls.Add(dgvVersions);
        Controls.Add(grpReason);
        Controls.Add(lblLoading);
        Controls.Add(loadingSpinner);
        Font = new Font("Segoe UI", 9F);
        MinimumSize = new Size(580, 800);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "NuGet Package Manager";
        ((System.ComponentModel.ISupportInitialize)dgvVersions).EndInit();
        grpReason.ResumeLayout(false);
        grpReason.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private TextBox txtPackage;
    private Button btnQuery;
    private DataGridView dgvVersions;
    private CheckBox chkSelectAll;
    private TextBox txtApiKey;
    private Label lblApiKey;
    private Button btnDelete;
    private Button btnRelist;
    private Button btnCancel;
    private TextBox txtReason;
    private Label lblReason;
    private GroupBox grpReason;
    private CheckBox chkLegacy;
    private CheckBox chkCriticalBugs;
    private CheckBox chkOther;
    private Label lblAltPackage;
    private TextBox txtAltPackage;
    private Label lblAltVersion;
    private ComboBox cmbAltVersion;
    private Label lblPackage;
    private Button btnSelectListed;
    private Button btnSelectUnlisted;
    private Label lblDeprecation;
    private Label lblLoading;
    private LoadingSpinner loadingSpinner; private Label lblDeprecationDescription;
    private Label lblListingDescription; private ComboBox cmbQuerySource;
    private Label lblQuerySource;
    private Label lblQuerySourceHelp;
    private DataGridViewCheckBoxColumn colSelect;
    private DataGridViewTextBoxColumn colVersion;
    private DataGridViewTextBoxColumn colStatus;
}

#endregion