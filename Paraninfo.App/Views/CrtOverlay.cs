using System.Windows;
using System.Windows.Media;
using Paraninfo.App.Models;

namespace Paraninfo.App.Views;

// Camada CRT estática: não acrescenta flicker, temporizadores ou distorção dos textos.
public sealed class CrtOverlay : FrameworkElement
{
    private bool _enabled;
    private Pen _scanline = new(Brushes.Transparent, 1);
    private readonly RadialGradientBrush _vignette = new()
    {
        Center = new Point(0.5, 0.47), GradientOrigin = new Point(0.5, 0.47),
        RadiusX = 0.72, RadiusY = 0.8,
        GradientStops = new GradientStopCollection
        {
            new(Colors.Transparent, 0.35), new(Color.FromArgb(70, 0, 0, 0), 1)
        }
    };

    public CrtOverlay()
    {
        IsHitTestVisible = false;
        _vignette.Freeze();
    }

    public void Configure(VisualConfig config)
    {
        _enabled = config.CrtEnabled;
        _scanline = new Pen(new SolidColorBrush(Color.FromArgb((byte)(255 * config.CrtScanlineOpacity), 0, 0, 0)), 1);
        _scanline.Freeze();
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (!_enabled || ActualWidth <= 0 || ActualHeight <= 0) return;
        var dpi = VisualTreeHelper.GetDpi(this);
        var pixel = 1 / dpi.DpiScaleY;
        for (var y = pixel / 2; y < ActualHeight; y += pixel * 4)
            dc.DrawLine(_scanline, new Point(0, y), new Point(ActualWidth, y));
        dc.DrawRectangle(_vignette, null, new Rect(RenderSize));
    }
}
