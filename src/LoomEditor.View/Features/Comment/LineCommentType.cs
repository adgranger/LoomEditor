// Ported from microsoft/vscode: src/vs/editor/contrib/comment/browser/lineCommentCommand.ts (Type, IInsertionPoint, ILinePreflightData, IPreflightData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Comment;

/// <summary>What a line comment command does (Monaco: <c>Type</c>).</summary>
/// <remarks>Threading: an enum; thread-safe.</remarks>
internal enum LineCommentType
{
    /// <summary>Removes the comments when every line has one, adds them otherwise.</summary>
    Toggle = 0,

    /// <summary>Adds comments.</summary>
    ForceAdd = 1,

    /// <summary>Removes comments.</summary>
    ForceRemove = 2,
}

/// <summary>What the command found out about one line (Monaco: <c>ILinePreflightData</c>).</summary>
/// <remarks>Mutable: the analysis and the insertion point alignment write to it. Threading: UI thread only.</remarks>
internal sealed class LinePreflightData
{
    public bool Ignore { get; set; }

    public required string CommentStr { get; init; }

    public int CommentStrOffset { get; set; }

    public int CommentStrLength { get; set; }
}

/// <summary>The analysis of the lines (Monaco: <c>IPreflightData</c>); <paramref name="Supported"/> is false when the language has no line comment.</summary>
/// <remarks>Threading: immutable apart from the lines it holds.</remarks>
internal sealed record PreflightData(bool Supported, bool ShouldRemoveComments, List<LinePreflightData> Lines);
