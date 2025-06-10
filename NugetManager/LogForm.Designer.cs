using NugetManager.UControls;

namespace NugetManager;

partial class LogForm
{    /// <summary>
     /// Required designer variable.
     /// </summary>
    private readonly System.ComponentModel.IContainer components = null;

    private RichTextBox txtLog;
    private Button btnClear;
    private Button btnClose;
    private Label lblStatus;
    private ModernProgressBar progressBar;

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        txtLog = new RichTextBox();
        btnClear = new Button();
        btnClose = new Button();
        progressBar = new ModernProgressBar();
        lblStatus = new Label();
        SuspendLayout();

        // 
        // txtLog
        // 
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.Font = new Font("Consolas", 9F, FontStyle.Regular);
        txtLog.Location = new Point(12, 45);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = RichTextBoxScrollBars.Both;
        txtLog.Size = new Size(660, 350);
        txtLog.TabIndex = 0;
        txtLog.BackColor = Color.FromArgb(40, 40, 40);
        txtLog.ForeColor = Color.White;
        txtLog.VScroll += OnTextBoxScroll;
        txtLog.TextChanged += OnTextChanged;
        txtLog.MouseWheel += OnMouseWheel;
        txtLog.KeyDown += OnKeyDown;

        // 
        // lblStatus
        // 
        lblStatus.AutoSize = true;
        lblStatus.Font = new Font("Segoe UI", 9F);
        lblStatus.Location = new Point(12, 12);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(43, 17);
        lblStatus.TabIndex = 1;
        lblStatus.Text = "Ready";        //        // progressBar
        // 
        progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        progressBar.Location = new Point(12, 410);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(500, 24);
        progressBar.TabIndex = 2; progressBar.ProgressColor = Color.FromArgb(0, 120, 212);
        progressBar.BackgroundColor = Color.FromArgb(230, 230, 230);
        progressBar.BorderRadius = 2;
        progressBar.ShowPercentage = false;
        progressBar.UseWin11Style = true;
        progressBar.UseGlow = false;

        // 
        // btnClear
        // 
        btnClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnClear.Location = new Point(530, 410);
        btnClear.Name = "btnClear";
        btnClear.Size = new Size(70, 23);
        btnClear.TabIndex = 3;
        btnClear.Text = "Clear"; btnClear.UseVisualStyleBackColor = true;

        // 
        // btnClose
        // 
        btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnClose.Location = new Point(610, 410);
        btnClose.Name = "btnClose";
        btnClose.Size = new Size(70, 23);
        btnClose.TabIndex = 4;
        btnClose.Text = "Close";
        btnClose.UseVisualStyleBackColor = true;

        // 
        // LogForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(684, 450);
        Controls.Add(btnClose);
        Controls.Add(btnClear);
        Controls.Add(progressBar);
        Controls.Add(lblStatus);
        Controls.Add(txtLog);
        MinimumSize = new Size(700, 400);
        Name = "LogForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Operation Log"; FormClosing += LogForm_FormClosing;
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}