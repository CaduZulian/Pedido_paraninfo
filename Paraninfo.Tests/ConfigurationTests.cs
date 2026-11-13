using System.Text.Json;
using Paraninfo.App.Models;
using Paraninfo.App.Services;
using Xunit;

namespace Paraninfo.Tests;

public sealed class ConfigurationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "Paraninfo-tests-" + Guid.NewGuid());
    private string ConfigPath => Path.Combine(_directory, "config.json");

    public ConfigurationTests() => Directory.CreateDirectory(_directory);

    [Theory]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{\"duracao\":\"inválido\"}")]
    public void InvalidJsonDoesNotPreventLoading(string json)
    {
        File.WriteAllText(ConfigPath, json);
        var config = new ConfigurationService().Load(ConfigPath);
        Assert.Equal(new InvitationConfig().Professor, config.Professor);
        Assert.NotNull(config.Duracao);
    }

    [Fact]
    public void MissingFileUsesDefaults()
    {
        var config = new ConfigurationService().Load(ConfigPath);
        Assert.Equal(35000, config.Duracao.Boot + config.Duracao.Anomaly + config.Duracao.Collapse);
        Assert.True(config.TelaCheia);
    }

    [Fact]
    public void PartialConfigRetainsValidFieldsAndRepairsNullOrOutOfRangeFields()
    {
        File.WriteAllText(ConfigPath, """
        {"professor":"Maria", "turma":null, "ano":0, "tentativasNao":99,
         "audio":{"volume":5}, "visual":{"particleCount":999},
         "duracao":{"boot":-1,"collapse":123},
         "mensagens":{"success":" ","thanks":"Valeu!","bootLines":[null]}}
        """);
        var config = new ConfigurationService().Load(ConfigPath);
        Assert.Equal("Maria", config.Professor);
        Assert.Equal(new InvitationConfig().Turma, config.Turma);
        Assert.Equal(2026, config.Ano);
        Assert.Equal(6, config.TentativasNao);
        Assert.Equal(1, config.Audio.Volume);
        Assert.Equal(200, config.Visual.ParticleCount);
        Assert.Equal(8000, config.Duracao.Boot);
        Assert.Equal(123, config.Duracao.Collapse);
        Assert.Equal("Valeu!", config.Mensagens.Thanks);
        Assert.False(string.IsNullOrWhiteSpace(config.Mensagens.Success));
        Assert.All(config.Mensagens.BootLines, line => Assert.False(string.IsNullOrWhiteSpace(line)));
    }

    [Fact]
    public void NullSectionsAndCaseInsensitivePropertiesAreSupported()
    {
        File.WriteAllText(ConfigPath, """{"PROFESSOR":"João","duracao":null,"mensagens":null,"audio":null,"visual":null}""");
        var config = new ConfigurationService().Load(ConfigPath);
        Assert.Equal("João", config.Professor);
        Assert.NotNull(config.Duracao);
        Assert.NotNull(config.Mensagens);
        Assert.NotNull(config.Audio);
        Assert.NotNull(config.Visual);
    }

    [Fact]
    public void CustomScriptAndAnimationSettingsArePreserved()
    {
        var original = new InvitationConfig { TentativasNao = 0, TelaCheia = false };
        original.Mensagens.BootLines = ["Olá, {professor}!"];
        original.Visual.ReducedMotion = true;
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(original));
        var loaded = new ConfigurationService().Load(ConfigPath);
        Assert.Equal(original.Mensagens.BootLines, loaded.Mensagens.BootLines);
        Assert.True(loaded.Visual.ReducedMotion);
        Assert.Equal(0, loaded.TentativasNao);
        Assert.False(loaded.TelaCheia);
    }

    [Fact]
    public void InvalidColorAndNullAlertScriptFallBackWithoutLosingOtherSettings()
    {
        File.WriteAllText(ConfigPath, """
        {"professor":"Maria","visual":{"particleColor":"vermelho","particleSpeed":100},
         "mensagens":{"alertLines":null}}
        """);
        var config = new ConfigurationService().Load(ConfigPath);
        Assert.Equal("Maria", config.Professor);
        Assert.Equal("#70FFAA", config.Visual.ParticleColor);
        Assert.Equal(2, config.Visual.ParticleSpeed);
        Assert.NotEmpty(config.Mensagens.AlertLines);
    }

    [Fact]
    public void ShippedConfigurationIsValidAndReachesInvitationWithinRequestedTime()
    {
        var file = Path.Combine(AppContext.BaseDirectory, "ExampleConfig.json");
        var json = File.ReadAllText(file);
        // Deserialização explícita impede que o fallback silencioso mascare JSON quebrado.
        Assert.NotNull(JsonSerializer.Deserialize<InvitationConfig>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));
        var config = new ConfigurationService().Load(file);
        Assert.InRange(config.Duracao.Boot + config.Duracao.Anomaly + config.Duracao.Collapse, 35000, 45000);
        Assert.True(config.TelaCheia);
        Assert.InRange(config.Audio.Volume, 0, 0.3);
    }

    [Fact]
    public void CustomNoHintsPreserveEmojiAndTokensAndInvalidHintsUseDefaults()
    {
        File.WriteAllText(ConfigPath, """
        {"professor":"Maria","mensagens":{"invitationHint":"Olá, {professor}",
         "noHints":["Vamos reconsiderar? 🙂","Confirma, {professor}?"]}}
        """);
        var service = new ConfigurationService();
        var config = service.Load(ConfigPath);
        var controller = new SequenceController(config, _ => { });
        Assert.Equal("Olá, Maria", controller.Expand(config.Mensagens.InvitationHint));
        Assert.Equal(new[] { "Vamos reconsiderar? 🙂", "Confirma, Maria?" }, config.Mensagens.NoHints.Select(controller.Expand));
        File.WriteAllText(ConfigPath, """{"mensagens":{"noHints":[null,""]}}""");
        Assert.Equal(new MessageConfig().NoHints, service.Load(ConfigPath).Mensagens.NoHints);
    }

    [Fact]
    public void RetroAppearanceCanBeDisabledAndInvalidStrengthsAreLimited()
    {
        File.WriteAllText(ConfigPath, """
        {"visual":{"terminalFont":" ","crtEnabled":false,"crtScanlineOpacity":2,"crtGlow":-1}}
        """);
        var config = new ConfigurationService().Load(ConfigPath);
        Assert.Equal("VT323", config.Visual.TerminalFont);
        Assert.False(config.Visual.CrtEnabled);
        Assert.Equal(0.3, config.Visual.CrtScanlineOpacity);
        Assert.Equal(0, config.Visual.CrtGlow);
    }

    [Fact]
    public void PublicTemplateUsesGenericIdentityAndMatchesSafeDefaults()
    {
        var config = new ConfigurationService().Load(Path.Combine(AppContext.BaseDirectory, "ExampleConfig.json"));
        var defaults = new InvitationConfig();
        Assert.Equal("Convidado", config.Professor);
        Assert.Equal(defaults.Turma, config.Turma);
        Assert.Equal(defaults.Instituicao, config.Instituicao);
        Assert.Equal(defaults.Disciplina, config.Disciplina);
        Assert.Equal(defaults.Mensagens.NoHints, config.Mensagens.NoHints);
    }

    public void Dispose()
    {
        // Remove somente o arquivo temporário criado por esta instância.
        if (File.Exists(ConfigPath)) File.Delete(ConfigPath);
        Directory.Delete(_directory, recursive: false);
    }
}
