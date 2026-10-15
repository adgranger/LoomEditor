// Ported from microsoft/vscode: src/vs/editor/common/cursorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>The part of a model (or view model) that the cursor move operations read (Monaco: <c>ICursorSimpleModel</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface ICursorSimpleModel
{
    int GetLineCount();

    string GetLineContent(int lineNumber);

    int GetLineMinColumn(int lineNumber);

    int GetLineMaxColumn(int lineNumber);

    int GetLineFirstNonWhitespaceColumn(int lineNumber);

    int GetLineLastNonWhitespaceColumn(int lineNumber);

    Position NormalizePosition(Position position, PositionAffinity affinity);

    /// <summary>The column at which indentation stops at a given line.</summary>
    int GetLineIndentColumn(int lineNumber);
}
