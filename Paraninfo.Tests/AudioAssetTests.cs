using System.Text;
using Xunit;

namespace Paraninfo.Tests;

public sealed class AudioAssetTests
{
    [Theory]
    [InlineData("type")]
    [InlineData("alert")]
    [InlineData("collapse")]
    [InlineData("success")]
    [InlineData("failure")]
    public void EmbeddedSoundSourcesAreValidShortPcmWaves(string name)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Sounds", name + ".wav"));
        Assert.True(bytes.Length > 44);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.Equal("WAVEfmt ", Encoding.ASCII.GetString(bytes, 8, 8));
        Assert.Equal("data", Encoding.ASCII.GetString(bytes, 36, 4));
        Assert.Equal(bytes.Length - 8, BitConverter.ToInt32(bytes, 4));
        Assert.Equal(bytes.Length - 44, BitConverter.ToInt32(bytes, 40));
        Assert.Equal(1, BitConverter.ToInt16(bytes, 20)); // PCM
        Assert.Equal(1, BitConverter.ToInt16(bytes, 22)); // Mono
        Assert.Equal(16, BitConverter.ToInt16(bytes, 34));
        var rate = BitConverter.ToInt32(bytes, 24);
        Assert.InRange(rate, 8000, 48000);
        Assert.InRange((bytes.Length - 44) / (2.0 * rate), 0.01, 1);
        Assert.Contains(Enumerable.Range(22, (bytes.Length - 44) / 2)
            .Select(index => BitConverter.ToInt16(bytes, index * 2)), sample => sample != 0);
    }
}
