// Ported from microsoft/vscode-textmate: src/grammar/grammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Theme;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>IThemeProvider</c>: what a grammar needs from the theme.</summary>
/// <remarks>Threading: single-threaded, like the grammar.</remarks>
internal interface IThemeProvider
{
    /// <summary><c>themeMatch</c>: the style of a scope path, or <see langword="null"/> when no theme rule matches.</summary>
    StyleAttributes? ThemeMatch(ScopeStack scopePath);

    /// <summary><c>getDefaults</c>.</summary>
    StyleAttributes GetDefaults();
}

/// <summary><c>IGrammarRepository</c>: the raw grammars known to the registry.</summary>
/// <remarks>Threading: single-threaded, like the grammar.</remarks>
internal interface IGrammarRepository
{
    /// <summary><c>lookup</c>: the raw grammar of a scope name, or <see langword="null"/>.</summary>
    RawGrammar? Lookup(string scopeName);

    /// <summary><c>injections</c>: the scope names of the grammars injected into <paramref name="scopeName"/>.</summary>
    IReadOnlyList<string> Injections(string scopeName);
}
