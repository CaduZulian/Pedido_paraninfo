using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Paraninfo.App.Models;
using Paraninfo.App.Services;

namespace Paraninfo.App;

public partial class MainWindow : Window
{
    private readonly InvitationConfig _config;
    private readonly SequenceController _sequence;
    private readonly AudioService _audio;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _closed;
    private bool _terminalAudible;
    private bool _isRunning;
    private TranslateTransform TerminalShift => (TranslateTransform)Terminal.RenderTransform;

    public MainWindow()
    {
        InitializeComponent();
        _config = new ConfigurationService().Load();
        try
        {
            Application.Current.Resources["TerminalFontFamily"] = _config.Visual.TerminalFont.Equals("VT323", StringComparison.OrdinalIgnoreCase)
                ? new FontFamily(new Uri("pack://application:,,,/"), "./Assets/Fonts/#VT323")
                : new FontFamily(_config.Visual.TerminalFont);
        }
        catch (ArgumentException) { /* Mantém a família padrão em caso de nome inválido. */ }
        _audio = new AudioService(_config.Audio);
        _sequence = new SequenceController(_config, WriteTerminal);
        _sequence.StateChanged += OnStateChanged;
        _sequence.ProgressChanged += OnProgress;
        Terminal.TextRevealed += () => { if (_terminalAudible && !_closed) _audio.Play("type"); };
        Effects.PopupAppeared += () => { if (!_closed) _audio.Play("alert"); };
        Effects.CriticalAppeared += () => { if (!_closed) _audio.Play("collapse"); };
        Celebration.OutcomePresented += accepted => { if (!_closed) _audio.Play(accepted ? "success" : "failure"); };
        Invitation.Configure(_config, _sequence.Expand);
        Terminal.Configure(_config);
        FinalCrt.Configure(_config.Visual);
        FinalPhosphor.Effect = _config.Visual.CrtEnabled && _config.Visual.CrtGlow > 0
            ? new DropShadowEffect { Color = Color.FromRgb(81, 242, 148), ShadowDepth = 0,
                BlurRadius = 4, Opacity = _config.Visual.CrtGlow, RenderingBias = RenderingBias.Performance }
            : null;
        Invitation.Answered += answer => _sequence.Respond(answer);
        Effects.Configure(_config, _sequence.Expand);
        var args = Environment.GetCommandLineArgs();
        var fullscreen = _config.TelaCheia;
#if DEBUG
        fullscreen = false;
#endif
        if (args.Contains("--presentation")) fullscreen = true;
        if (args.Contains("--windowed")) fullscreen = false;
        if (fullscreen)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }
        // Uma ativação inicial; não recaptura foco nem bloqueia Alt+Tab ou a barra do Windows.
        Loaded += Window_Loaded;
        UpdateKeys();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Window_Loaded;
        Activate();
        if (Environment.GetCommandLineArgs().Contains("--skip"))
            _sequence.SkipToInvitation();
        _isRunning = true;
        try { await _sequence.RunAsync(_lifetime.Token); }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception)
        {
            if (!_closed)
            {
                _audio.Stop();
                Effects.Update(PresentationState.Finished, 0);
                FinalLayout.Visibility = Visibility.Collapsed;
                Blackout.Opacity = 0;
                Invitation.Visibility = Celebration.Visibility = Visibility.Collapsed;
                Terminal.Visibility = Visibility.Visible;
                Terminal.Opacity = 1;
                Terminal.Write("A apresentação foi interrompida.\nPressione F2 para abrir o convite ou ESC para sair.");
                PhaseLabel.Text = "● RECUPERAÇÃO MANUAL";
                _recovery = true;
            }
        }
        finally
        {
            _isRunning = false;
            if (_closed) _lifetime.Dispose();
        }
    }

    private bool _recovery;

    private void WriteTerminal(string text)
    {
        if (_closed) return;
        Terminal.Write(text);
    }

    private void OnStateChanged(PresentationState state)
    {
        if (_closed) return;
        _audio.Stop();
        Terminal.ResetText();
        TerminalShift.X = TerminalShift.Y = 0;
        Terminal.Opacity = 1;
        Effects.Update(state, 0);
        var terminalStage = state is PresentationState.BootSequence or PresentationState.SystemAnomaly
            or PresentationState.SystemCollapse;
        _terminalAudible = terminalStage;
        FinalLayout.Visibility = terminalStage ? Visibility.Collapsed : Visibility.Visible;
        Blackout.Opacity = 0;
        Terminal.Visibility = state is PresentationState.BootSequence or PresentationState.SystemAnomaly
            or PresentationState.SystemCollapse ? Visibility.Visible : Visibility.Collapsed;
        Invitation.Visibility = state == PresentationState.Invitation ? Visibility.Visible : Visibility.Collapsed;
        Celebration.Visibility = state is PresentationState.Celebration or PresentationState.Finished
            ? Visibility.Visible : Visibility.Collapsed;
        PhaseLabel.Text = state switch
        {
            PresentationState.BootSequence => "SYSTEM ONLINE",
            PresentationState.SystemAnomaly => "! ANOMALY DETECTED",
            PresentationState.SystemCollapse => "! CRITICAL REQUIREMENT",
            PresentationState.Invitation => "AWAITING INPUT",
            PresentationState.Celebration => "DEPENDENCY RESOLVED",
            PresentationState.Finished => "SESSION COMPLETED",
            _ => "INITIALIZING"
        };
        Terminal.SetColor((Brush)FindResource(state is PresentationState.SystemAnomaly
            or PresentationState.SystemCollapse ? "WarningAmberBrush" : "TerminalGreenBrush"));
        switch (state)
        {
            case PresentationState.Invitation:
                _audio.Stop();
                Reveal(Invitation);
                Invitation.FocusAnswer();
                break;
            case PresentationState.Celebration:
                Celebration.Configure(_config, true, _sequence.Expand, playSound: true);
                Reveal(Celebration);
                break;
            case PresentationState.Finished:
                _audio.Stop();
                Celebration.Configure(_config, _sequence.Accepted, _sequence.Expand,
                    playSound: _sequence.HasResponded && !_sequence.Accepted);
                break;
        }
    }

    private void OnProgress(double progress)
    {
        if (_closed) return;
        Effects.Update(_sequence.State, progress);
        if (_sequence.State == PresentationState.SystemCollapse)
        {
            Blackout.Opacity = progress >= 0.94 ? 1 : 0;
            if (progress >= 0.7 && _terminalAudible)
            {
                _terminalAudible = false;
                _audio.Stop();
            }
        }
        if (_sequence.State is PresentationState.SystemAnomaly or PresentationState.SystemCollapse)
        {
            if (!_config.Visual.ReducedMotion)
            {
                TerminalShift.X = Effects.Displacement.X;
                TerminalShift.Y = Effects.Displacement.Y;
                Terminal.Opacity = Effects.SignalOpacity;
            }
        }
    }

    private void Reveal(UIElement element)
    {
        element.BeginAnimation(OpacityProperty, null);
        element.Opacity = 1;
        if (!_config.Visual.ReducedMotion)
            element.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(700))
            { FillBehavior = FillBehavior.Stop });
    }

    private void UpdateKeys() => KeysLabel.Text = $"F2 CONVITE · M ÁUDIO {(_audio.Muted ? "OFF" : "ON")} · ESC SAIR";

    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        _lifetime.Cancel();
        _sequence.StateChanged -= OnStateChanged;
        _sequence.ProgressChanged -= OnProgress;
        _audio.Dispose();
        if (!_isRunning) _lifetime.Dispose();
        base.OnClosed(e);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { e.Handled = true; Close(); }
        else if (e.Key == Key.M) { e.Handled = true; _audio.ToggleMute(); UpdateKeys(); }
        else if (e.Key == Key.F2)
        {
            e.Handled = true;
            if (_recovery)
            {
                FinalLayout.Visibility = Visibility.Visible;
                Terminal.Visibility = Celebration.Visibility = Visibility.Collapsed;
                Invitation.Visibility = Visibility.Visible;
                // Na contingência, o controlador pode já estar finalizado.
                Invitation.Answered += answer =>
                {
                    Celebration.Configure(_config, answer, _sequence.Expand, playSound: true);
                    Invitation.Visibility = Visibility.Collapsed;
                    Celebration.Visibility = Visibility.Visible;
                };
                Invitation.FocusAnswer();
                _recovery = false;
            }
            else _sequence.SkipToInvitation();
        }
    }
}
