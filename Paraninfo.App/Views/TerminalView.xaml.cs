using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Paraninfo.App.Models;
using Paraninfo.App.Services;

namespace Paraninfo.App.Views;

public partial class TerminalView : UserControl
{
    private readonly TextRevealTracker _typing = new();
    public event Action? TextRevealed;
    public TerminalView() => InitializeComponent();

    public void Configure(InvitationConfig config)
    {
        var year = config.Ano;
        ShellVersion.Text = $"Paraninfo Academic Shell [versão {year}.1]";
        Prompt.Text = $"PS C:\\Turma\\{year}> .\\Paraninfo.exe";
        Crt.Configure(config.Visual);
        PhosphorText.Effect = config.Visual.CrtEnabled && config.Visual.CrtGlow > 0
            ? new DropShadowEffect { Color = Color.FromRgb(81, 242, 148), ShadowDepth = 0,
                BlurRadius = 4, Opacity = config.Visual.CrtGlow, RenderingBias = RenderingBias.Performance }
            : null;
    }

    public void Write(string text)
    {
        Output.Text = text;
        Scroller.ScrollToEnd();
        InvalidateVisual();
    }

    public void ResetText()
    {
        _typing.Reset();
        Write("");
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (_typing.Present(Output.Text)) TextRevealed?.Invoke();
    }

    public void SetColor(Brush color)
    {
        Output.Foreground = color;
    }

}
