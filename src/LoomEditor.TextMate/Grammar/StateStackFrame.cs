// Ported from microsoft/vscode-textmate: src/grammar/grammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>StateStackFrame</c>: a serializable form of one <see cref="StateStackImpl"/> element.</summary>
/// <param name="RuleId"><c>ruleId</c>.</param>
/// <param name="EnterPos"><c>enterPos</c>.</param>
/// <param name="AnchorPos"><c>anchorPos</c>.</param>
/// <param name="BeginRuleCapturedEol"><c>beginRuleCapturedEOL</c>.</param>
/// <param name="EndRule"><c>endRule</c>.</param>
/// <param name="NameScopesList"><c>nameScopesList</c>.</param>
/// <param name="ContentNameScopesList"><c>contentNameScopesList</c>: on top of <paramref name="NameScopesList"/>.</param>
/// <remarks>Threading: treat as immutable once built.</remarks>
internal sealed record StateStackFrame(
    int RuleId,
    int? EnterPos,
    int? AnchorPos,
    bool BeginRuleCapturedEol,
    string? EndRule,
    List<AttributedScopeStackFrame> NameScopesList,
    List<AttributedScopeStackFrame> ContentNameScopesList);
