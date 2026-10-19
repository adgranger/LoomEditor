namespace LoomEditor.TextMate.Oniguruma;

/// <summary>The native Oniguruma 6.9.10 engine (ADR 0007).</summary>
/// <remarks>Threading: stateless; safe on any thread.</remarks>
internal sealed class OnigurumaEngine : IRegexEngine
{
    /// <inheritdoc />
    public string Version => OnigLibrary.Version;
}
