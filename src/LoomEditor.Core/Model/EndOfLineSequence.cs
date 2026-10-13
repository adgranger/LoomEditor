// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>An end-of-line sequence a model can be switched to.</summary>
/// <remarks>
/// <para>Monaco: <c>EndOfLineSequence</c>.</para>
/// <para>Threading: plain enum; thread-safe.</para>
/// </remarks>
public enum EndOfLineSequence
{
    /// <summary>Use line feed (<c>\n</c>) as the end of line character.</summary>
    LF = 0,

    /// <summary>Use carriage return and line feed (<c>\r\n</c>) as the end of line character.</summary>
    CRLF = 1,
}
