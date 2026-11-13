using System.Diagnostics;
using System.Text;
using Paraninfo.App.Models;

namespace Paraninfo.App.Services;

// Um único relógio por etapa coordena texto, pausas e efeitos. Chamado pela thread WPF.
public sealed class SequenceController
{
    private readonly InvitationConfig _config;
    private readonly Action<string> _writeText;
    private readonly TaskCompletionSource<bool> _response = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenSource? _introCancellation;
    private bool _started;
    private bool _skipRequested;

    public PresentationState State { get; private set; } = PresentationState.Idle;
    public bool Accepted { get; private set; }
    public bool HasResponded { get; private set; }
    public event Action<PresentationState>? StateChanged;
    public event Action<double>? ProgressChanged;

    public SequenceController(InvitationConfig config, Action<string> writeText)
    {
        _config = config;
        _writeText = writeText;
    }

    public void SkipToInvitation()
    {
        if (State is PresentationState.Idle or PresentationState.BootSequence
            or PresentationState.SystemAnomaly or PresentationState.SystemCollapse)
        {
            _skipRequested = true;
            _introCancellation?.Cancel();
        }
    }

    public bool Respond(bool accepted) => State == PresentationState.Invitation && _response.TrySetResult(accepted);

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_started)
            throw new InvalidOperationException("A apresentação já foi iniciada.");
        _started = true;
        try
        {
            using (var intro = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                _introCancellation = intro;
                try
                {
                    if (_skipRequested)
                        intro.Cancel();
                    await RunStageAsync(PresentationState.BootSequence, _config.Duracao.Boot,
                        _config.Mensagens.BootLines, intro.Token);
                    await RunStageAsync(PresentationState.SystemAnomaly, _config.Duracao.Anomaly,
                        _config.Mensagens.AnomalyLines, intro.Token);
                    await RunStageAsync(PresentationState.SystemCollapse, _config.Duracao.Collapse,
                        _config.Mensagens.CollapseLines, intro.Token);
                }
                catch (OperationCanceledException) when (_skipRequested && !cancellationToken.IsCancellationRequested)
                {
                    // F2 cancela somente a introdução, preservando a espera pelo professor.
                }
                finally { _introCancellation = null; }
            }
            cancellationToken.ThrowIfCancellationRequested();
            SetState(PresentationState.Invitation);
            Accepted = await _response.Task.WaitAsync(cancellationToken);
            HasResponded = true;
            cancellationToken.ThrowIfCancellationRequested();
            if (Accepted)
                await RunStageAsync(PresentationState.Celebration, _config.Duracao.Celebration,
                    [], cancellationToken);
        }
        finally { SetState(PresentationState.Finished); }
    }

    public string Expand(string text) => text
        .Replace("{boot}", _config.Mensagens.Boot).Replace("{anomaly}", _config.Mensagens.Anomaly)
        .Replace("{professor}", _config.Professor).Replace("{turma}", _config.Turma)
        .Replace("{instituicao}", _config.Instituicao).Replace("{disciplina}", _config.Disciplina)
        .Replace("{ano}", _config.Ano.ToString());

    private async Task RunStageAsync(PresentationState state, int duration,
        string[] script, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lines = script.Select(Expand).ToArray();
        var elapsed = Stopwatch.StartNew();
        SetState(state);
        string? previous = null;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var progress = Math.Clamp(elapsed.Elapsed.TotalMilliseconds / duration, 0, 1);
            if (lines.Length > 0)
            {
                // Digitação ocupa 68% de cada intervalo; o restante é uma pausa natural.
                var position = progress * lines.Length;
                var current = Math.Min((int)position, lines.Length - 1);
                var text = new StringBuilder();
                for (var i = 0; i < current; i++)
                    text.AppendLine(lines[i]);
                var count = progress >= 1 ? lines[current].Length
                    : (int)(lines[current].Length * Math.Min(1, (position - current) / 0.68));
                // Não divide pares UTF-16 ao revelar nomes e mensagens personalizados.
                if (count > 0 && count < lines[current].Length && char.IsHighSurrogate(lines[current][count - 1]))
                    count--;
                text.Append(lines[current].AsSpan(0, count));
                var snapshot = text.ToString();
                if (snapshot != previous)
                    _writeText(previous = snapshot);
            }
            ProgressChanged?.Invoke(progress);
            if (progress >= 1)
                break;
            var remaining = duration - elapsed.Elapsed.TotalMilliseconds;
            if (remaining > 0)
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(33, remaining)), cancellationToken);
        } while (true);
    }

    private void SetState(PresentationState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }
}
