// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>IFindNextMatchResult</c>: the rule whose regex matched, and the captures.</summary>
/// <param name="RuleId"><c>ruleId</c>.</param>
/// <param name="CaptureIndices"><c>captureIndices</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record IFindNextMatchResult(RuleId RuleId, IReadOnlyList<OnigCaptureIndex> CaptureIndices);

/// <summary><c>CompiledRule</c>: a list of regexes compiled into one scanner, with the rule id of each regex.</summary>
/// <remarks>Threading: single-threaded; it owns its scanner and disposes it.</remarks>
internal sealed class CompiledRule : IDisposable
{
    private readonly IOnigScanner _scanner;
    private readonly IReadOnlyList<string> _regExps;
    private readonly IReadOnlyList<RuleId> _rules;

    /// <summary>Compiles <paramref name="regExps"/>.</summary>
    /// <exception cref="OnigException">A regex does not compile.</exception>
    public CompiledRule(IOnigLib onigLib, IReadOnlyList<string> regExps, IReadOnlyList<RuleId> rules)
    {
        ArgumentNullException.ThrowIfNull(onigLib);
        _regExps = regExps;
        _rules = rules;
        _scanner = onigLib.CreateOnigScanner(regExps);
    }

    /// <summary><c>dispose</c>.</summary>
    public void Dispose() => _scanner.Dispose();

    /// <inheritdoc />
    public override string ToString()
    {
        var r = new List<string>();
        for (int i = 0; i < _rules.Count; i++)
        {
            r.Add("   - " + _rules[i] + ": " + _regExps[i]);
        }

        return string.Join("\n", r);
    }

    /// <summary><c>findNextMatchSync</c> on a string created by the same library.</summary>
    public IFindNextMatchResult? FindNextMatchSync(IOnigString str, int startPosition, FindOption options) =>
        ToResult(_scanner.FindNextMatchSync(str, startPosition, options));

    /// <summary><c>findNextMatchSync</c> on a plain string.</summary>
    public IFindNextMatchResult? FindNextMatchSync(string str, int startPosition, FindOption options) =>
        ToResult(_scanner.FindNextMatchSync(str, startPosition, options));

    private IFindNextMatchResult? ToResult(IOnigMatch? result) =>
        result is null ? null : new IFindNextMatchResult(_rules[result.Index], result.CaptureIndices);
}
