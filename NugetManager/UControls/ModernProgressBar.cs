using System.Drawing.Drawing2D;
// ReSharper disable UnusedMember.Global

namespace NugetManager.UControls;

/// <summary>
/// Custom modern progress bar with enhanced visual appearance
/// </summary>
public class ModernProgressBar : Control
{
    private int _value;
    private int _maximum = 100;
    private int _minimum; private Color _progressColor = Color.FromArgb(0, 120, 212); // Windows 11 Blue
    private Color _progressSecondaryColor = Color.FromArgb(0, 99, 177); // Darker Windows 11 Blue
    private Color _backgroundColor = Color.FromArgb(230, 230, 230); // More visible gray background
    private int _borderRadius = 2; // Very subtle radius like Win11
    private const int _borderWidth = 0; // No border like Win11
    private bool _showPercentage; // Default to no percentage display
    private readonly Font _textFont = new("Segoe UI Variable Text", 9F, FontStyle.Regular);
    private Color _textColor = Color.FromArgb(50, 50, 50);
    private bool _useGlow; // Win11 style is clean without glow
    private Color _glowColor = Color.FromArgb(100, 33, 150, 243);
    private bool _useWin11Style = true; // Enable Windows 11 styling

    // Marquee animation properties
    private bool _isMarquee;
    private System.Windows.Forms.Timer? _marqueeTimer;
    private float _marqueePosition;
    private const int _marqueeWidth = 50;
    private const int _marqueeSpeed = 3;

    // Animation properties
    private System.Windows.Forms.Timer? _animationTimer;
    private int _targetValue;
    private float _currentAnimatedValue;
    private bool _enableAnimation = true;
    private const int _animationSpeed = 8; // Windows 11 style interaction states
    private float _hoverOpacity;
    private System.Windows.Forms.Timer? _hoverTimer;

    /// <summary>
    /// Initializes a new instance of the ModernProgressBar class
    /// </summary>
    public ModernProgressBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        Size = new(300, 30);
    }    /// <summary>
         /// Gets or sets the current value of the progress bar
         /// </summary>
    [System.ComponentModel.DefaultValue(0)]
    public int Value
    {
        get => _value;
        set
        {
            if (value < _minimum) value = _minimum;
            if (value > _maximum) value = _maximum;

            if (_enableAnimation && !DesignMode)
            {
                AnimateToValue(value);
            }
            else
            {
                _value = value;
                _currentAnimatedValue = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the maximum value of the progress bar
    /// </summary>
    [System.ComponentModel.DefaultValue(100)]
    public int Maximum
    {
        get => _maximum;
        set
        {
            if (value < _minimum) value = _minimum;
            _maximum = value;
            if (_value > _maximum) _value = _maximum;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the minimum value of the progress bar
    /// </summary>
    [System.ComponentModel.DefaultValue(0)]
    public int Minimum
    {
        get => _minimum;
        set
        {
            if (value > _maximum) value = _maximum;
            _minimum = value;
            if (_value < _minimum) _value = _minimum;
            Invalidate();
        }
    }    /// <summary>
         /// Gets or sets the color of the progress bar
         /// </summary>
    [System.ComponentModel.DefaultValue(typeof(Color), "33, 150, 243")]
    public Color ProgressColor
    {
        get => _progressColor;
        set { _progressColor = value; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the secondary color for progress gradient
    /// </summary>
    [System.ComponentModel.DefaultValue(typeof(Color), "25, 118, 210")]
    public Color ProgressSecondaryColor
    {
        get => _progressSecondaryColor;
        set { _progressSecondaryColor = value; Invalidate(); }
    }/// <summary>
     /// Gets or sets the background color of the progress bar
     /// </summary>
    [System.ComponentModel.DefaultValue(typeof(Color), "248, 249, 250")]
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the border color of the progress bar
    /// </summary>
    [System.ComponentModel.DefaultValue(typeof(Color), "218, 220, 224")]
    public Color BorderColor
    {
        get;
        set
        {
            field = value;
            Invalidate();
        }
    } = Color.FromArgb(0, 0, 0, 0);

    /// <summary>
    /// Gets or sets the border radius of the progress bar
    /// </summary>
    [System.ComponentModel.DefaultValue(12)]
    public int BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = Math.Max(0, value); Invalidate(); }
    }

    /// <summary>
    /// Gets or sets whether to show percentage text
    /// </summary>
    [System.ComponentModel.DefaultValue(true)]
    public bool ShowPercentage
    {
        get => _showPercentage;
        set { _showPercentage = value; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the color of the percentage text
    /// </summary>
    [System.ComponentModel.DefaultValue(typeof(Color), "32, 33, 36")]
    public Color TextColor
    {
        get => _textColor;
        set { _textColor = value; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets whether the progress bar should show marquee animation
    /// </summary>
    [System.ComponentModel.DefaultValue(false)]
    public bool IsMarquee
    {
        get => _isMarquee;
        set
        {
            if (_isMarquee == value) return;
            _isMarquee = value;

            if (_isMarquee)
            {
                StartMarquee();
            }
            else
            {
                StopMarquee();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to use glow effect
    /// </summary>
    [System.ComponentModel.DefaultValue(true)]
    public bool UseGlow
    {
        get => _useGlow;
        set { _useGlow = value; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the glow color
    /// </summary>
    [System.ComponentModel.DefaultValue(typeof(Color), "100, 33, 150, 243")]
    public Color GlowColor
    {
        get => _glowColor;
        set { _glowColor = value; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets whether to enable smooth animation
    /// </summary>
    [System.ComponentModel.DefaultValue(true)]
    public bool EnableAnimation
    {
        get => _enableAnimation;
        set => _enableAnimation = value;
    }

    /// <summary>
    /// Gets or sets whether to use Windows 11 style
    /// </summary>
    [System.ComponentModel.DefaultValue(true)]
    public bool UseWin11Style
    {
        get => _useWin11Style;
        set { _useWin11Style = value; Invalidate(); }
    }

    private void StartMarquee()
    {
        if (_marqueeTimer != null) return;

        _marqueeTimer = new() { Interval = 30 };
        _marqueeTimer.Tick += (_, _) =>
        {
            _marqueePosition += _marqueeSpeed;
            if (_marqueePosition > Width + _marqueeWidth)
                _marqueePosition = -_marqueeWidth;
            Invalidate();
        };
        _marqueeTimer.Start();
    }

    private void StopMarquee()
    {
        _marqueeTimer?.Stop();
        _marqueeTimer?.Dispose();
        _marqueeTimer = null;
        _marqueePosition = 0;
        Invalidate();
    }

    private void AnimateToValue(int targetValue)
    {
        _targetValue = targetValue;

        if (_animationTimer != null)
        {
            _animationTimer.Stop();
            _animationTimer.Dispose();
        }

        _animationTimer = new() { Interval = 16 }; // ~60 FPS
        _animationTimer.Tick += AnimationTimer_Tick;
        _animationTimer.Start();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        var difference = _targetValue - _currentAnimatedValue;

        if (Math.Abs(difference) < 0.5f)
        {
            _currentAnimatedValue = _targetValue;
            _value = _targetValue;
            _animationTimer?.Stop();
            _animationTimer?.Dispose();
            _animationTimer = null;
        }
        else
        {
            _currentAnimatedValue += difference * _animationSpeed / 100f;
            _value = (int)Math.Round(_currentAnimatedValue);
        }

        Invalidate();
    }
    /// <summary>
    /// Handles the paint event for the progress bar
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

        if (_useWin11Style)
        {
            DrawWin11Style(e.Graphics);
        }
        else
        {
            DrawLegacyStyle(e.Graphics);
        }
    }
    private void DrawWin11Style(Graphics g)
    {
        // Windows 11 style: very subtle background, minimal borders
        // Background track - thinner like Win11 scrollbars, centered vertically
        var trackHeight = Math.Max(4, Height / 4); // Minimum 4px height
        var trackY = (Height - trackHeight) / 2;
        var trackRect = new Rectangle(0, trackY, Width, trackHeight);
        // Add hover effect to background track
        var bgColor = _backgroundColor;
        if (_hoverOpacity > 0)
        {
            var hoverColor = Color.FromArgb(200, 200, 200); // Darker hover color for better contrast
            bgColor = Color.FromArgb(
                (int)(_backgroundColor.R * (1 - _hoverOpacity) + hoverColor.R * _hoverOpacity),
                (int)(_backgroundColor.G * (1 - _hoverOpacity) + hoverColor.G * _hoverOpacity),
                (int)(_backgroundColor.B * (1 - _hoverOpacity) + hoverColor.B * _hoverOpacity));
        }

        using var trackBrush = new SolidBrush(bgColor);
        var trackPath = GetRoundedRectPath(trackRect, Math.Min(_borderRadius, trackHeight / 2));
        g.FillPath(trackBrush, trackPath);

        if (_isMarquee)
        {
            DrawWin11Marquee(g, trackRect);
        }
        else
        {
            DrawWin11Progress(g, trackRect);
        }

        // Draw percentage text (Windows 11 style positioning)
        if (_showPercentage && !_isMarquee)
        {
            DrawWin11Text(g);
        }
    }
    private void DrawWin11Progress(Graphics g, Rectangle trackRect)
    {
        var progressWidth = _maximum > _minimum ?
            (float)trackRect.Width * (_value - _minimum) / (_maximum - _minimum) : 0;

        if (!(progressWidth > 0)) return;
        var progressRect = trackRect with { Width = (int)progressWidth };

        if (progressRect.Width <= 0) return;
        // Windows 11 style: solid color, clean and simple
        using var progressBrush = new SolidBrush(_progressColor);
        var progressPath = GetRoundedRectPath(progressRect, Math.Min(_borderRadius, trackRect.Height / 2));
        g.FillPath(progressBrush, progressPath);

        // Add very subtle end cap highlight (like scrollbar thumb)
        if (progressRect.Width <= 6 || trackRect.Height <= 6) return;
        var capSize = Math.Min(3, trackRect.Height / 2);
        var capRect = new Rectangle(
            progressRect.Right - capSize,
            progressRect.Y + (progressRect.Height - capSize) / 2,
            capSize, capSize);

        using var capBrush = new SolidBrush(
            Color.FromArgb(60, Color.White));
        g.FillEllipse(capBrush, capRect);
    }

    private void DrawWin11Marquee(Graphics g, Rectangle trackRect)
    {
        var marqueeRect = trackRect with { X = (int)_marqueePosition, Width = _marqueeWidth };

        // Clip to track bounds
        var clipPath = GetRoundedRectPath(trackRect, _borderRadius);
        g.SetClip(clipPath);

        if (marqueeRect.Width > 0)
        {
            // Windows 11 style marquee: smooth fade in/out
            using var marqueeBrush = new LinearGradientBrush(
                marqueeRect,
                Color.FromArgb(0, _progressColor),
                Color.FromArgb(0, _progressColor),
                LinearGradientMode.Horizontal);

            var blend = new ColorBlend(3)
            {
                Colors =
                [
                    Color.FromArgb(0, _progressColor),
                    _progressColor,
                    Color.FromArgb(0, _progressColor)
                ],
                Positions = [0.0f, 0.5f, 1.0f]
            };
            marqueeBrush.InterpolationColors = blend;

            var marqueePath = GetRoundedRectPath(marqueeRect, _borderRadius);
            g.FillPath(marqueeBrush, marqueePath);
        }

        g.ResetClip();
    }
    private void DrawWin11Text(Graphics g)
    {
        var percentage = _maximum > _minimum ?
            (int)Math.Round(100.0 * (_value - _minimum) / (_maximum - _minimum)) : 0;
        var text = $"{percentage}%";

        using var textBrush = new SolidBrush(_textColor);
        var textSize = g.MeasureString(text, _textFont);

        // Position text at the center right, like Windows 11 clean style
        var textX = Width - textSize.Width - 8;
        var textY = (Height - textSize.Height) / 2;

        // Clean text without shadow for Win11 style
        g.DrawString(text, _textFont, textBrush, textX, textY);
    }

    private void DrawLegacyStyle(Graphics g)
    {
        var rect = new Rectangle(0, 0, Width, Height);

        // Draw shadow effect
        if (_useGlow)
        {
            DrawShadow(g, rect);
        }

        // Draw background with rounded corners
        using var backgroundBrush = new SolidBrush(_backgroundColor);
        var backgroundPath = GetRoundedRectPath(rect, _borderRadius);
        g.FillPath(backgroundBrush, backgroundPath);

        // Draw border

        if (_isMarquee)
        {
            // Draw marquee animation
            DrawMarquee(g);
        }
        else
        {
            // Draw normal progress
            DrawProgress(g);
        }

        // Draw percentage text
        if (!_showPercentage || _isMarquee) return;
        var percentage = _maximum > _minimum ?
            (int)Math.Round(100.0 * (_value - _minimum) / (_maximum - _minimum)) : 0;
        var text = $"{percentage}%";

        using var textBrush = new SolidBrush(_textColor);
        var textSize = g.MeasureString(text, _textFont);
        var textX = (Width - textSize.Width) / 2;
        var textY = (Height - textSize.Height) / 2;

        // Add text shadow for better visibility
        using var shadowBrush = new SolidBrush(Color.FromArgb(30, Color.Black));
        g.DrawString(text, _textFont, shadowBrush, textX + 1, textY + 1);
        g.DrawString(text, _textFont, textBrush, textX, textY);
    }
    private void DrawProgress(Graphics g)
    {
        var progressWidth = _maximum > _minimum ?
            (float)(Width - 2 * _borderWidth) * (_value - _minimum) / (_maximum - _minimum) : 0;

        if (!(progressWidth > 0)) return;
        var progressRect = new Rectangle(_borderWidth, _borderWidth,
            (int)progressWidth, Height - 2 * _borderWidth);

        if (progressRect is not { Width: > 0, Height: > 0 }) return;
        // Draw glow effect first
        if (_useGlow && progressRect.Width > 4)
        {
            var glowRect = new Rectangle(progressRect.X - 1, progressRect.Y - 1,
                progressRect.Width + 2, progressRect.Height + 2);
            var glowPath = GetRoundedRectPath(glowRect, _borderRadius);
            using var glowBrush = new SolidBrush(_glowColor);
            g.FillPath(glowBrush, glowPath);
        }

        var gradientRect = progressRect with { Width = Math.Max(progressRect.Width, 1), Height = Math.Max(progressRect.Height, 1) };

        // Create a more sophisticated gradient
        using var progressBrush = new LinearGradientBrush(gradientRect,
            _progressColor,
            _progressSecondaryColor,
            LinearGradientMode.Vertical);

        // Add color blend for better appearance
        var blend = new ColorBlend(3)
        {
            Colors =
            [
                _progressColor,
                Color.FromArgb(255,
                    Math.Min(255, (_progressColor.R + _progressSecondaryColor.R) / 2),
                    Math.Min(255, (_progressColor.G + _progressSecondaryColor.G) / 2),
                    Math.Min(255, (_progressColor.B + _progressSecondaryColor.B) / 2)),
                _progressSecondaryColor
            ],
            Positions = [0.0f, 0.5f, 1.0f]
        };
        progressBrush.InterpolationColors = blend;

        var progressPath = GetRoundedRectPath(progressRect, _borderRadius - 1);
        g.FillPath(progressBrush, progressPath);

        // Add highlight effect on top
        if (progressRect.Height <= 6) return;
        var highlightRect = progressRect with { Height = progressRect.Height / 3 };
        var highlightPath = GetRoundedRectPath(highlightRect, _borderRadius - 1);

        using var highlightBrush = new LinearGradientBrush(highlightRect,
            Color.FromArgb(60, Color.White),
            Color.FromArgb(10, Color.White),
            LinearGradientMode.Vertical);
        g.FillPath(highlightBrush, highlightPath);
    }
    private void DrawMarquee(Graphics g)
    {
        var marqueeRect = new Rectangle((int)_marqueePosition, _borderWidth,
            _marqueeWidth, Height - 2 * _borderWidth);

        // Clip to the progress bar bounds
        var clipRect = new Rectangle(_borderWidth, _borderWidth,
            Width - 2 * _borderWidth, Height - 2 * _borderWidth);
        var clipPath = GetRoundedRectPath(clipRect, _borderRadius - 1);
        g.SetClip(clipPath);

        if (marqueeRect is { Width: > 0, Height: > 0 })
        {
            using var marqueeBrush = new LinearGradientBrush(
                new(marqueeRect.X, marqueeRect.Y, marqueeRect.Width, marqueeRect.Height),
                Color.FromArgb(0, _progressColor),
                _progressColor,
                LinearGradientMode.Horizontal);

            // Create a more sophisticated blend for marquee
            var blend = new ColorBlend(5)
            {
                Colors =
                [
                    Color.FromArgb(0, _progressColor),
                    Color.FromArgb(128, _progressColor),
                    _progressColor,
                    Color.FromArgb(128, _progressSecondaryColor),
                    Color.FromArgb(0, _progressSecondaryColor)
                ],
                Positions = [0.0f, 0.25f, 0.5f, 0.75f, 1.0f]
            };
            marqueeBrush.InterpolationColors = blend;

            var marqueePath = GetRoundedRectPath(marqueeRect, _borderRadius - 1);
            g.FillPath(marqueeBrush, marqueePath);
        }

        g.ResetClip();
    }

    private void DrawShadow(Graphics g, Rectangle rect)
    {
        if (!_useGlow) return;

        // Create a subtle shadow/glow effect
        var shadowRect = rect with { X = rect.X + 1, Y = rect.Y + 1 };
        var shadowPath = GetRoundedRectPath(shadowRect, _borderRadius);

        using var shadowBrush = new SolidBrush(Color.FromArgb(20, Color.Black));
        g.FillPath(shadowBrush, shadowPath);
    }

    private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        var diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }    /// <summary>
         /// Releases resources used by the progress bar
         /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopMarquee();
            _animationTimer?.Stop();
            _animationTimer?.Dispose();
            _animationTimer = null;
            _hoverTimer?.Stop();
            _hoverTimer?.Dispose();
            _hoverTimer = null;
            _textFont.Dispose();
        }
        base.Dispose(disposing);
    }    /// <summary>
         /// Handles mouse enter event for Windows 11 style hover effects
         /// </summary>
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (_useWin11Style)
        {
            StartHoverAnimation(true);
        }
    }

    /// <summary>
    /// Handles mouse leave event for Windows 11 style hover effects
    /// </summary>
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_useWin11Style)
        {
            StartHoverAnimation(false);
        }
    }

    private void StartHoverAnimation(bool fadeIn)
    {
        _hoverTimer?.Stop();
        _hoverTimer?.Dispose();

        _hoverTimer = new() { Interval = 16 }; // 60 FPS
        _hoverTimer.Tick += (_, _) =>
        {
            if (fadeIn)
            {
                _hoverOpacity = Math.Min(1f, _hoverOpacity + 0.1f);
                if (_hoverOpacity >= 1f)
                {
                    _hoverTimer.Stop();
                    _hoverTimer.Dispose();
                    _hoverTimer = null;
                }
            }
            else
            {
                _hoverOpacity = Math.Max(0f, _hoverOpacity - 0.1f);
                if (_hoverOpacity <= 0f)
                {
                    _hoverTimer.Stop();
                    _hoverTimer.Dispose();
                    _hoverTimer = null;
                }
            }
            Invalidate();
        };
        _hoverTimer.Start();
    }
}
