// Ported from microsoft/vscode-textmate: src/main.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;
using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Theme;

namespace LoomEditor.TextMate.Grammar;

/// <summary>
/// <c>RegistryOptions</c>: a registry helper that can locate grammars given scope names. Everything is synchronous
/// (ADR 0008, decision 3): upstream's <c>Promise</c>s are plain values.
/// </summary>
/// <param name="OnigLib"><c>onigLib</c>: the regex engine.</param>
/// <param name="LoadGrammar"><c>loadGrammar</c>: the raw grammar of a scope name, or <see langword="null"/>.</param>
/// <param name="Theme"><c>theme</c>.</param>
/// <param name="ColorMap"><c>colorMap</c>.</param>
/// <param name="GetInjections"><c>getInjections</c>: the scope names of the grammars injected into a scope name.</param>
/// <remarks>Threading: immutable; the functions are called on the thread that uses the registry.</remarks>
internal sealed record RegistryOptions(
    IOnigLib OnigLib,
    Func<string, RawGrammar?> LoadGrammar,
    RawTheme? Theme = null,
    IReadOnlyList<string>? ColorMap = null,
    Func<string, IReadOnlyList<string>?>? GetInjections = null);
