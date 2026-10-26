// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (CompletionItemKinds.toLabel)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;

namespace LoomEditor.View.Suggest;

/// <summary>The kind badge text and the kind names of the completion list. No codicon font exists, so a badge is two letters.</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class SuggestKindBadges
{
    /// <summary>The two-letter badge of <paramref name="kind"/>; empty for an unknown value.</summary>
    public static string Glyph(CompletionItemKind kind) => kind switch
    {
        CompletionItemKind.Method => "Me",
        CompletionItemKind.Function => "Fn",
        CompletionItemKind.Constructor => "Ct",
        CompletionItemKind.Field => "Fd",
        CompletionItemKind.Variable => "Va",
        CompletionItemKind.Class => "Cl",
        CompletionItemKind.Struct => "St",
        CompletionItemKind.Interface => "If",
        CompletionItemKind.Module => "Mo",
        CompletionItemKind.Property => "Pr",
        CompletionItemKind.Event => "Ev",
        CompletionItemKind.Operator => "Op",
        CompletionItemKind.Unit => "Un",
        CompletionItemKind.Value => "Vl",
        CompletionItemKind.Constant => "Cn",
        CompletionItemKind.Enum => "En",
        CompletionItemKind.EnumMember => "EM",
        CompletionItemKind.Keyword => "Kw",
        CompletionItemKind.Text => "Tx",
        CompletionItemKind.Color => "Co",
        CompletionItemKind.File => "Fi",
        CompletionItemKind.Reference => "Rf",
        CompletionItemKind.Customcolor => "Cc",
        CompletionItemKind.Folder => "Fo",
        CompletionItemKind.TypeParameter => "Tp",
        CompletionItemKind.User => "Us",
        CompletionItemKind.Issue => "Is",
        CompletionItemKind.Tool => "To",
        CompletionItemKind.Snippet => "Sn",
        _ => "",
    };

    /// <summary>Monaco: <c>CompletionItemKinds.toLabel</c>; empty for an unknown value.</summary>
    public static string ToLabel(CompletionItemKind kind) => kind switch
    {
        CompletionItemKind.Method => "Method",
        CompletionItemKind.Function => "Function",
        CompletionItemKind.Constructor => "Constructor",
        CompletionItemKind.Field => "Field",
        CompletionItemKind.Variable => "Variable",
        CompletionItemKind.Class => "Class",
        CompletionItemKind.Struct => "Struct",
        CompletionItemKind.Interface => "Interface",
        CompletionItemKind.Module => "Module",
        CompletionItemKind.Property => "Property",
        CompletionItemKind.Event => "Event",
        CompletionItemKind.Operator => "Operator",
        CompletionItemKind.Unit => "Unit",
        CompletionItemKind.Value => "Value",
        CompletionItemKind.Constant => "Constant",
        CompletionItemKind.Enum => "Enum",
        CompletionItemKind.EnumMember => "Enum Member",
        CompletionItemKind.Keyword => "Keyword",
        CompletionItemKind.Text => "Text",
        CompletionItemKind.Color => "Color",
        CompletionItemKind.File => "File",
        CompletionItemKind.Reference => "Reference",
        CompletionItemKind.Customcolor => "Custom Color",
        CompletionItemKind.Folder => "Folder",
        CompletionItemKind.TypeParameter => "Type Parameter",
        CompletionItemKind.User => "User",
        CompletionItemKind.Issue => "Issue",
        CompletionItemKind.Tool => "Tool",
        CompletionItemKind.Snippet => "Snippet",
        _ => "",
    };
}
