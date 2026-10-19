// Ported from microsoft/vscode-textmate: src/grammar/basicScopesAttributeProvider.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>BasicScopeAttributes</c>: the language id and standard token type that a scope name implies.</summary>
/// <param name="LanguageId"><c>languageId</c>.</param>
/// <param name="TokenType"><c>tokenType</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record BasicScopeAttributes(int LanguageId, OptionalStandardTokenType TokenType);

/// <summary><c>BasicScopeAttributesProvider</c>: maps scope names to their embedded language and standard token type.</summary>
/// <remarks>Threading: single-threaded (the cache is not synchronized), like the grammar that owns it.</remarks>
internal sealed partial class BasicScopeAttributesProvider
{
    private static readonly BasicScopeAttributes NullScopeMetadata = new(0, OptionalStandardTokenType.Other);

    private readonly BasicScopeAttributes _defaultAttributes;
    private readonly ScopeMatcher<int> _embeddedLanguagesMatcher;
    private readonly CachedFn<string, BasicScopeAttributes> _getBasicScopeAttributes;

    /// <summary>Creates the provider.</summary>
    /// <param name="initialLanguageId">The language id of the grammar itself.</param>
    /// <param name="embeddedLanguages">Scope name → language id (<c>IEmbeddedLanguagesMap</c>), if any.</param>
    public BasicScopeAttributesProvider(int initialLanguageId, IReadOnlyDictionary<string, int>? embeddedLanguages)
    {
        _defaultAttributes = new BasicScopeAttributes(initialLanguageId, OptionalStandardTokenType.NotSet);
        _embeddedLanguagesMatcher = new ScopeMatcher<int>([.. embeddedLanguages ?? new Dictionary<string, int>()]);
        _getBasicScopeAttributes = new CachedFn<string, BasicScopeAttributes>(scopeName =>
            new BasicScopeAttributes(ScopeToLanguage(scopeName), ToStandardTokenType(scopeName)));
    }

    /// <summary><c>getDefaultAttributes</c>.</summary>
    public BasicScopeAttributes GetDefaultAttributes() => _defaultAttributes;

    /// <summary><c>getBasicScopeAttributes</c>: <see langword="null"/> gives language 0 and token type <c>Other</c>.</summary>
    public BasicScopeAttributes GetBasicScopeAttributes(string? scopeName)
        => scopeName is null ? NullScopeMetadata : _getBasicScopeAttributes.Get(scopeName);

    /// <summary>
    /// Given a produced TM scope, returns the language that token describes, or 0 if unknown (<c>_scopeToLanguage</c>).
    /// e.g. source.html => html, source.css.embedded.html => css, punctuation.definition.tag.html => 0.
    /// </summary>
    private int ScopeToLanguage(string scope)
        => _embeddedLanguagesMatcher.TryMatch(scope, out int languageId) ? languageId : 0;

    private static OptionalStandardTokenType ToStandardTokenType(string scopeName)
    {
        Match m = StandardTokenTypeRegExp().Match(scopeName);
        if (!m.Success)
        {
            return OptionalStandardTokenType.NotSet;
        }

        return m.Groups[1].Value switch
        {
            "comment" => OptionalStandardTokenType.Comment,
            "string" => OptionalStandardTokenType.String,
            "regex" => OptionalStandardTokenType.RegEx,
            "meta.embedded" => OptionalStandardTokenType.Other,
            _ => throw new InvalidOperationException("Unexpected match for standard token type!"),
        };
    }

    [GeneratedRegex(@"\b(comment|string|regex|meta\.embedded)\b", RegexOptions.CultureInvariant)]
    private static partial Regex StandardTokenTypeRegExp();
}
