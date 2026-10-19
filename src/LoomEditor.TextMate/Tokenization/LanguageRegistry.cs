// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/textMateTokenizationFeatureImpl.ts (_validateGrammarDefinition)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Grammar;
using CoreLanguageIdCodec = LoomEditor.Core.Tokens.LanguageIdCodec;

namespace LoomEditor.TextMate.Tokenization;

/// <summary>
/// The known languages: ids (encoded by a <see cref="CoreLanguageIdCodec"/>), file extensions and names, and their
/// grammars (VS Code: the language service and the grammar extension point; here the source is <c>language.json</c>
/// files). <c>plaintext</c> is always registered.
/// </summary>
/// <remarks>Threading: register everything first, then read; not thread-safe while registering.</remarks>
internal sealed class LanguageRegistry
{
    private readonly Dictionary<string, LanguageDefinition> _languages = new(StringComparer.Ordinal);

    /// <summary>Creates a registry that only knows plain text.</summary>
    public LanguageRegistry()
    {
        Register(new LanguageDefinition(CoreLanguageIdCodec.PlainTextLanguageId, ["Plain Text", "text"], [".txt"], [], null, []));
    }

    /// <summary>Maps the language ids to the integers stored in token metadata.</summary>
    public CoreLanguageIdCodec Codec { get; } = new();

    /// <summary>The language ids, in registration order.</summary>
    public IEnumerable<string> LanguageIds => _languages.Keys;

    /// <summary>Registers a language; a second definition of an id replaces the first one.</summary>
    public void Register(LanguageDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Codec.Register(definition.Id);
        _languages[definition.Id] = definition;
    }

    /// <summary><c>isRegisteredLanguageId</c>.</summary>
    public bool IsRegisteredLanguageId(string languageId) => _languages.ContainsKey(languageId);

    /// <summary>The language of a file extension (with or without its dot, case insensitive), or <see langword="null"/>.</summary>
    public string? GetLanguageIdByExtension(string extension)
    {
        string dotted = extension.StartsWith('.') ? extension : "." + extension;
        return _languages.Values
            .FirstOrDefault(l => l.Extensions.Any(e => string.Equals(e, dotted, StringComparison.OrdinalIgnoreCase)))?.Id;
    }

    /// <summary>The language of a file name: a declared file name first, else its extension; or <see langword="null"/>.</summary>
    public string? GetLanguageIdByFileName(string fileName)
    {
        string? byName = _languages.Values
            .FirstOrDefault(l => l.FileNames.Any(n => string.Equals(n, fileName, StringComparison.OrdinalIgnoreCase)))?.Id;
        return byName ?? GetLanguageIdByExtension(Path.GetExtension(fileName));
    }

    /// <summary>
    /// <c>_validateGrammarDefinition</c> for every grammar: embedded languages become encoded ids (unknown languages are
    /// dropped), token types become enums (unknown ones are dropped), brackets default to <c>["*"]</c> / <c>[]</c>.
    /// Call it once all the languages are registered.
    /// </summary>
    public List<ValidGrammarDefinition> CreateGrammarDefinitions()
    {
        var result = new List<ValidGrammarDefinition>();
        foreach (LanguageDefinition language in _languages.Values)
        {
            if (language.Grammar is not null)
            {
                result.Add(Validate(language.Grammar, language.Id));
            }

            result.AddRange(language.ExtraGrammars.Select(extra => Validate(extra, null)));
        }

        return result;
    }

    private ValidGrammarDefinition Validate(GrammarDefinition grammar, string? languageId)
    {
        var embeddedLanguages = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach ((string scope, string language) in grammar.EmbeddedLanguages)
        {
            if (IsRegisteredLanguageId(language))
            {
                embeddedLanguages[scope] = Codec.EncodeLanguageId(language);
            }
        }

        var tokenTypes = new Dictionary<string, StandardTokenType>(StringComparer.Ordinal);
        foreach ((string scope, string tokenType) in grammar.TokenTypes)
        {
            switch (tokenType)
            {
                case "string":
                    tokenTypes[scope] = StandardTokenType.String;
                    break;
                case "other":
                    tokenTypes[scope] = StandardTokenType.Other;
                    break;
                case "comment":
                    tokenTypes[scope] = StandardTokenType.Comment;
                    break;
                case "regex":
                    tokenTypes[scope] = StandardTokenType.RegEx;
                    break;
            }
        }

        return new ValidGrammarDefinition(
            grammar.Location,
            languageId,
            grammar.ScopeName,
            embeddedLanguages,
            tokenTypes,
            grammar.InjectTo,
            grammar.BalancedBracketScopes ?? ["*"],
            grammar.UnbalancedBracketScopes ?? []);
    }
}
