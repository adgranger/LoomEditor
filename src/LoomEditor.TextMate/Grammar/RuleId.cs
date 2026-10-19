// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary>
/// <c>RuleId</c>: the id of a compiled rule (upstream's branded number). Two special values: <see cref="End"/> (the
/// end regex matched) and <see cref="While"/> (the while regex matched).
/// </summary>
/// <param name="Value">The number.</param>
/// <remarks>Threading: a value type.</remarks>
internal readonly record struct RuleId(int Value)
{
    /// <summary><c>endRuleId</c>: this is a special constant to indicate that the end regexp matched.</summary>
    public static readonly RuleId End = new(-1);

    /// <summary><c>whileRuleId</c>: this is a special constant to indicate that the while regexp matched.</summary>
    public static readonly RuleId While = new(-2);

    /// <summary><c>ruleIdFromNumber</c>.</summary>
    public static RuleId FromNumber(int id) => new(id);

    /// <summary><c>ruleIdToNumber</c>.</summary>
    public static int ToNumber(RuleId id) => id.Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
