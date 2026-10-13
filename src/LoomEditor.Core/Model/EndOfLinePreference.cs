// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>Which end-of-line sequence to use when reading text from a model.</summary>
/// <remarks>
/// <para>Monaco: <c>EndOfLinePreference</c>.</para>
/// <para>Threading: plain enum; thread-safe.</para>
/// </remarks>
public enum EndOfLinePreference
{
    /// <summary>Use the end of line character identified in the text buffer.</summary>
    TextDefined = 0,

    /// <summary>Use line feed (<c>\n</c>) as the end of line character.</summary>
    LF = 1,

    /// <summary>Use carriage return and line feed (<c>\r\n</c>) as the end of line character.</summary>
    CRLF = 2,
}
