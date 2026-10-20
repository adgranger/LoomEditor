// Ported from microsoft/vscode: src/vs/editor/test/common/modes/supports/onEnterRules.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;

namespace LoomEditor.Core.Tests.Languages.TestHelpers;

/// <summary>The on-enter rules of a few languages, for tests (regex literals copied verbatim from Monaco).</summary>
/// <remarks>Threading: immutable.</remarks>
internal static class OnEnterRules
{
    public static readonly IReadOnlyList<OnEnterRule> JavascriptOnEnterRules =
    [
        // e.g. /** | */
        new(
            BeforeText: new Regex(@"^\s*\/\*\*(?!\/)([^\*]|\*(?!\/))*$", RegexOptions.CultureInvariant),
            AfterText: new Regex(@"^\s*\*\/$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.IndentOutdent, AppendText: " * ")),
        // e.g. /** ...|
        new(
            BeforeText: new Regex(@"^\s*\/\*\*(?!\/)([^\*]|\*(?!\/))*$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.None, AppendText: " * ")),
        // e.g.  * ...|
        new(
            BeforeText: new Regex(@"^(\t|[ ])*[ ]\*([ ]([^\*]|\*(?!\/))*)?$", RegexOptions.CultureInvariant),
            PreviousLineText: new Regex(@"(?=^(\s*(\/\*\*|\*)).*)(?=(?!(\s*\*\/)))", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.None, AppendText: "* ")),
        // e.g.  */|
        new(
            BeforeText: new Regex(@"^(\t|[ ])*[ ]\*\/\s*$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.None, RemoveText: 1)),
        // e.g.  *-----*/|
        new(
            BeforeText: new Regex(@"^(\t|[ ])*[ ]\*[^/]*\*\/\s*$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.None, RemoveText: 1)),
        new(
            BeforeText: new Regex(@"^\s*(\bcase\s.+:|\bdefault:)$", RegexOptions.CultureInvariant),
            AfterText: new Regex(@"^(?!\s*(\bcase\b|\bdefault\b))", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.Indent)),
        new(
            PreviousLineText: new Regex(@"^\s*(((else ?)?if|for|while)\s*\(.*\)\s*|else\s*)$", RegexOptions.CultureInvariant),
            BeforeText: new Regex(@"^\s+([^{i\s]|i(?!f\b))", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.Outdent)),
        // Indent when pressing enter from inside ()
        new(
            BeforeText: new Regex(@"^.*\([^\)]*$", RegexOptions.CultureInvariant),
            AfterText: new Regex(@"^\s*\).*$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.IndentOutdent, AppendText: "\t")),
        // Indent when pressing enter from inside {}
        new(
            BeforeText: new Regex(@"^.*\{[^\}]*$", RegexOptions.CultureInvariant),
            AfterText: new Regex(@"^\s*\}.*$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.IndentOutdent, AppendText: "\t")),
        // Indent when pressing enter from inside []
        new(
            BeforeText: new Regex(@"^.*\[[^\]]*$", RegexOptions.CultureInvariant),
            AfterText: new Regex(@"^\s*\].*$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.IndentOutdent, AppendText: "\t")),
    ];

    public static readonly IReadOnlyList<OnEnterRule> PhpOnEnterRules =
    [
        new(
            BeforeText: new Regex(@"^\s*\/\*\*(?!\/)([^\*]|\*(?!\/))*$", RegexOptions.CultureInvariant),
            AfterText: new Regex(@"^\s*\*\/$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.IndentOutdent, AppendText: " * ")),
        new(
            BeforeText: new Regex(@"^\s*\/\*\*(?!\/)([^\*]|\*(?!\/))*$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.None, AppendText: " * ")),
        new(
            BeforeText: new Regex(@"^(\t|(\ \ ))*\ \*(\ ([^\*]|\*(?!\/))*)?$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.None, AppendText: "* ")),
        new(
            BeforeText: new Regex(@"^(\t|(\ \ ))*\ \*\/\s*$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.None, RemoveText: 1)),
        new(
            BeforeText: new Regex(@"^(\t|(\ \ ))*\ \*[^/]*\*\/\s*$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.None, RemoveText: 1)),
        new(
            BeforeText: new Regex(@"^\s+([^{i\s]|i(?!f\b))", RegexOptions.CultureInvariant),
            PreviousLineText: new Regex(@"^\s*(((else ?)?if|for(each)?|while)\s*\(.*\)\s*|else\s*)$", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.Outdent)),
    ];

    public static readonly IReadOnlyList<OnEnterRule> CppOnEnterRules =
    [
        new(
            PreviousLineText: new Regex(@"^\s*(((else ?)?if|for|while)\s*\(.*\)\s*|else\s*)$", RegexOptions.CultureInvariant),
            BeforeText: new Regex(@"^\s+([^{i\s]|i(?!f\b))", RegexOptions.CultureInvariant),
            Action: new EnterAction(IndentAction.Outdent)),
    ];

    public static readonly IReadOnlyList<OnEnterRule> HtmlOnEnterRules =
    [
        new(
            BeforeText: new Regex(@"<(?!(?:area|base|br|col|embed|hr|img|input|keygen|link|menuitem|meta|param|source|track|wbr))([_:\w][_:\w\-.\d]*)(?:(?:[^'""/>]|""[^""]*""|'[^']*')*?(?!\/)>)[^<]*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
            AfterText: new Regex(@"^<\/([_:\w][_:\w\-.\d]*)\s*>", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
            Action: new EnterAction(IndentAction.IndentOutdent)),
        new(
            BeforeText: new Regex(@"<(?!(?:area|base|br|col|embed|hr|img|input|keygen|link|menuitem|meta|param|source|track|wbr))([_:\w][_:\w\-.\d]*)(?:(?:[^'""/>]|""[^""]*""|'[^']*')*?(?!\/)>)[^<]*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
            Action: new EnterAction(IndentAction.Indent)),
    ];

    public static readonly IReadOnlyList<OnEnterRule> VbOnEnterRules =
    [
        // Prevent indent after End statements and block terminators (but NOT ElseIf...Then or Else which should indent)
        new(
            BeforeText: new Regex(@"^\s*((End\s+(If|Sub|Function|Class|Module|Enum|Structure|Interface|Namespace|With|Select|Try|While|For|Property|Get|Set|SyncLock|Using|AddHandler|RaiseEvent|RemoveHandler|Event|Operator))|Loop|Next|Wend|Until)\b.*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
            Action: new EnterAction(IndentAction.None)),
    ];
}
