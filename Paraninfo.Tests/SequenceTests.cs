using Paraninfo.App.Models;
using Paraninfo.App.Services;
using Xunit;

namespace Paraninfo.Tests;

public sealed class SequenceTests
{
    private static InvitationConfig FastConfig() => new()
    {
        Professor = "Maria 🧑‍🏫",
        Duracao = new DurationConfig { Boot = 35, Anomaly = 35, Collapse = 35, Celebration = 35 }
    };

    [Fact]
    public async Task CompleteSequenceWaitsForAnswerAndKeepsOrderedStates()
    {
        var config = FastConfig();
        var states = new List<PresentationState>();
        var texts = new List<string>();
        var invitation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var controller = new SequenceController(config, texts.Add);
        controller.StateChanged += state =>
        {
            states.Add(state);
            if (state == PresentationState.Invitation) invitation.SetResult();
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var run = controller.RunAsync(timeout.Token);
        Assert.False(controller.Respond(true));
        await invitation.Task.WaitAsync(timeout.Token);
        await Task.Delay(50, timeout.Token);
        Assert.False(run.IsCompleted);
        Assert.Equal(PresentationState.Invitation, controller.State);
        Assert.True(controller.Respond(true));
        Assert.False(controller.Respond(true));
        await run;
        Assert.True(controller.Accepted);
        Assert.True(controller.HasResponded);
        Assert.Equal(new[] { PresentationState.BootSequence, PresentationState.SystemAnomaly,
            PresentationState.SystemCollapse, PresentationState.Invitation,
            PresentationState.Celebration, PresentationState.Finished }, states);
        Assert.Contains(texts, text => text.Contains(config.Professor));
        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.RunAsync());
    }

    [Theory]
    [InlineData(PresentationState.BootSequence)]
    [InlineData(PresentationState.SystemAnomaly)]
    [InlineData(PresentationState.SystemCollapse)]
    public async Task SkipInterruptsAnyIntroStage(PresentationState target)
    {
        var controller = new SequenceController(FastConfig(), _ => { });
        var states = new List<PresentationState>();
        controller.StateChanged += state =>
        {
            states.Add(state);
            if (state == target) controller.SkipToInvitation();
            if (state == PresentationState.Invitation) controller.Respond(false);
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await controller.RunAsync(timeout.Token);
        Assert.Contains(PresentationState.Invitation, states);
        Assert.DoesNotContain(PresentationState.Celebration, states);
        Assert.False(controller.Accepted);
        Assert.Equal(PresentationState.Finished, controller.State);
    }

    [Theory]
    [InlineData(PresentationState.BootSequence)]
    [InlineData(PresentationState.SystemAnomaly)]
    [InlineData(PresentationState.SystemCollapse)]
    [InlineData(PresentationState.Invitation)]
    [InlineData(PresentationState.Celebration)]
    public async Task CancellationStopsAnyStage(PresentationState target)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var controller = new SequenceController(FastConfig(), _ => { });
        controller.StateChanged += state =>
        {
            if (state == target) cancellation.Cancel();
            else if (state == PresentationState.Invitation) controller.Respond(true);
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => controller.RunAsync(cancellation.Token));
        Assert.Equal(PresentationState.Finished, controller.State);
        Assert.Equal(target == PresentationState.Celebration, controller.HasResponded);
    }

    [Fact]
    public async Task CancellationHasPriorityOverSkip()
    {
        using var cancellation = new CancellationTokenSource();
        var controller = new SequenceController(FastConfig(), _ => { });
        controller.SkipToInvitation();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => controller.RunAsync(cancellation.Token));
        Assert.Equal(PresentationState.Finished, controller.State);
    }

    [Fact]
    public async Task SkipBeforeStartAndDeclineFinishWithoutCelebration()
    {
        var controller = new SequenceController(FastConfig(), _ => throw new Exception("Introdução não deve rodar"));
        controller.SkipToInvitation();
        controller.StateChanged += state =>
        {
            if (state == PresentationState.Invitation) controller.Respond(false);
        };
        await controller.RunAsync();
        Assert.False(controller.Accepted);
        Assert.True(controller.HasResponded);
        Assert.Equal(PresentationState.Finished, controller.State);
    }

    [Fact]
    public async Task FailureBeforeAnswerIsNotReportedAsADecline()
    {
        var controller = new SequenceController(FastConfig(), _ => throw new InvalidOperationException("Falha simulada"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.RunAsync());
        Assert.Equal(PresentationState.Finished, controller.State);
        Assert.False(controller.HasResponded);
        Assert.False(controller.Accepted);
    }

    [Fact]
    public void PlaceholdersExpandFromConfiguration()
    {
        var config = FastConfig();
        var controller = new SequenceController(config, _ => { });
        Assert.Equal($"{config.Professor} / {config.Turma} / {config.Ano}",
            controller.Expand("{professor} / {turma} / {ano}"));
    }
}
