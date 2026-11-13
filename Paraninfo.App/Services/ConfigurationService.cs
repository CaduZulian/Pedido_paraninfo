using System.IO;
using System.Text.Json;
using Paraninfo.App.Models;

namespace Paraninfo.App.Services;

public sealed class ConfigurationService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public InvitationConfig Load(string? path = null)
    {
        path ??= Path.Combine(AppContext.BaseDirectory, "config.json");

        if (!File.Exists(path))
            return new InvitationConfig();

        try
        {
            var json = File.ReadAllText(path);
            return Normalize(JsonSerializer.Deserialize<InvitationConfig>(json, Options)
                   ?? new InvitationConfig());
        }
        catch (JsonException)
        {
            // Não impede a execução se o arquivo estiver inválido.
            return new InvitationConfig();
        }
        catch (IOException)
        {
            return new InvitationConfig();
        }
        catch (UnauthorizedAccessException)
        {
            return new InvitationConfig();
        }
    }

    private static InvitationConfig Normalize(InvitationConfig config)
    {
        var defaults = new InvitationConfig();
        static string Text(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
        static int Duration(int value, int fallback) => value is > 0 and <= 300000 ? value : fallback;

        config.Professor = Text(config.Professor, defaults.Professor);
        config.Disciplina = Text(config.Disciplina, defaults.Disciplina);
        config.Turma = Text(config.Turma, defaults.Turma);
        config.Instituicao = Text(config.Instituicao, defaults.Instituicao);
        if (config.Ano is < 1 or > 9999)
            config.Ano = defaults.Ano;
        config.Duracao ??= new DurationConfig();
        config.Mensagens ??= new MessageConfig();
        config.Audio ??= new AudioConfig();
        config.Visual ??= new VisualConfig();
        config.Visual.TerminalFont = Text(config.Visual.TerminalFont, defaults.Visual.TerminalFont);
        config.Visual.CrtScanlineOpacity = double.IsFinite(config.Visual.CrtScanlineOpacity)
            ? Math.Clamp(config.Visual.CrtScanlineOpacity, 0, 0.3) : defaults.Visual.CrtScanlineOpacity;
        config.Visual.CrtGlow = double.IsFinite(config.Visual.CrtGlow)
            ? Math.Clamp(config.Visual.CrtGlow, 0, 0.6) : defaults.Visual.CrtGlow;
        config.Audio.Volume = double.IsFinite(config.Audio.Volume) ? Math.Clamp(config.Audio.Volume, 0, 1) : defaults.Audio.Volume;
        config.Visual.ParticleCount = Math.Clamp(config.Visual.ParticleCount, 0, 200);
        var color = config.Visual.ParticleColor;
        if (string.IsNullOrWhiteSpace(color) || color.Length is not (7 or 9)
            || color[0] != '#' || !color.Skip(1).All(Uri.IsHexDigit))
            config.Visual.ParticleColor = defaults.Visual.ParticleColor;
        config.Visual.ParticleSpeed = double.IsFinite(config.Visual.ParticleSpeed)
            ? Math.Clamp(config.Visual.ParticleSpeed, 0.5, 2) : 1;
        config.TentativasNao = Math.Clamp(config.TentativasNao, 0, 6);
        config.Duracao.Boot = Duration(config.Duracao.Boot, defaults.Duracao.Boot);
        config.Duracao.Anomaly = Duration(config.Duracao.Anomaly, defaults.Duracao.Anomaly);
        config.Duracao.Collapse = Duration(config.Duracao.Collapse, defaults.Duracao.Collapse);
        config.Duracao.Celebration = Duration(config.Duracao.Celebration, defaults.Duracao.Celebration);
        config.Mensagens.Boot = Text(config.Mensagens.Boot, defaults.Mensagens.Boot);
        config.Mensagens.Anomaly = Text(config.Mensagens.Anomaly, defaults.Mensagens.Anomaly);
        config.Mensagens.Invitation = Text(config.Mensagens.Invitation, defaults.Mensagens.Invitation);
        config.Mensagens.Success = Text(config.Mensagens.Success, defaults.Mensagens.Success);
        config.Mensagens.Thanks = Text(config.Mensagens.Thanks, defaults.Mensagens.Thanks);
        config.Mensagens.Declined = Text(config.Mensagens.Declined, defaults.Mensagens.Declined);
        config.Mensagens.CollapseTitle = Text(config.Mensagens.CollapseTitle, defaults.Mensagens.CollapseTitle);
        config.Mensagens.CollapseMessage = Text(config.Mensagens.CollapseMessage, defaults.Mensagens.CollapseMessage);
        config.Mensagens.InvitationHint = Text(config.Mensagens.InvitationHint, defaults.Mensagens.InvitationHint);
        static string[] Lines(string[]? lines, string[] fallback) =>
            lines is { Length: > 0 and <= 40 } && lines.All(line => !string.IsNullOrWhiteSpace(line) && line.Length <= 500)
                ? lines : fallback;
        config.Mensagens.BootLines = Lines(config.Mensagens.BootLines, defaults.Mensagens.BootLines);
        config.Mensagens.AnomalyLines = Lines(config.Mensagens.AnomalyLines, defaults.Mensagens.AnomalyLines);
        config.Mensagens.CollapseLines = Lines(config.Mensagens.CollapseLines, defaults.Mensagens.CollapseLines);
        config.Mensagens.AlertLines = Lines(config.Mensagens.AlertLines, defaults.Mensagens.AlertLines);
        config.Mensagens.NoHints = Lines(config.Mensagens.NoHints, defaults.Mensagens.NoHints);
        return config;
    }
}
