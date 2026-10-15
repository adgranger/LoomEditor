// Ported from microsoft/vscode: src/vs/editor/common/model.ts (ITextModelUpdateOptions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>The options to change with <see cref="TextModel.UpdateOptions"/>; an option left <see langword="null"/> keeps its value.</summary>
/// <remarks>
/// <para>Monaco: <c>ITextModelUpdateOptions</c>, without <c>bracketColorizationOptions</c>.</para>
/// <para>Threading: immutable value; thread-safe.</para>
/// </remarks>
/// <param name="TabSize">The new tab size.</param>
/// <param name="IndentSize">The new indent size. Takes precedence over <paramref name="IndentSizeIsTabSize"/>.</param>
/// <param name="IndentSizeIsTabSize">True to make the indent size follow the tab size (Monaco: <c>indentSize: 'tabSize'</c>).</param>
/// <param name="InsertSpaces">The new insert-spaces option.</param>
/// <param name="TrimAutoWhitespace">The new trim-auto-whitespace option.</param>
public readonly record struct TextModelUpdateOptions(
    int? TabSize = null,
    int? IndentSize = null,
    bool IndentSizeIsTabSize = false,
    bool? InsertSpaces = null,
    bool? TrimAutoWhitespace = null);
