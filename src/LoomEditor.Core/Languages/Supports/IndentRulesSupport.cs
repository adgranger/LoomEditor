// Ported from microsoft/vscode: src/vs/editor/common/languages/supports/indentRules.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages.Supports;

/// <summary>The bits returned by <see cref="IndentRulesSupport.GetIndentMetadata"/> (Monaco: <c>IndentConsts</c>).</summary>
/// <remarks>Threading: constants.</remarks>
internal static class IndentConsts
{
    public const int IncreaseMask = 0b0001;
    public const int DecreaseMask = 0b0010;
    public const int IndentNextLineMask = 0b0100;
    public const int UnIndentMask = 0b1000;
}

/// <summary>Evaluates the indentation rules of a language against a line (Monaco: <c>IndentRulesSupport</c>).</summary>
/// <remarks>
/// Monaco resets <c>lastIndex</c> of global regexes before each test; .NET regexes are stateless, so there is no
/// <c>resetGlobalRegex</c>. Threading: immutable.
/// </remarks>
internal sealed class IndentRulesSupport(IndentationRule indentationRules)
{
    private readonly IndentationRule _indentationRules = indentationRules;

    public bool ShouldIncrease(string text) =>
        // Monaco also has a commented-out indentNextLinePattern check here.
        _indentationRules.IncreaseIndentPattern.IsMatch(text);

    public bool ShouldDecrease(string text) => _indentationRules.DecreaseIndentPattern.IsMatch(text);

    public bool ShouldIndentNextLine(string text) => _indentationRules.IndentNextLinePattern?.IsMatch(text) ?? false;

    /// <summary>Whether the text matches <c>unIndentedLinePattern</c>.</summary>
    public bool ShouldIgnore(string text) => _indentationRules.UnIndentedLinePattern?.IsMatch(text) ?? false;

    public int GetIndentMetadata(string text)
    {
        int ret = 0;
        if (ShouldIncrease(text))
        {
            ret += IndentConsts.IncreaseMask;
        }

        if (ShouldDecrease(text))
        {
            ret += IndentConsts.DecreaseMask;
        }

        if (ShouldIndentNextLine(text))
        {
            ret += IndentConsts.IndentNextLineMask;
        }

        if (ShouldIgnore(text))
        {
            ret += IndentConsts.UnIndentMask;
        }

        return ret;
    }
}
