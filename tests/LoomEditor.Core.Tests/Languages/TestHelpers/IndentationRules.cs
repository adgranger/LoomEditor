// Ported from microsoft/vscode: src/vs/editor/test/common/modes/supports/indentationRules.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;

namespace LoomEditor.Core.Tests.Languages.TestHelpers;

/// <summary>The indentation rules of a few languages, for tests (regex literals copied verbatim from Monaco).</summary>
/// <remarks>Threading: immutable.</remarks>
internal static class IndentationRules
{
    public static readonly IndentationRule JavascriptIndentationRules = new(
        DecreaseIndentPattern: new Regex(@"^((?!.*?\/\*).*\*\/)?\s*[\}\]\)].*$", RegexOptions.CultureInvariant),
        IncreaseIndentPattern: new Regex(@"^((?!\/\/).)*(\{([^}""'`]*|(\t|[ ])*\/\/.*)|\([^)""'`]*|\[[^\]""'`]*)$", RegexOptions.CultureInvariant),
        // e.g.  * ...| or */| or *-----*/|
        UnIndentedLinePattern: new Regex(@"^(\t|[ ])*[ ]\*[^/]*\*\/\s*$|^(\t|[ ])*[ ]\*\/\s*$|^(\t|[ ])*[ ]\*([ ]([^\*]|\*(?!\/))*)?$", RegexOptions.CultureInvariant),
        IndentNextLinePattern: new Regex(@"^((.*=>\s*)|((.*[^\w]+|\s*)(if|while|for)\s*\(.*\)\s*))$", RegexOptions.CultureInvariant));

    public static readonly IndentationRule RubyIndentationRules = new(
        DecreaseIndentPattern: new Regex(@"^\s*([}\]]([,)]?\s*(#|$)|\.[a-zA-Z_]\w*\b)|(end|rescue|ensure|else|elsif)\b|(in|when)\s)", RegexOptions.CultureInvariant),
        IncreaseIndentPattern: new Regex(@"^\s*((begin|class|(private|protected)\s+def|def|else|elsif|ensure|for|if|module|rescue|unless|until|when|in|while|case)|([^#]*\sdo\b)|([^#]*=\s*(case|if|unless)))\b([^#\{;]|(\""|'|\/).*\4)*(#.*)?$", RegexOptions.CultureInvariant));

    public static readonly IndentationRule PhpIndentationRules = new(
        IncreaseIndentPattern: new Regex(@"({(?!.*}).*|\(|\[|((else(\s)?)?if|else|for(each)?|while|switch|case).*:)\s*((\/[/*].*|)?$|\?>)", RegexOptions.CultureInvariant),
        DecreaseIndentPattern: new Regex(@"^(.*\*\/)?\s*((\})|(\)+[;,])|(\]\)*[;,])|\b(else:)|\b((end(if|for(each)?|while|switch));))", RegexOptions.CultureInvariant));

    public static readonly IndentationRule GoIndentationRules = new(
        DecreaseIndentPattern: new Regex(@"^\s*(\bcase\b.*:|\bdefault\b:|}[)}]*[),]?|\)[,]?)$", RegexOptions.CultureInvariant),
        IncreaseIndentPattern: new Regex(@"^.*(\bcase\b.*:|\bdefault\b:|(\b(func|if|else|switch|select|for|struct)\b.*)?{[^}""'`]*|\([^)""'`]*)$", RegexOptions.CultureInvariant));

    public static readonly IndentationRule HtmlIndentationRules = new(
        DecreaseIndentPattern: new Regex(@"^\s*(<\/(?!html)[-_\.A-Za-z0-9]+\b[^>]*>|-->|\})", RegexOptions.CultureInvariant),
        IncreaseIndentPattern: new Regex(@"<(?!\?|(?:area|base|br|col|frame|hr|html|img|input|keygen|link|menuitem|meta|param|source|track|wbr)\b|[^>]*\/>)([-_\.A-Za-z0-9]+)(?=\s|>)\b[^>]*>(?!.*<\/\1>)|<!--(?!.*-->)|\{[^}""']*$", RegexOptions.CultureInvariant));

    public static readonly IndentationRule LatexIndentationRules = new(
        DecreaseIndentPattern: new Regex(@"^\s*\\end{(?!document)", RegexOptions.CultureInvariant),
        IncreaseIndentPattern: new Regex(@"\\begin{(?!document)([^}]*)}(?!.*\\end{\1})", RegexOptions.CultureInvariant));

    public static readonly IndentationRule LuaIndentationRules = new(
        DecreaseIndentPattern: new Regex(@"^\s*((\b(elseif|else|end|until)\b)|(\})|(\)))", RegexOptions.CultureInvariant),
        IncreaseIndentPattern: new Regex(@"^((?!(\-\-)).)*((\b(else|function|then|do|repeat)\b((?!\b(end|until)\b).)*)|(\{\s*))$", RegexOptions.CultureInvariant));

    public static readonly IndentationRule VbIndentationRules = new(
        // Decrease indent when line starts with End <keyword>, Else, ElseIf, Case, Catch, Finally, Loop, Next, Wend, Until
        DecreaseIndentPattern: new Regex(@"^\s*((End\s+(If|Sub|Function|Class|Module|Enum|Structure|Interface|Namespace|With|Select|Try|While|For|Property|Get|Set|SyncLock|Using|AddHandler|RaiseEvent|RemoveHandler|Event|Operator))|Else|ElseIf|Case|Catch|Finally|Loop|Next|Wend|Until)\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
        // Increase indent after lines with block-starting keywords (Sub, Function, Class, Module, If...Then, etc.)
        // Both alternatives are anchored to start of line with ^\s*
        IncreaseIndentPattern: new Regex(@"^\s*((If|ElseIf).*Then(?!.*End\s+If)\s*(('|REM).*)?|(Else|While|For|Do|Select\s+Case|Case|Sub|Function|Class|Module|Enum|Structure|Interface|Namespace|With|Try|Catch|Finally|SyncLock|Using|Property|Get|Set|AddHandler|RaiseEvent|RemoveHandler|Event|Operator)\b(?!.*\bEnd\s+(If|Sub|Function|Class|Module|Enum|Structure|Interface|Namespace|With|Select|Try|While|For|Property|Get|Set|SyncLock|Using|AddHandler|RaiseEvent|RemoveHandler|Event|Operator)\b).*(('|REM).*)?)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
}
