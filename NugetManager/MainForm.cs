using System.Diagnostics;
using NugetManager.Services;

// ReSharper disable AsyncVoidEventHandlerMethod

namespace NugetManager;

/// <inheritdoc />
public partial class MainForm : Form
{
    private PackageOperationService? _operationService;

    // Services
    private PackageVersionManager? _versionManager;
    private CancellationTokenSource? cancellationTokenSource;
    private LogForm? logForm;

    /// <inheritdoc />
    public MainForm()
    {
        InitializeComponent();
        InitializeServices();
        InitializeUI();
    }

    /// <summary>
    /// 初始化服务
    /// </summary>
    private void InitializeServices()
    {
        Action<string> logAction = message => logForm?.AppendLog(message);
        _versionManager = new(logAction);
        _operationService = new(logAction);
    }

    /// <summary>
    /// 初始化UI控件
    /// </summary>
    private void InitializeUI()
    {
        // Initialize disabled alternative package, version and custom description
        SetOtherReasonControls(false);
        dgvVersions.CellValueChanged += dgvVersions_CellValueChanged;
        dgvVersions.CurrentCellDirtyStateChanged += dgvVersions_CurrentCellDirtyStateChanged;
        btnDelete.Visible = false;
        btnRelist.Visible = false;

        // Force adjust Z-order, ensure GroupBox is displayed in foreground
        grpReason.BringToFront();
        lblDeprecation.BringToFront();

        // Add form closing event to ensure log form closes synchronously
        FormClosing += Form1_FormClosing;

        // 初始化查询源选择
        InitializeQuerySourceComboBox();
    }

    /// <summary>
    /// 初始化查询源下拉框
    /// </summary>
    private void InitializeQuerySourceComboBox()
    {
        cmbQuerySource.Items.Clear();
        cmbQuerySource.Items.Add("Package Base Address API (Recommended)");
        cmbQuerySource.Items.Add("Web Scraping (nuget.org)");
        cmbQuerySource.SelectedIndex = 0; // 默认选择第一个
    }

    /// <summary>
    /// 窗体关闭时，强制关闭日志窗体并清理临时文件
    /// </summary>
    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // 取消所有操作
        cancellationTokenSource?.Cancel();

        // 强制关闭日志窗体
        if (logForm is { IsDisposed: false })
        {
            logForm.Close();
            logForm.Dispose();
        }

        // 清理临时提取的nuget.exe文件
        CleanupTempNugetFiles();
    }

    /// <summary>
    /// 清理临时提取的nuget.exe文件
    /// </summary>
    private static void CleanupTempNugetFiles()
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "NugetManager");
            if (!Directory.Exists(tempPath)) return;
            var nugetExePath = Path.Combine(tempPath, "nuget.exe");
            if (File.Exists(nugetExePath))
            {
                File.Delete(nugetExePath);
                Debug.WriteLine($"Cleaned up temp nuget.exe: {nugetExePath}");
            }

            // 如果目录为空，删除目录
            if (Directory.EnumerateFileSystemEntries(tempPath).Any()) return;
            Directory.Delete(tempPath);
            Debug.WriteLine($"Cleaned up temp directory: {tempPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CleanupTempNugetFiles failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 查询包所有版本及Listed状态（根据用户选择的查询源）
    /// </summary>
    private async Task<List<(string Version, bool Listed)>> QueryAllVersionsWithStatusAsync(string packageName)
    {
        if (_versionManager == null)
        {
            throw new InvalidOperationException("Version manager not initialized");
        }
        var querySource = cmbQuerySource.SelectedIndex;
        return await _versionManager.QueryAllVersionsWithStatusAsync(packageName, querySource);
    }

    // 查询按钮点击事件（异步，带loading）
    private async void btnQuery_Click(object sender, EventArgs e)
    {
        var pkg = txtPackage.Text.Trim();
        if (string.IsNullOrEmpty(pkg))
        {
            MessageBox.Show("Please enter a package name.", "Info");
            return;
        } // 显示loading状态 - 隐藏列表，显示loading提示
        dgvVersions.Visible = false;
        lblLoading.Visible = true;
        loadingSpinner.IsSpinning = true;
        btnQuery.Enabled = false;
        btnQuery.Text = "Searching...";

        // 隐藏批量操作按钮
        btnDelete.Visible = false;
        btnRelist.Visible = false; // 确保日志窗口存在并显示查询过程
        logForm ??= new(this);
        logForm.Show();
        logForm.AppendLog($"=== Querying package: {pkg} ===");
        logForm.AppendLog("Trying multiple API strategies for complete version discovery...");
        dgvVersions.Rows.Clear();
        logForm.AppendLog("Starting comprehensive version query...");
        var versions = await QueryAllVersionsWithStatusAsync(pkg);
        logForm.AppendLog($"Query completed, processing {versions.Count} unique versions...");
        foreach (var v in versions) dgvVersions.Rows.Add(false, v.Version, v.Listed ? "Listed" : "Unlisted"); // 隐藏loading状态，显示结果
        lblLoading.Visible = false;
        loadingSpinner.IsSpinning = false;
        btnQuery.Enabled = true;
        btnQuery.Text = "Search";
        dgvVersions.Visible = true; // 记录查询结果
        var listedCount = versions.Count(v => v.Listed);
        var unlistedCount = versions.Count(v => !v.Listed);
        logForm.AppendLog($"=== Query completed: {versions.Count} versions found ===");
        logForm.AppendLog($"Listed: {listedCount}, Unlisted: {unlistedCount}");

        // 显示一些示例版本以供验证
        if (versions.Count > 0)
        {
            logForm.AppendLog("First 5 versions found:");
            var sample = versions.Take(5);
            foreach (var v in sample) logForm.AppendLog($"  {v.Version} - {(v.Listed ? "Listed" : "Unlisted")}");
        }
        switch (versions.Count)
        {
            case 0:
                MessageBox.Show("No versions found for this package. Please check the package name.", "Info");
                logForm.AppendLog("⚠️  No versions found. Package may not exist or API may be unavailable.");
                break;
            case < 10:
                logForm.AppendLog($"⚠️  Only {versions.Count} versions found. This seems low for most packages.");
                logForm.AppendLog("This might indicate API limitations or package-specific issues.");
                break;
            default:
                logForm.AppendLog($"✓ Successfully loaded {versions.Count} versions");
                break;
        }
        UpdateButtonTexts();
    } // 替代包输入框失去焦点时，仅查询Listed版本（异步）

    private async void txtAltPackage_Leave(object sender, EventArgs e)
    {
        var pkg = txtAltPackage.Text.Trim();
        if (string.IsNullOrEmpty(pkg)) return;
        var allVersions = await QueryAllVersionsWithStatusAsync(pkg);
        var listedVersions = allVersions.Where(v => v.Listed).Select(v => v.Version).ToList();
        cmbAltVersion.Items.Clear();
        foreach (var v in listedVersions)
            cmbAltVersion.Items.Add(v);
        if (cmbAltVersion.Items.Count > 0)
            cmbAltVersion.SelectedIndex = 0;
    } // 批量删除按钮事件（异步） - 改进为真正的Deprecate功能

    private async void btnDelete_Click(object sender, EventArgs e)
    {
        var pkg = txtPackage.Text.Trim();
        var apiKey = txtApiKey.Text.Trim();

        // 检查列是否存在
        if (dgvVersions.Columns["colSelect"] == null || dgvVersions.Columns["colVersion"] == null)
        {
            MessageBox.Show("DataGridView columns not properly initialized.", "Error");
            return;
        }
        var selectedVersions = dgvVersions.Rows.Cast<DataGridViewRow>()
                                          .Where(r => r.Cells["colSelect"].Value != null && Convert.ToBoolean(r.Cells["colSelect"].Value))
                                          .Select(r => r.Cells["colVersion"].Value?.ToString())
                                          .Where(v => !string.IsNullOrEmpty(v)).ToList();
        if (string.IsNullOrEmpty(pkg) || string.IsNullOrEmpty(apiKey) || selectedVersions.Count == 0)
        {
            MessageBox.Show("Please enter package name, API Key and select versions to deprecate.", "Info");
            return;
        }

        // 收集Deprecation信息
        var deprecationInfo = GetDeprecationInfo();
        if (deprecationInfo == null)
        {
            MessageBox.Show("Please select at least one deprecation reason.", "Info");
            return;
        }

        // 创建新取消令牌
        if (cancellationTokenSource is not null) await cancellationTokenSource.CancelAsync();
        cancellationTokenSource = new();
        var cancellationToken = cancellationTokenSource.Token;

        // 确保日志窗口存在并显示
        logForm ??= new(this);
        logForm.Show(); // 重置进度
        logForm.ResetProgress();
        logForm.SetStatus($"Starting deprecation of {selectedVersions.Count} versions...");
        logForm.AppendLog($"Starting batch deprecation for package: {pkg}");
        logForm.AppendLog($"Selected versions: {string.Join(", ", selectedVersions)}");
        logForm.AppendLog($"Deprecation info: {deprecationInfo}");
        logForm.AppendLog("");
        logForm.AppendLog("ℹ️  Note: NuGet.org deprecation API may not be available.");
        logForm.AppendLog("   If API fails, the tool will fallback to 'unlist' method.");
        logForm.AppendLog("   Unlisted packages have similar effect to deprecated packages.");
        logForm.AppendLog("");

        // 禁用相关按钮，启用取消按钮
        btnDelete.Enabled = false;
        btnRelist.Enabled = false;
        btnQuery.Enabled = false;
        btnCancel.Visible = true;
        btnCancel.Enabled = true;
        try
        {
            if (_operationService == null)
            {
                throw new InvalidOperationException("Operation service not initialized");
            }

            // 创建进度回调，更新日志窗口的进度条和状态
            void ProgressCallback(int current, int total)
            {
                if (logForm == null) return;
                logForm.SetProgress(current, total);
                logForm.SetStatus($"Processing {current}/{total} versions...");
            }

            // 使用服务执行弃用操作
            var success = await _operationService.DeprecatePackageVersionsAsync(pkg, apiKey, selectedVersions!, deprecationInfo, cancellationToken, ProgressCallback);
            if (cancellationToken.IsCancellationRequested)
            {
                logForm.SetStatus("Operation cancelled");
                logForm.AppendLog("=== Batch deprecation cancelled ===");
                MessageBox.Show("Batch deprecation was cancelled.", "Cancelled");
            }
            else if (success)
            {
                logForm.SetProgress(selectedVersions.Count, selectedVersions.Count); // 确保进度条显示100%
                logForm.SetStatus("Deprecation completed successfully!");
                logForm.AppendLog("=== Batch deprecation completed ===");
                logForm.AppendLog("ℹ️  Note: If deprecation API was unavailable, packages were unlisted instead.");
                logForm.AppendLog("   Unlisted packages won't appear in search results (similar to deprecation).");
                logForm.AppendLog("⚠️ Status synchronization may take some time, immediate refresh may not show latest changes");
                logForm.AppendLog("💡 Suggestion: Wait 1-2 minutes before querying latest status");
                MessageBox.Show("Batch deprecation completed. Check log for details.\n\nNote: If deprecation API was unavailable, packages were unlisted instead.\nStatus synchronization may take some time. Consider waiting 1-2 minutes before refreshing.", "Info");

                // 自动刷新版本列表
                await RefreshVersionList();
            }
            else
            {
                logForm.SetStatus("Deprecation completed with errors");
                logForm.AppendLog("⚠️ Some deprecation operations failed");
                logForm.AppendLog("💡 Tip: For manual deprecation, you can use nuget.org web interface:");
                logForm.AppendLog($"   https://www.nuget.org/packages/{pkg}/manage");
                MessageBox.Show("Batch deprecation completed with errors. Check log for details.\n\nFor manual deprecation, you can use the NuGet.org web interface.", "Warning");
            }
        }
        catch (OperationCanceledException)
        {
            logForm.SetStatus("Operation cancelled");
            logForm.AppendLog("=== Batch deprecation cancelled ===");
            MessageBox.Show("Batch deprecation was cancelled.", "Cancelled");
        }
        catch (Exception ex)
        {
            logForm.AppendLog($"FATAL ERROR: {ex.Message}");
            MessageBox.Show($"Error during deprecation: {ex.Message}", "Error");
        }
        finally
        {
            // 重新启用按钮，隐藏取消按钮
            btnDelete.Enabled = true;
            btnRelist.Enabled = true;
            btnQuery.Enabled = true;
            btnCancel.Visible = false;
            btnCancel.Enabled = false;
        }
    }

    // 批量Unlist按钮事件（异步）
    private async void btnRelist_Click(object sender, EventArgs e)
    {
        var pkg = txtPackage.Text.Trim();
        var apiKey = txtApiKey.Text.Trim();

        // 检查列是否存在
        if (dgvVersions.Columns["colSelect"] == null)
        {
            MessageBox.Show("DataGridView columns not properly initialized.", "Error");
            return;
        }
        var selectedRows = dgvVersions.Rows.Cast<DataGridViewRow>()
                                      .Where(r => r.Cells["colSelect"].Value != null && Convert.ToBoolean(r.Cells["colSelect"].Value))
                                      .ToList();
        if (string.IsNullOrEmpty(pkg) || string.IsNullOrEmpty(apiKey) || selectedRows.Count == 0)
        {
            MessageBox.Show("Please enter package name, API Key and select versions to unlist.", "Info");
            return;
        }

        // 只处理Listed版本（可以unlist的）
        var listedVersions = selectedRows
                             .Where(r => r.Cells["colStatus"].Value?.ToString() == "Listed")
                             .Select(r => r.Cells["colVersion"].Value?.ToString())
                             .Where(v => !string.IsNullOrEmpty(v))
                             .ToList();
        var unlistedVersions = selectedRows
                               .Where(r => r.Cells["colStatus"].Value?.ToString() == "Unlisted")
                               .Select(r => r.Cells["colVersion"].Value?.ToString())
                               .Where(v => !string.IsNullOrEmpty(v))
                               .ToList();
        if (listedVersions.Count == 0)
        {
            MessageBox.Show(unlistedVersions.Count > 0 ? "All selected versions are already unlisted. Cannot unlist them again.\n\nTo re-list unlisted packages, you need to re-publish them using:\nnuget push <package-file> -ApiKey <your-api-key> -Source https://api.nuget.org/v3/index.json" : "No listed versions selected to unlist.", "Info");
            return;
        }

        // 创建新取消令牌
        if (cancellationTokenSource is not null) await cancellationTokenSource.CancelAsync();
        cancellationTokenSource = new();
        var cancellationToken = cancellationTokenSource.Token;

        // 确保日志窗口存在并显示
        logForm ??= new(this);
        logForm.Show();

        // 重置进度
        logForm.ResetProgress();
        logForm.SetStatus("Starting batch unlist operation...");
        logForm.AppendLog($"Starting batch unlist for package: {pkg}");
        logForm.AppendLog($"Versions to unlist: {string.Join(", ", listedVersions)}");
        if (unlistedVersions.Count > 0)
        {
            logForm.AppendLog($"Already unlisted versions (skipped): {string.Join(", ", unlistedVersions)}");
        }

        // 禁用相关按钮，启用取消按钮
        btnDelete.Enabled = false;
        btnRelist.Enabled = false;
        btnQuery.Enabled = false;
        btnCancel.Visible = true;
        btnCancel.Enabled = true;
        try
        {
            if (_operationService == null)
            {
                throw new InvalidOperationException("Operation service not initialized");
            }

            // 创建进度回调，更新日志窗口的进度条和状态
            void ProgressCallback(int current, int total)
            {
                if (logForm == null) return;
                logForm.SetProgress(current, total);
                logForm.SetStatus($"Unlisting {current}/{total} versions...");
            }

            // 使用服务执行删除操作
            var validVersions = listedVersions.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            var success = await _operationService.DeletePackageVersionsAsync(pkg, apiKey, validVersions, cancellationToken, ProgressCallback);
            logForm.AppendLog(success ? "✓ All versions unlisted successfully" : "⚠️ Some versions failed to unlist");

            // 提示无法处理的unlisted版本
            if (unlistedVersions.Count > 0)
            {
                logForm.AppendLog("");
                logForm.AppendLog("=== Note about already unlisted versions ===");
                foreach (var v in unlistedVersions)
                {
                    logForm.AppendLog($"⚠️  Version {v} is already unlisted.");
                }
                logForm.AppendLog("   To re-list unlisted versions, you need to re-publish them using:");
                logForm.AppendLog("   nuget push <package-file> -ApiKey <your-api-key> -Source https://api.nuget.org/v3/index.json");
                logForm.AppendLog("   Note: You need the original .nupkg files for re-publishing");
            }
            if (cancellationToken.IsCancellationRequested)
            {
                logForm.SetStatus("Operation cancelled");
                logForm.AppendLog("=== Batch unlist cancelled ===");
                MessageBox.Show("Batch unlist was cancelled.", "Cancelled");
            }
            else
            {
                logForm.SetProgress(validVersions.Count, validVersions.Count); // 确保进度条显示100%
                logForm.SetStatus("Unlist operation completed");
                logForm.AppendLog("=== Batch unlist completed ===");
                MessageBox.Show("Batch unlist completed. Check log for details.\n\nNote: Status synchronization may take some time. Consider waiting 1-2 minutes before refreshing.", "Info");

                // 自动刷新版本列表
                await RefreshVersionList();
            }
        }
        catch (OperationCanceledException)
        {
            logForm.SetStatus("Operation cancelled");
            logForm.AppendLog("=== Batch unlist cancelled ===");
            MessageBox.Show("Batch unlist was cancelled.", "Cancelled");
        }
        catch (Exception ex)
        {
            logForm.AppendLog($"FATAL ERROR: {ex.Message}");
            MessageBox.Show($"Error during unlist: {ex.Message}", "Error");
        }
        finally
        {
            // 重新启用按钮，隐藏取消按钮
            btnDelete.Enabled = true;
            btnRelist.Enabled = true;
            btnQuery.Enabled = true;
            btnCancel.Visible = false;
            btnCancel.Enabled = false;
        }
    }

    /// <summary>
    /// 获取用户选择的Deprecation信息
    /// </summary>
    private string? GetDeprecationInfo()
    {
        var reasons = new List<string>();
        if (chkCriticalBugs.Checked) reasons.Add("Critical bugs");
        if (chkLegacy.Checked) reasons.Add("Legacy");
        if (chkOther.Checked && !string.IsNullOrWhiteSpace(txtReason.Text)) reasons.Add($"Other: {txtReason.Text.Trim()}");
        if (reasons.Count == 0) return null;
        var info = $"Reasons: {string.Join(", ", reasons)}";

        // 添加替代包信息（如果提供）
        var altPackage = txtAltPackage.Text.Trim();
        var altVersion = cmbAltVersion.Text.Trim();
        if (string.IsNullOrEmpty(altPackage)) return info;
        info += $"; Alternative package: {altPackage}";
        if (!string.IsNullOrEmpty(altVersion)) info += $" v{altVersion}";
        return info;
    }

    /// <summary>
    /// 刷新版本列表
    /// </summary>
    private async Task RefreshVersionList()
    {
        var pkg = txtPackage.Text.Trim();
        if (string.IsNullOrEmpty(pkg)) return;
        logForm?.AppendLog("Refreshing version list...");
        dgvVersions.Rows.Clear();
        var versions = await QueryAllVersionsWithStatusAsync(pkg);
        foreach (var v in versions) dgvVersions.Rows.Add(false, v.Version, v.Listed ? "Listed" : "Unlisted");
        logForm?.AppendLog($"Refreshed: {versions.Count} versions found");

        // 检查是否是在操作完成后的自动刷新，如果是，提示用户状态同步信息        
        var currentStatus = logForm?.CurrentStatus ?? "";
        if (currentStatus.Contains("completed") || currentStatus.Contains("operation completed")) logForm?.AppendLog("💡 If status not updated in time, please wait 1-2 minutes then refresh manually");
    }

    // 其他原因勾选事件，控制替代包、版本和自定义说明的可用性
    private void chkOther_CheckedChanged(object sender, EventArgs e)
    {
        SetOtherReasonControls(chkOther.Checked);
    }

    private void SetOtherReasonControls(bool enabled)
    {
        txtReason.Enabled = enabled;
        lblReason.Enabled = enabled;
    }

    private void chkSelectAll_CheckedChanged(object sender, EventArgs e)
    {
        // 检查列是否存在
        if (dgvVersions.Columns["colSelect"] == null)
            return;
        var check = chkSelectAll.Checked;
        foreach (DataGridViewRow row in dgvVersions.Rows) row.Cells["colSelect"].Value = check;
        UpdateButtonTexts();
    }
    
    // DataGridView选中行变化时，自动显示/隐藏批量操作按钮

    private void dgvVersions_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        var colSel = dgvVersions.Columns["colSelect"];
        if (colSel == null || e.ColumnIndex != colSel.Index) return;
        var anyChecked = dgvVersions.Rows.Cast<DataGridViewRow>()
                                    .Any(r => r.Cells["colSelect"].Value != null && Convert.ToBoolean(r.Cells["colSelect"].Value));
        btnDelete.Visible = anyChecked;
        btnRelist.Visible = anyChecked;
        UpdateButtonTexts();
    }

    private void dgvVersions_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (dgvVersions.IsCurrentCellDirty) dgvVersions.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    // 取消操作按钮事件
    private void btnCancel_Click(object? sender, EventArgs e)
    {
        if (cancellationTokenSource is not { Token.IsCancellationRequested: false }) return;
        cancellationTokenSource.Cancel();
        logForm?.AppendLog("=== User requested cancellation ===");
        logForm?.SetStatus("Cancelling operation...");
    }

    /// <summary>
    /// 更新按钮文本基于选中的版本状态
    /// </summary>
    private void UpdateButtonTexts()
    {
        // 检查列是否存在
        if (dgvVersions.Columns["colSelect"] == null)
        {
            logForm?.AppendLog("× Error: colSelect column not found in DataGridView");
            return;
        }
        var selectedRows = dgvVersions.Rows.Cast<DataGridViewRow>()
                                      .Where(r => r.Cells["colSelect"].Value != null && Convert.ToBoolean(r.Cells["colSelect"].Value))
                                      .ToList();
        if (selectedRows.Count == 0)
        {
            btnDelete.Text = "Deprecate";
            btnRelist.Text = "Unlist Selected";
            return;
        }
        var listedCount = selectedRows.Count(r => r.Cells["colStatus"].Value?.ToString() == "Listed");

        // 更新Deprecate按钮文本
        btnDelete.Text = $"Deprecate ({selectedRows.Count})";

        // 更新Unlist按钮文本
        btnRelist.Text = listedCount > 0 ? $"Unlist ({listedCount})" : "Unlist Selected";
    }

    /// <summary>
    /// 查询源帮助提示点击事件
    /// </summary>
    private void lblQuerySourceHelp_Click(object? sender, EventArgs e)
    {
        const string helpMessage = """
                                   📋 Query Source Information:

                                   🔹 Package Base Address API (Recommended)
                                      • Fastest and most stable official API
                                      • Can only retrieve Listed versions
                                      • Recommended for daily queries

                                   🔸 Enhanced V3 Registration API (including Unlisted)
                                      • Can retrieve all versions including Unlisted
                                      • Fast speed, but status may not be latest
                                      • Recommended for complete history

                                   🔹 NuGet CLI Tool
                                      • Uses local NuGet tool for queries
                                      • Gets most accurate Listed status
                                      • Slower but most reliable data

                                   🔸 Web Scraping (nuget.org)
                                      • Scrapes official website for data
                                      • Can only get Listed versions (when not logged in)
                                      • Slow and unstable, not recommended

                                   🔹 Comprehensive API Search (all sources)
                                      • Tries all available query methods
                                      • Gets most complete version list
                                      • Takes longer but most comprehensive results

                                   💡 Suggestions:
                                   • Daily use: Choose "Package Base Address API"
                                   • Finding Unlisted versions: Choose "Enhanced V3 Registration API"
                                   • Need most accurate status: Choose "Comprehensive API Search"

                                   ⚠️ Note: Some sources' status information may take time to sync from official servers!
                                   """;
        MessageBox.Show(helpMessage, "Query Source Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void linkGithub_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/joesdu/NugetManager") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开链接: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();

        // Close log window and cancel token
        if (disposing)
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            logForm?.Close();
            logForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}