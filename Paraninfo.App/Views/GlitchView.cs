using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Paraninfo.App.Models;

namespace Paraninfo.App.Views;

// Desenho coordenado pelo progresso do roteiro, sem timers ou janelas externas.
public sealed class GlitchView : FrameworkElement
{
    private PresentationState _state;
    private double _progress;
    private VisualConfig _config = new();
    private Typeface _font = new("Consolas");
    private string[] _alerts = [];
    private string _collapseTitle = "";
    private string _collapseMessage = "";
    private string _professor = "";
    private Brush _particleBrush = Green;
    private double _anomalySeconds;
    private double _collapseSeconds;
    private double _seconds;
    private Burst? _activeBurst;
    private readonly List<Burst> _bursts = [];
    private readonly List<Alert> _windows = [];
    private sealed record Burst(int Id, double Start, double End, int Seed, double Strength);
    private sealed record Alert(double Start, double End, Rect Bounds, string Message, bool Primary);
    public Vector Displacement { get; private set; }
    public double SignalOpacity { get; private set; } = 1;
    private readonly HashSet<Alert> _announcedAlerts = [];
    private bool _criticalAnnounced;
    public event Action? PopupAppeared;
    public event Action? CriticalAppeared;
    private readonly List<(double X, double Speed, double Drift, double Size, double Spin)> _particles = [];
    private static readonly Brush Green = Frozen("#70FFAA");
    private static readonly Brush Amber = Frozen("#F2C572");
    private static readonly Brush Panel = Frozen("#F2101C16");
    private static readonly Brush Glitch = Frozen("#6951F294");
    private static readonly Brush Occlusion = Frozen("#E8090F0C");
    private static readonly Brush Shadow = Frozen("#90000000");
    private static readonly Brush AlertBar = Frozen("#283D2C");

    public GlitchView() => IsHitTestVisible = false;

    public void Configure(InvitationConfig invitation, Func<string, string> expand)
    {
        var config = invitation.Visual;
        _config = config;
        _font = new Typeface((FontFamily)Application.Current.Resources["TerminalFontFamily"],
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        _anomalySeconds = invitation.Duracao.Anomaly / 1000.0;
        _collapseSeconds = invitation.Duracao.Collapse / 1000.0;
        _alerts = invitation.Mensagens.AlertLines.Select(expand).ToArray();
        _collapseTitle = expand(invitation.Mensagens.CollapseTitle);
        _collapseMessage = expand(invitation.Mensagens.CollapseMessage);
        _professor = $"Professor {invitation.Professor}";
        _particleBrush = Frozen(config.ParticleColor);
        var random = Random.Shared;
        _particles.Clear();
        for (var i = 0; i < config.ParticleCount; i++)
            _particles.Add((random.NextDouble(), 0.6 + random.NextDouble(),
                random.NextDouble() - 0.5, 3 + random.NextDouble() * 7, random.NextDouble() * 360));
    }

    public void Update(PresentationState state, double progress)
    {
        if (state != _state)
        {
            _bursts.Clear();
            _windows.Clear();
            _announcedAlerts.Clear();
            _criticalAnnounced = false;
            if (state is PresentationState.SystemAnomaly or PresentationState.SystemCollapse)
                BuildFailureScene(state);
        }
        _state = state;
        _progress = progress;
        _seconds = progress * (state == PresentationState.SystemCollapse ? _collapseSeconds : _anomalySeconds);
        _activeBurst = _bursts.FirstOrDefault(b => _seconds >= b.Start && _seconds < b.End);
        Displacement = default;
        SignalOpacity = 1;
        if (_activeBurst is { } burst)
        {
            if (!_config.ReducedMotion)
            {
                var random = new Random(burst.Seed ^ (int)(_seconds * 12));
                Displacement = new Vector(random.Next(-36, 37) * burst.Strength,
                    random.Next(-10, 11) * burst.Strength);
                SignalOpacity = 0.65 + random.NextDouble() * 0.25;
            }
        }
        InvalidateVisual();
    }

    private void BuildFailureScene(PresentationState state)
    {
        var random = Random.Shared;
        var collapse = state == PresentationState.SystemCollapse;
        var duration = collapse ? _collapseSeconds : _anomalySeconds;
        var end = collapse ? duration * 0.7 : duration;
        // Um impacto imediato, seguido de rajadas curtas com silêncios de duração variável.
        var time = 0.0;
        while (time < end)
        {
            var length = _bursts.Count == 0 ? 0.38 : 0.09 + random.NextDouble() * 0.28;
            _bursts.Add(new Burst(_bursts.Count, time, time + length, random.Next(),
                _bursts.Count == 0 ? 1.2 : 0.6 + random.NextDouble() * (collapse ? 1.2 : 0.8)));
            time += length + 0.35 + random.NextDouble() * (collapse ? 0.8 : 1.4);
        }
        // O primeiro erro corta a tela imediatamente; os demais surgem em posições e instantes novos.
        _windows.Add(new Alert(0, Math.Min(end, 2.1),
            new Rect(random.Next(30, 400), random.Next(45, 280), 760, 178), _alerts[0], true));
        time = 0.5 + random.NextDouble() * 0.6;
        var previous = _windows[0].Bounds.Location;
        while (time < end)
        {
            var width = random.Next(400, 601);
            var height = random.Next(126, 151);
            Point position = default;
            for (var attempt = 0; attempt < 12; attempt++)
            {
                position = new Point(random.Next(12, 1188 - width), random.Next(12, 558 - height));
                if ((position - previous).Length > 230) break;
            }
            _windows.Add(new Alert(time, time + 0.9 + random.NextDouble() * 2.0,
                new Rect(position, new Size(width, height)), _alerts[random.Next(_alerts.Length)], false));
            previous = position;
            time += 0.4 + random.NextDouble() * 1.15;
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0)
            return;
        dc.PushClip(new RectangleGeometry(new Rect(RenderSize)));
        dc.PushTransform(new ScaleTransform(ActualWidth / 1200, ActualHeight / 570));
        if (_state == PresentationState.Celebration && !_config.ReducedMotion)
            DrawParticles(dc);
        if (_state is PresentationState.SystemAnomaly or PresentationState.SystemCollapse)
            DrawFailure(dc);
        dc.Pop();
        dc.Pop();
    }

    private void DrawFailure(DrawingContext dc)
    {
        var collapse = _state == PresentationState.SystemCollapse;
        if (collapse && _progress >= 0.7)
        {
            // Corte seco: o terminal e os erros desaparecem no mesmo frame em que o título surge.
            dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 1200, 570));
            if (_progress < 0.94)
            {
                CenterText(dc, _collapseTitle, 190, Amber, 27);
                CenterText(dc, _collapseMessage, 245, Green, 22);
                CenterText(dc, _professor, 310, Green, 27);
                if (!_criticalAnnounced)
                {
                    _criticalAnnounced = true;
                    CriticalAppeared?.Invoke();
                }
            }
            return;
        }
        if (!_config.ReducedMotion && _activeBurst is { } burst)
        {
            var random = new Random(burst.Seed ^ (int)(_seconds * 12));
            // Faixas largas, oclusões e ecos do erro, mantendo a paleta verde e sem flashes brancos.
            for (var i = 0; i < 13; i++)
            {
                var y = random.Next(0, 550);
                dc.DrawRectangle(i % 3 == 0 ? Occlusion : Glitch, null,
                    new Rect(random.Next(-80, 400), y, random.Next(250, 1150), random.Next(4, 28)));
            }
            dc.PushOpacity(0.6);
            for (var i = 0; i < 4; i++)
            {
                Text(dc, _alerts[random.Next(_alerts.Length)], random.Next(-70, 450), random.Next(0, 530),
                    i % 2 == 0 ? Green : Amber, random.Next(20, 39));
            }
            dc.Pop();
        }
        foreach (var alert in _windows.Where(window => _seconds >= window.Start && _seconds < window.End))
        {
            var bounds = alert.Bounds;
            var x = bounds.X;
            var y = bounds.Y;
            dc.DrawRectangle(Shadow, null, new Rect(x + 8, y + 10, bounds.Width, bounds.Height));
            dc.DrawRoundedRectangle(Panel, new Pen(Amber, alert.Primary ? 3 : 1), bounds, 4, 4);
            dc.DrawRectangle(AlertBar, null, new Rect(x + 2, y + 2, bounds.Width - 4, 32));
            Text(dc, alert.Primary ? "!  CRITICAL DEPENDENCY FAILURE" : "!  ACADEMIC SYSTEM / INTERRUPT", x + 16, y + 8, Amber, 16);
            Text(dc, "×", bounds.Right - 28, y + 6, Amber, 20);
            var message = Format(alert.Message, Green, alert.Primary ? 31 : 21);
            message.MaxTextWidth = bounds.Width - 36;
            message.MaxTextHeight = alert.Primary ? 82 : 54;
            message.Trimming = TextTrimming.CharacterEllipsis;
            dc.DrawText(message, new Point(x + 18, y + 46));
            Text(dc, "[ local diagnostic / recovery failed ]", x + 18, bounds.Bottom - 25, Amber, 13);
            if (_announcedAlerts.Add(alert)) PopupAppeared?.Invoke();
        }
    }

    private void DrawParticles(DrawingContext dc)
    {
        dc.PushOpacity(Math.Clamp((1 - _progress) * 4, 0, 0.85));
        foreach (var p in _particles)
        {
            var x = p.X * 1200 + Math.Sin(_progress * 8 + p.Spin) * 45 + p.Drift * _progress * 160;
            var y = -80 + _progress * p.Speed * 880 * _config.ParticleSpeed;
            if (y < -20 || y > 600)
                continue;
            dc.PushTransform(new RotateTransform(p.Spin + _progress * 380, x, y));
            dc.DrawRectangle(_particleBrush, null, new Rect(x, y, p.Size, p.Size * 0.48));
            dc.Pop();
        }
        dc.Pop();
    }

    private void Text(DrawingContext dc, string value, double x, double y, Brush brush, double size) =>
        dc.DrawText(Format(value, brush, size), new Point(x, y));

    private void CenterText(DrawingContext dc, string value, double y, Brush brush, double size)
    {
        var text = Format(value, brush, size);
        text.MaxTextWidth = 1100;
        text.MaxTextHeight = 90;
        text.Trimming = TextTrimming.CharacterEllipsis;
        text.TextAlignment = TextAlignment.Center;
        dc.DrawText(text, new Point(50, y));
    }

    private FormattedText Format(string value, Brush brush, double size) =>
        new(value, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, _font, size, brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private static Brush Frozen(string color)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
        brush.Freeze();
        return brush;
    }
}
