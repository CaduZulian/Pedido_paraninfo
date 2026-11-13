namespace Paraninfo.App.Models;

public sealed class InvitationConfig
{
    public string Professor { get; set; } = "Convidado";
    public string Disciplina { get; set; } = "Disciplina de Exemplo";
    public string Turma { get; set; } = "Turma de Formandos";
    public string Instituicao { get; set; } = "Instituição de Ensino";
    public int Ano { get; set; } = 2026;
    public DurationConfig Duracao { get; set; } = new();
    public MessageConfig Mensagens { get; set; } = new();
    public AudioConfig Audio { get; set; } = new();
    public VisualConfig Visual { get; set; } = new();
    public bool TelaCheia { get; set; } = true;
    public int TentativasNao { get; set; } = 3;
}

public sealed class DurationConfig
{
    public int Boot { get; set; } = 8000;
    public int Anomaly { get; set; } = 12000;
    public int Collapse { get; set; } = 15000;
    public int Celebration { get; set; } = 6000;
}

public sealed class MessageConfig
{
    public string Boot { get; set; } = "Initializing academic system...";
    public string Anomaly { get; set; } = "ERROR 404: PARANINFO NOT FOUND";
    public string Invitation { get; set; } = "Professor, aceita ser nosso paraninfo?";
    public string Success { get; set; } = "PARANINFO INSTALLED SUCCESSFULLY!";
    public string Thanks { get; set; } = "Obrigado por fazer parte da nossa jornada!";
    public string Declined { get; set; } = "Tudo bem, professor. Nosso agradecimento continua!";
    public string CollapseTitle { get; set; } = "CRITICAL SYSTEM REQUIREMENT";
    public string CollapseMessage { get; set; } = "One final component is missing.";
    public string InvitationHint { get; set; } = "Sem pressa. A turma esperou por este momento.";
    public string[] NoHints { get; set; } =
    [
        "Confirma essa escolha?", "Vamos tentar novamente?", "Ainda dá tempo de aceitar.",
        "O convite continua aberto.", "A turma ficará feliz com sua presença.", "A decisão é sua."
    ];
    public string[] AlertLines { get; set; } =
        ["{anomaly}", "Dependency resolver unavailable.", "Manual confirmation required."];
    public string[] BootLines { get; set; } =
    [
        "{boot}", "Loading academic modules...", "Institution: {instituicao}",
        "Module: {disciplina}", "Checking graduation status...", "[OK] Knowledge",
        "[OK] Experience", "[OK] Teamwork", "[OK] Graduation requirements",
        "USER: {turma} / {ano}", "SYSTEM READY."
    ];
    public string[] AnomalyLines { get; set; } =
    [
        "WARNING: Unexpected request detected.", "Searching for missing component...",
        "{anomaly}", "Attempting to resolve...", "Dependency resolver: retry 01 / 03",
        "Academic protocol interrupted.", "Waiting for manual intervention..."
    ];
    public string[] CollapseLines { get; set; } =
    [
        "CRITICAL SYSTEM REQUIREMENT", "One final component is missing.",
        "Automatic recovery unavailable.", "Switching to human protocol...",
        "Professor {professor}"
    ];
}

public sealed class AudioConfig
{
    public bool Enabled { get; set; } = true;
    public double Volume { get; set; } = 0.2;
}

public sealed class VisualConfig
{
    public string TerminalFont { get; set; } = "VT323";
    public bool CrtEnabled { get; set; } = true;
    public double CrtScanlineOpacity { get; set; } = 0.10;
    public double CrtGlow { get; set; } = 0.25;
    public bool ReducedMotion { get; set; }
    public int ParticleCount { get; set; } = 100;
    public string ParticleColor { get; set; } = "#70FFAA";
    public double ParticleSpeed { get; set; } = 1;
}
