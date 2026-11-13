using System.Windows.Controls;
using System.Windows.Media;
using Paraninfo.App.Models;

namespace Paraninfo.App.Views;

public partial class CelebrationView : UserControl
{
    private bool? _pendingOutcome;
    public event Action<bool>? OutcomePresented;
    public CelebrationView() => InitializeComponent();

    public void Configure(InvitationConfig config, bool accepted, Func<string, string> expand, bool playSound = false)
    {
        _pendingOutcome = playSound ? accepted : null;
        Status.Text = accepted ? "[OK] OPERATION COMPLETED / STATUS: SUCCESS" : "[INFO] SESSÃO CONCLUÍDA";
        TitleLabel.Text = expand(accepted ? config.Mensagens.Success : config.Mensagens.Declined);
        ThanksLabel.Text = expand(config.Mensagens.Thanks);
        Credits.Text = $"Developers: {config.Turma}\nProject: Graduação {config.Ano}\n"
            + $"{(accepted ? "Paraninfo" : "Professor")}: {config.Professor}\n{config.Instituicao}";
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (_pendingOutcome is not { } accepted) return;
        _pendingOutcome = null;
        OutcomePresented?.Invoke(accepted);
    }
}
