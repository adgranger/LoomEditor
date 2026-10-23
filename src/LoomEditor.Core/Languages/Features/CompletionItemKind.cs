// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (CompletionItemKind)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages.Features;

/// <summary>The kind of a completion item. Monaco's numbering (not LSP's: the LSP mapping lives in the LSP client).</summary>
/// <remarks>Threading: enum, safe to share between threads.</remarks>
public enum CompletionItemKind
{
    /// <summary>A method.</summary>
    Method,
    /// <summary>A function.</summary>
    Function,
    /// <summary>A constructor.</summary>
    Constructor,
    /// <summary>A field.</summary>
    Field,
    /// <summary>A variable.</summary>
    Variable,
    /// <summary>A class.</summary>
    Class,
    /// <summary>A struct.</summary>
    Struct,
    /// <summary>An interface.</summary>
    Interface,
    /// <summary>A module.</summary>
    Module,
    /// <summary>A property.</summary>
    Property,
    /// <summary>An event.</summary>
    Event,
    /// <summary>An operator.</summary>
    Operator,
    /// <summary>A unit.</summary>
    Unit,
    /// <summary>A value.</summary>
    Value,
    /// <summary>A constant.</summary>
    Constant,
    /// <summary>An enum.</summary>
    Enum,
    /// <summary>An enum member.</summary>
    EnumMember,
    /// <summary>A keyword.</summary>
    Keyword,
    /// <summary>Plain text.</summary>
    Text,
    /// <summary>A color.</summary>
    Color,
    /// <summary>A file.</summary>
    File,
    /// <summary>A reference.</summary>
    Reference,
    /// <summary>A custom color.</summary>
    Customcolor,
    /// <summary>A folder.</summary>
    Folder,
    /// <summary>A type parameter.</summary>
    TypeParameter,
    /// <summary>A user.</summary>
    User,
    /// <summary>An issue.</summary>
    Issue,
    /// <summary>A tool.</summary>
    Tool,
    /// <summary>A snippet.</summary>
    Snippet,
}
