// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>The end-of-line sequence to use when a new model has no line break to infer it from.</summary>
/// <remarks>
/// <para>Monaco: <c>DefaultEndOfLine</c>.</para>
/// <para>Threading: plain enum; thread-safe.</para>
/// </remarks>
public enum DefaultEndOfLine
{
    /// <summary>Use line feed (<c>\n</c>) as the end of line character.</summary>
    LF = 1,

    /// <summary>Use carriage return and line feed (<c>\r\n</c>) as the end of line character.</summary>
    CRLF = 2,
}
