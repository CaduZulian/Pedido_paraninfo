namespace Paraninfo.App.Services;

// Compara o que foi efetivamente desenhado, não as atualizações intermediárias do roteiro.
public sealed class TextRevealTracker
{
    private string _rendered = "";

    public void Reset() => _rendered = "";

    public bool Present(string text)
    {
        var previous = _rendered;
        _rendered = text;
        if (text.Length <= previous.Length || !text.StartsWith(previous, StringComparison.Ordinal)) return false;
        foreach (var character in text.AsSpan(previous.Length))
            if (!char.IsWhiteSpace(character)) return true;
        return false;
    }
}
