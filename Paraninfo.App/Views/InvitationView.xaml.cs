using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Paraninfo.App.Models;

namespace Paraninfo.App.Views;

public partial class InvitationView : UserControl
{
    private int _attempts;
    private int _limit;
    private bool _responded;
    private string[] _hints = [];
    public event Action<bool>? Answered;

    public InvitationView() => InitializeComponent();

    public void Configure(InvitationConfig config, Func<string, string> expand)
    {
        Professor.Text = $"Professor {config.Professor}";
        Question.Text = expand(config.Mensagens.Invitation);
        ClassLabel.Text = $"{config.Turma} · {config.Instituicao} · {config.Ano}";
        _limit = config.Visual.ReducedMotion ? 0 : config.TentativasNao;
        _hints = config.Mensagens.NoHints.Select(expand).ToArray();
        Hint.Text = expand(config.Mensagens.InvitationHint);
    }

    public void FocusAnswer() => Yes.Focus();

    private void Area_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var groupLeft = Math.Max(0, (ButtonArea.ActualWidth - Yes.Width - 32 - No.Width) / 2);
        Canvas.SetLeft(Yes, groupLeft);
        Canvas.SetTop(Yes, 50);
        if (_attempts == 0)
        {
            Canvas.SetLeft(No, groupLeft + Yes.Width + 32);
            Canvas.SetTop(No, 50);
        }
        ClampNo();
    }

    private void ClampNo()
    {
        Canvas.SetLeft(No, Math.Clamp(Canvas.GetLeft(No), 0, Math.Max(0, ButtonArea.ActualWidth - No.Width)));
        Canvas.SetTop(No, Math.Clamp(Canvas.GetTop(No), 0, Math.Max(0, ButtonArea.ActualHeight - No.Height)));
    }

    private void No_MouseEnter(object sender, MouseEventArgs e)
    {
        // Foco de teclado nunca foge: Tab/Enter permite recusar desde a primeira tentativa.
        if (_responded || _attempts >= _limit || No.IsKeyboardFocused)
            return;
        var pointer = e.GetPosition(ButtonArea);
        var yesBounds = new Rect(Canvas.GetLeft(Yes) - 12, Canvas.GetTop(Yes) - 12, Yes.Width + 24, Yes.Height + 24);
        Point? target = null;
        // Sorteia posições contínuas; rejeita o cursor, o SIM e saltos curtos.
        // Não escolhe sempre o canto mais distante nem repete uma rota fixa.
        var previous = new Point(Canvas.GetLeft(No), Canvas.GetTop(No));
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var point = new Point(
                Random.Shared.NextDouble() * Math.Max(0, ButtonArea.ActualWidth - No.Width),
                Random.Shared.NextDouble() * Math.Max(0, ButtonArea.ActualHeight - No.Height));
            var bounds = new Rect(point, new Size(No.Width, No.Height));
            var pointerClearance = bounds;
            pointerClearance.Inflate(80, 50);
            if (bounds.IntersectsWith(yesBounds) || pointerClearance.Contains(pointer)
                || (point - previous).Length < No.Width + 40)
                continue;
            target = point;
            break;
        }
        if (target is not { } destination) return;
        Canvas.SetLeft(No, destination.X);
        Canvas.SetTop(No, destination.Y);
        ClampNo();
        _attempts++;
        Hint.Text = _hints[(_attempts - 1) % _hints.Length];
    }

    private void Yes_Click(object sender, RoutedEventArgs e) => Respond(true);
    private void No_Click(object sender, RoutedEventArgs e) => Respond(false);

    private void Respond(bool answer)
    {
        if (_responded)
            return;
        _responded = true;
        Yes.IsEnabled = No.IsEnabled = false;
        Answered?.Invoke(answer);
    }
}
