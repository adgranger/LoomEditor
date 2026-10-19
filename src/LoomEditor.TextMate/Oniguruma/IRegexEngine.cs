namespace LoomEditor.TextMate.Oniguruma;

/// <summary>
/// The regular-expression engine used by the TextMate tokenizer.
/// </summary>
/// <remarks>
/// <para>
/// TextMate grammars are written for Oniguruma's regex dialect, which .NET's <c>Regex</c> does not fully support.
/// The production implementation will call the native Oniguruma library through <c>[LibraryImport]</c>.
/// Keeping it behind this interface means: (1) the tokenizer can be tested with a fake engine,
/// and (2) Oniguruma could be replaced (it is no longer maintained upstream, see docs/adr/0001-baseline.md).
/// </para>
/// <para>Phase 4. Monaco equivalent: <c>IOnigLib</c> in vscode-textmate.</para>
/// </remarks>
internal interface IRegexEngine : IOnigLib
{
    /// <summary>Version string of the underlying engine, for diagnostics.</summary>
    string Version { get; }
}
