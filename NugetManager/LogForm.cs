using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace NugetManager;

/// <inheritdoc />
public partial class LogForm : Form
{
    private readonly Form? parentForm;
    private bool autoScroll = true;
    private bool isUserScrolling;

    /// <inheritdoc />
    public LogForm(Form? parent = null)
    {
        parentForm = parent;
        InitializeComponent();
        InitializeCustomBehavior();
    }

    /// <summary>
    /// Get current status text
    /// </summary>
    public string CurrentStatus => lblStatus.Text;

    /// <summary>
    /// Initialize custom behavior after InitializeComponent
    /// </summary>
    private void InitializeCustomBehavior()
    {
        // Set window style and behavior
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        // Has parent form, auto dock to right side
        if (parentForm == null) return;
        PositionToRightOfParent();

        // Listen to parent form move and resize events
        parentForm.Move += OnParentFormMove;
        parentForm.Resize += OnParentFormResize;
        parentForm.FormClosed += OnParentFormClosed;

        // 注册鼠标事件
        txtLog.MouseDown += TxtLog_MouseDown;
        txtLog.MouseMove += TxtLog_MouseMove;
        txtLog.MouseLeave += TxtLog_MouseLeave;
        txtLog.ReadOnly = true; // 防止用户编辑
        txtLog.DetectUrls = false; // 禁用默认URL检测，使用自定义高亮
    }

    /// <summary>
    /// AppendLog
    /// </summary>
    /// <param name="message"></param>
    public void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(AppendLog), message);
            return;
        }
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var fullMessage = $"[{timestamp}] {message}";

        // 检查用户是否在底部
        CheckIfUserAtBottom();

        // 添加带颜色的文本
        AppendColoredText(fullMessage);

        // 只有在用户位于底部时才自动滚动
        if (!autoScroll) return;
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.ScrollToCaret();
    }

    // 正则表达式用于检测URL
    [GeneratedRegex(@"https?://[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    // 当前鼠标悬停的URL范围
    private (int start, int length)? hoveredUrlRange;

    private void AppendColoredText(string message)
    {
        // 保存当前选择
        var currentSelection = txtLog.SelectionStart;
        var currentLength = txtLog.SelectionLength;

        // 移动到文本末尾
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.SelectionLength = 0;

        // 先高亮URL
        foreach (Match match in UrlRegex().Matches(message))
        {
            // 先添加前面的普通文本
            if (match.Index > 0)
            {
                var before = message[..match.Index];
                AppendTextWithColor(before, txtLog.ForeColor);
            }
            // 添加URL文本（蓝色并带下划线）
            txtLog.SelectionColor = Color.RoyalBlue;
            txtLog.SelectionFont = new(txtLog.Font, FontStyle.Underline);
            txtLog.AppendText(match.Value);
            txtLog.SelectionFont = txtLog.Font;
            txtLog.SelectionColor = txtLog.ForeColor;
            txtLog.AppendText("\r\n");

            // 处理剩余文本
            message = message[(match.Index + match.Length)..];
            // 只处理第一个匹配，递归处理剩余文本
            if (message.Length > 0) AppendColoredText(message);
            return;
        }

        // 如果没有URL，按原有逻辑高亮关键词
        var patterns = new List<(string[] keywords, Color color)>
        {
            (["✓", "成功", "完成", "Success", "Completed", "OK", "Done"], Color.LimeGreen),
            (["×", "✗", "失败", "错误", "异常", "Error", "Failed", "Exception", "Fail"], Color.Red),
            (["⚠️", "⚠", "警告", "注意", "提醒", "Warning", "Caution", "Notice", "Alert", "Warn"], Color.Orange)
        };
        var processedMessage = ProcessMessageWithColors(message, patterns);
        txtLog.AppendText(processedMessage);
        txtLog.AppendText("\r\n");

        // 恢复原来的选择（如果用户有选择的话）
        if (autoScroll || currentSelection >= txtLog.Text.Length) return;
        txtLog.SelectionStart = currentSelection;
        txtLog.SelectionLength = Math.Min(currentLength, txtLog.Text.Length - currentSelection);
    }

    private void TxtLog_MouseMove(object? sender, MouseEventArgs e)
    {
        // 仅在按住Ctrl时才检测和高亮URL
        if (!ModifierKeys.HasFlag(Keys.Control))
        {
            ResetUrlHighlight();
            return;
        }

        var charIndex = txtLog.GetCharIndexFromPosition(e.Location);
        if (charIndex < 0 || charIndex >= txtLog.Text.Length)
        {
            ResetUrlHighlight();
            return;
        }

        // 查找鼠标下的URL
        foreach (Match match in UrlRegex().Matches(txtLog.Text))
        {
            if (charIndex < match.Index || charIndex >= match.Index + match.Length) continue;
            if (hoveredUrlRange != null && hoveredUrlRange.Value.start == match.Index) return;
            HighlightUrl(match.Index, match.Length);
            txtLog.Cursor = Cursors.Hand;
            return;
        }
        ResetUrlHighlight();
    }

    private void TxtLog_MouseLeave(object? sender, EventArgs e)
    {
        ResetUrlHighlight();
    }

    private void HighlightUrl(int start, int length)
    {
        // 取消之前的高亮
        if (hoveredUrlRange != null)
        {
            txtLog.SelectionStart = hoveredUrlRange.Value.start;
            txtLog.SelectionLength = hoveredUrlRange.Value.length;
            txtLog.SelectionBackColor = txtLog.BackColor;
        }
        // 设置新高亮
        txtLog.SelectionStart = start;
        txtLog.SelectionLength = length;
        txtLog.SelectionBackColor = Color.LightSkyBlue;
        hoveredUrlRange = (start, length);
    }

    private void ResetUrlHighlight()
    {
        if (hoveredUrlRange != null)
        {
            txtLog.SelectionStart = hoveredUrlRange.Value.start;
            txtLog.SelectionLength = hoveredUrlRange.Value.length;
            txtLog.SelectionBackColor = txtLog.BackColor;
            hoveredUrlRange = null;
        }
        txtLog.Cursor = Cursors.IBeam;
    }

    private void TxtLog_MouseDown(object? sender, MouseEventArgs e)
    {
        // 只有在按住Ctrl键时才允许点击打开链接
        if (e.Button != MouseButtons.Left || !ModifierKeys.HasFlag(Keys.Control)) return;
        var charIndex = txtLog.GetCharIndexFromPosition(e.Location);
        if (charIndex < 0 || charIndex >= txtLog.Text.Length) return;

        foreach (Match match in UrlRegex().Matches(txtLog.Text))
        {
            if (charIndex < match.Index || charIndex >= match.Index + match.Length) continue;
            try
            {
                Process.Start(new ProcessStartInfo(match.Value) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开链接: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            break;
        }
    }

    private string ProcessMessageWithColors(string message, List<(string[] keywords, Color color)> patterns)
    {
        var index = 0;
        while (index < message.Length)
        {
            var nextMatch = FindNextKeywordMatch(message, index, patterns);
            if (nextMatch.HasValue)
            {
                var (matchIndex, keyword, color) = nextMatch.Value;

                // 添加匹配前的普通文本
                if (matchIndex > index)
                {
                    var beforeText = message[index..matchIndex];
                    AppendTextWithColor(beforeText, txtLog.ForeColor);
                }

                // 添加彩色关键词
                AppendTextWithColor(keyword, color);
                index = matchIndex + keyword.Length;
            }
            else
            {
                // 没有更多匹配，添加剩余文本
                var remainingText = message[index..];
                AppendTextWithColor(remainingText, txtLog.ForeColor);
                break;
            }
        }
        return ""; // 已经直接输出到RichTextBox，返回空字符串
    }

    private void AppendTextWithColor(string text, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;
        txtLog.SelectionColor = color;
        txtLog.AppendText(text);
        txtLog.SelectionColor = txtLog.ForeColor; // 恢复默认颜色
    }

    private static (int index, string keyword, Color color)? FindNextKeywordMatch(string message, int startIndex, List<(string[] keywords, Color color)> patterns)
    {
        var bestMatch = (index: -1, keyword: "", color: Color.Black);
        var earliestIndex = int.MaxValue;
        foreach (var (keywords, color) in patterns)
        foreach (var keyword in keywords)
        {
            var index = message.IndexOf(keyword, startIndex, StringComparison.OrdinalIgnoreCase);
            if (index < 0 || index >= earliestIndex) continue;
            // 检查是否是完整的单词（对于英文关键词）
            if (!IsValidKeywordMatch(message, index, keyword)) continue;
            earliestIndex = index;
            bestMatch = (index, keyword, color);
        }
        return earliestIndex == int.MaxValue ? null : bestMatch;
    }

    private static bool IsValidKeywordMatch(string message, int index, string keyword)
    {
        // 对于emoji符号，直接匹配
        if (keyword.Length == 1 && keyword is "✓" or "×" or "✗" or "⚠")
            return true;
        if (keyword == "⚠️")
            return true;
        // 对于英文单词，检查边界（避免部分匹配）
        if (!IsEnglishKeyword(keyword)) return true;
        var beforeChar = index > 0 ? message[index - 1] : ' ';
        var afterChar = index + keyword.Length < message.Length ? message[index + keyword.Length] : ' ';
        return !char.IsLetter(beforeChar) && !char.IsLetter(afterChar);
        // 对于中文关键词，直接匹配
    }

    private static bool IsEnglishKeyword(string keyword)
    {
        return keyword.All(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
    }

    private void CheckIfUserAtBottom()
    {
        autoScroll = IsAtBottom();
    }

    private (int current, int max)? GetScrollInfo()
    {
        try
        {
            // 获取滚动条信息
            const int SB_VERT = 1;
            const int SIF_ALL = 0x17;
            var si = new SCROLLINFO
            {
                cbSize = Marshal.SizeOf<SCROLLINFO>(),
                fMask = SIF_ALL
            };
            if (GetScrollInfo(txtLog.Handle, SB_VERT, ref si)) return (si.nPos, (si.nMax - (int)si.nPage) + 1);
        }
        catch
        {
            // 忽略错误
        }
        return null;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetScrollInfo(IntPtr hwnd, int nBar, ref SCROLLINFO lpScrollInfo);

    private void OnTextBoxScroll(object? sender, EventArgs e)
    {
        // 用户手动滚动时，检查是否在底部
        isUserScrolling = true;
        CheckIfUserAtBottom();

        // 设置一个短暂的延迟来重置用户滚动状态
        Task.Delay(100).ContinueWith(_ =>
        {
            if (InvokeRequired)
                Invoke(() => isUserScrolling = false);
            else
                isUserScrolling = false;
        });
    }

    private void OnMouseWheel(object? sender, MouseEventArgs e)
    {
        // 鼠标滚轮滚动时，检查是否在底部
        isUserScrolling = true;

        // 短暂延迟后检查位置并重置滚动状态
        Task.Delay(50).ContinueWith(_ =>
        {
            if (InvokeRequired)
            {
                Invoke(() =>
                {
                    CheckIfUserAtBottom();
                    isUserScrolling = false;
                });
            }
            else
            {
                CheckIfUserAtBottom();
                isUserScrolling = false;
            }
        });
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        // 当文本改变时，如果不是用户滚动引起的，则重置滚动状态
        if (!isUserScrolling) CheckIfUserAtBottom();
        isUserScrolling = false;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // 检测特定按键（如End、Ctrl+End、PageDown等）
        if (e.KeyCode == Keys.End ||
            e is { Control: true, KeyCode: Keys.End } ||
            e.KeyCode == Keys.PageDown ||
            e.KeyCode == Keys.Down)
            // 短暂延迟后检查位置
            Task.Delay(50).ContinueWith(_ =>
            {
                if (InvokeRequired)
                    Invoke(CheckIfUserAtBottom);
                else
                    CheckIfUserAtBottom();
            });
    }

    /// <summary>
    /// SetStatus
    /// </summary>
    /// <param name="status"></param>
    public void SetStatus(string status)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(SetStatus), status);
            return;
        }
        lblStatus.Text = status;
    }

    /// <summary>
    /// SetProgress
    /// </summary>
    /// <param name="value"></param>
    /// <param name="maximum"></param>
    public void SetProgress(int value, int maximum = 100)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<int, int>(SetProgress), value, maximum);
            return;
        }
        progressBar.Maximum = maximum;
        progressBar.Value = Math.Min(value, maximum);
    }

    /// <summary>
    /// ResetProgress
    /// </summary>
    public void ResetProgress()
    {
        if (InvokeRequired)
        {
            Invoke(ResetProgress);
            return;
        }
        progressBar.Value = 0;
    }

    private void PositionToRightOfParent()
    {
        if (parentForm == null) return; // 计算位置：主窗体右边缘
        var x = parentForm.Location.X + parentForm.Width;
        var y = parentForm.Location.Y;

        // 确保窗口不会超出屏幕边界
        var screen = Screen.FromControl(parentForm);
        var maxX = screen.WorkingArea.Right - Width;
        var maxY = screen.WorkingArea.Bottom - Height;
        x = Math.Min(x, maxX);
        y = Math.Min(y, maxY);
        y = Math.Max(y, screen.WorkingArea.Top);
        Location = new(x, y);

        // 设置高度与主窗体相同
        Height = parentForm.Height;
    }

    private void OnParentFormMove(object? sender, EventArgs e)
    {
        if (Visible && parentForm != null) PositionToRightOfParent();
    }

    private void OnParentFormResize(object? sender, EventArgs e)
    {
        if (Visible && parentForm != null) PositionToRightOfParent();
    }

    private void OnParentFormClosed(object? sender, FormClosedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// 窗体关闭时的处理，防止阻止主程序关闭
    /// </summary>
    private void LogForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // 如果是用户点击关闭按钮，则隐藏窗体而不是真正关闭
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        // 如果是父窗体关闭或程序退出，则允许真正关闭
        else
        {
            e.Cancel = false;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (parentForm != null)
            {
                // Unsubscribe from events
                parentForm.Move -= OnParentFormMove;
                parentForm.Resize -= OnParentFormResize;
                parentForm.FormClosed -= OnParentFormClosed;
            }
            // Dispose managed resources
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// 更精确地检查是否在底部 - 使用文本框自身的滚动信息
    /// </summary>
    private bool IsAtBottom()
    {
        try
        {
            if (txtLog.Lines.Length == 0) return true;

            // 方法1：使用Win32 API获取滚动信息
            var scrollInfo = GetScrollInfo();
            if (scrollInfo.HasValue)
            {
                var (current, max) = scrollInfo.Value;
                return max - current <= 5; // 允许5行的误差
            }

            // 方法2：使用RichTextBox的位置信息
            var lastCharIndex = txtLog.Text.Length - 1;
            if (lastCharIndex < 0) return true;
            var lastCharPos = txtLog.GetPositionFromCharIndex(lastCharIndex);
            var visibleRect = txtLog.ClientRectangle;

            // 如果最后一个字符在可视区域内或接近底部
            return lastCharPos.Y <= visibleRect.Bottom + 20;
        }
        catch
        {
            return true; // 出错时默认认为在底部
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SCROLLINFO
    {
        public int cbSize;
        public int fMask;
        public int nMin;
        public int nMax;
        public uint nPage;
        public int nPos;
        public int nTrackPos;
    }
}