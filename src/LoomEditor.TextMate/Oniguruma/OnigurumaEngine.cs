namespace LoomEditor.TextMate.Oniguruma;

/// <summary>The native Oniguruma 6.9.10 engine (ADR 0007).</summary>
/// <remarks>Threading: stateless; safe on any thread. Each scanner is used by the thread that created it.</remarks>
internal sealed class OnigurumaEngine : IRegexEngine
{
    /// <inheritdoc />
    public string Version => OnigLibrary.Version;

    /// <inheritdoc />
    public IOnigScanner CreateOnigScanner(IReadOnlyList<string> sources) => new OnigScanner(sources);

    /// <inheritdoc />
    public IOnigString CreateOnigString(string str) => new OnigString(str);
}
