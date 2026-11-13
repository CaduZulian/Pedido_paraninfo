using Paraninfo.App.Services;
using Xunit;

namespace Paraninfo.Tests;

public sealed class TextRevealTests
{
    [Fact]
    public void EveryRenderedAdditionProducesACueWithoutDroppingFastUpdates()
    {
        var tracker = new TextRevealTracker();
        Assert.False(tracker.Present(""));
        Assert.True(tracker.Present("L"));
        Assert.True(tracker.Present("Lo"));
        Assert.True(tracker.Present("Loading"));
        Assert.False(tracker.Present("Loading"));
        Assert.False(tracker.Present("Loading"));
    }

    [Fact]
    public void PausesAndLineBreaksStaySilentButNextVisibleTextPlays()
    {
        var tracker = new TextRevealTracker();
        Assert.True(tracker.Present("[OK]"));
        Assert.False(tracker.Present("[OK] \t\r\n"));
        Assert.True(tracker.Present("[OK] \t\r\nERROR"));
    }

    [Fact]
    public void ClearingOrReplacingTextDoesNotReplayAnOldLine()
    {
        var tracker = new TextRevealTracker();
        Assert.True(tracker.Present("Loading modules"));
        Assert.False(tracker.Present(""));
        Assert.True(tracker.Present("E"));
        Assert.False(tracker.Present("Different text"));
        Assert.False(tracker.Present("Different text"));
    }

    [Fact]
    public void NewStageCanStartWithTheSameTextAndUnicodeStillProducesACue()
    {
        var tracker = new TextRevealTracker();
        Assert.True(tracker.Present("Olá"));
        tracker.Reset();
        Assert.True(tracker.Present("Olá"));
        Assert.True(tracker.Present("Olá 🧑‍🏫"));
        Assert.False(tracker.Present("Olá 🧑‍🏫"));
    }
}
