namespace LoomEditor.TextMate.Tokenization;

/// <summary>
/// A TextMate grammar declared by a <c>language.json</c> (VS Code: <c>ITMSyntaxExtensionPoint</c>, ADR 0008 decision 6).
/// A list that is missing or not an array of strings is <see langword="null"/>: the defaults are applied when the
/// definition is validated (<see cref="LanguageRegistry.CreateGrammarDefinitions"/>).
/// </summary>
/// <param name="Location">The resource name of the grammar file (the language folder joined with <c>path</c>).</param>
/// <param name="ScopeName"><c>scopeName</c>.</param>
/// <param name="EmbeddedLanguages"><c>embeddedLanguages</c>: scope name → language id.</param>
/// <param name="TokenTypes"><c>tokenTypes</c>: selector → <c>string</c>, <c>comment</c>, <c>other</c> or <c>regex</c>.</param>
/// <param name="InjectTo"><c>injectTo</c>: the scope names this grammar is injected into.</param>
/// <param name="BalancedBracketScopes"><c>balancedBracketScopes</c>.</param>
/// <param name="UnbalancedBracketScopes"><c>unbalancedBracketScopes</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record GrammarDefinition(
    string Location,
    string ScopeName,
    IReadOnlyDictionary<string, string> EmbeddedLanguages,
    IReadOnlyDictionary<string, string> TokenTypes,
    IReadOnlyList<string>? InjectTo,
    IReadOnlyList<string>? BalancedBracketScopes,
    IReadOnlyList<string>? UnbalancedBracketScopes);

/// <summary>A language declared by a <c>language.json</c>: its names, its file matching and its grammars.</summary>
/// <param name="Id">The language id.</param>
/// <param name="Aliases"><c>aliases</c>.</param>
/// <param name="Extensions"><c>extensions</c>, with their leading dot.</param>
/// <param name="FileNames"><c>filenames</c>.</param>
/// <param name="Grammar">The grammar of the language, or <see langword="null"/> (plain text).</param>
/// <param name="ExtraGrammars">Other grammars of the folder: injections and grammars only included by others.</param>
/// <param name="ConfigurationLocation">
/// The resource name of the language configuration file (the language folder joined with <c>configuration</c>), or
/// <see langword="null"/>.
/// </param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record LanguageDefinition(
    string Id,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> Extensions,
    IReadOnlyList<string> FileNames,
    GrammarDefinition? Grammar,
    IReadOnlyList<GrammarDefinition> ExtraGrammars,
    string? ConfigurationLocation = null);
