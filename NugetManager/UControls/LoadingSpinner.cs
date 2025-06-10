using System.Drawing.Drawing2D;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace NugetManager.UControls;

/// <summary>
/// Windows 11 style loading spinner animation
/// </summary>
public sealed class LoadingSpinner : Control
{
    private System.Windows.Forms.Timer? _animationTimer;
    private float _rotationAngle;
    private Color _spinnerColor = Color.FromArgb(0, 120, 212); // Windows 11 Blue
    private int _thickness = 3;
    private bool _isSpinning;

    /// <summary>
    /// Initializes a new instance of the LoadingSpinner class
    /// </summary>
    public LoadingSpinner()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        Size = new(32, 32);
        BackColor = Color.Transparent;
    }

    /// <summary>
    /// Gets or sets the color of the spinner
    /// </summary>
    [System.ComponentModel.DefaultValue(typeof(Color), "0, 120, 212")]
    public Color SpinnerColor
    {
        get => _spinnerColor;
        set { _spinnerColor = value; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the thickness of the spinner
    /// </summary>
    [System.ComponentModel.DefaultValue(3)]
    public int Thickness
    {
        get => _thickness;
        set { _thickness = Math.Max(1, value); Invalidate(); }
    }

    /// <summary>
    /// Gets or sets whether the spinner is currently spinning
    /// </summary>
    [System.ComponentModel.DefaultValue(false)]
    public bool IsSpinning
    {
        get => _isSpinning;
        set
        {
            if (_isSpinning == value) return;
            _isSpinning = value;

            if (_isSpinning)
            {
                StartSpinning();
            }
            else
            {
                StopSpinning();
            }
        }
    }

    /// <summary>
    /// Starts the spinning animation
    /// </summary>
    public void StartSpinning()
    {
        if (_animationTimer != null) return;

        _isSpinning = true; _animationTimer = new() { Interval = 20 }; // ~50 FPS, smoother
        _animationTimer.Tick += (_, _) =>
        {
            _rotationAngle += 3f; // Slower, more Windows 11 like
            if (_rotationAngle >= 360f)
                _rotationAngle = 0f;
            Invalidate();
        };
        _animationTimer.Start();
        Visible = true;
    }

    /// <summary>
    /// Stops the spinning animation
    /// </summary>
    public void StopSpinning()
    {
        _isSpinning = false;
        _animationTimer?.Stop();
        _animationTimer?.Dispose();
        _animationTimer = null;
        Visible = false;
    }    /// <summary>
         /// Handles the paint event
         /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        if (!_isSpinning)
        {
            base.OnPaint(e);
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

        var center = new PointF(Width / 2f, Height / 2f);
        var radius = Math.Min(Width, Height) / 2f - _thickness - 2;

        // Windows 11 style: 8 dots in a circle
        const int dotCount = 8;
        var dotSize = _thickness + 1;

        for (var i = 0; i < dotCount; i++)
        {
            var angle = (360f / dotCount * i + _rotationAngle) * Math.PI / 180;
            var dotX = center.X + (float)(radius * Math.Cos(angle)) - dotSize / 2f;
            var dotY = center.Y + (float)(radius * Math.Sin(angle)) - dotSize / 2f;

            // Calculate opacity based on position (leading dots are more opaque)
            var leadingIndex = (int)(_rotationAngle / (360f / dotCount)) % dotCount;
            var distance = Math.Min(Math.Abs(i - leadingIndex), dotCount - Math.Abs(i - leadingIndex));
            var opacity = Math.Max(30, 255 - distance * 40); // Smooth fade

            using var brush = new SolidBrush(Color.FromArgb(opacity, _spinnerColor));
            e.Graphics.FillEllipse(brush, dotX, dotY, dotSize, dotSize);
        }

        base.OnPaint(e);
    }

    /// <summary>
    /// Handles visibility changes
    /// </summary>
    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(value);
        if (!value && _isSpinning)
        {
            StopSpinning();
        }
    }

    /// <summary>
    /// Releases resources
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopSpinning();
        }
        base.Dispose(disposing);
    }
}
