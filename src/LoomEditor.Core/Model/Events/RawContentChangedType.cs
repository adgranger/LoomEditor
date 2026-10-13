// Ported from microsoft/vscode: src/vs/editor/common/textModelEvents.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Events;

/// <summary>The kind of a <see cref="ModelRawChange"/> (Monaco: <c>RawContentChangedType</c>).</summary>
public enum RawContentChangedType
{
    /// <summary>The model has been reset to a new value.</summary>
    Flush = 1,

    /// <summary>A line has changed.</summary>
    LineChanged = 2,

    /// <summary>Lines have been deleted.</summary>
    LinesDeleted = 3,

    /// <summary>Lines have been inserted.</summary>
    LinesInserted = 4,

    /// <summary>The end of line sequence has changed.</summary>
    EolChanged = 5,
}
