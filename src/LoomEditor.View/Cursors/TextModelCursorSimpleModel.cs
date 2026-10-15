// Ported from microsoft/vscode: src/vs/editor/common/cursorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>An <see cref="ICursorSimpleModel"/> that reads a <see cref="TextModel"/>.</summary>
/// <remarks>
/// <para>
/// Not in Monaco: <c>ITextModel</c> implements <c>ICursorSimpleModel</c> there. <see cref="TextModel"/> lives in Core
/// and cannot implement a View interface, so this class forwards each call.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class TextModelCursorSimpleModel(TextModel model) : ICursorSimpleModel
{
    public int GetLineCount() => model.LineCount;

    public string GetLineContent(int lineNumber) => model.GetLineContent(lineNumber);

    public int GetLineMinColumn(int lineNumber) => model.GetLineMinColumn(lineNumber);

    public int GetLineMaxColumn(int lineNumber) => model.GetLineMaxColumn(lineNumber);

    public int GetLineFirstNonWhitespaceColumn(int lineNumber) => model.GetLineFirstNonWhitespaceColumn(lineNumber);

    public int GetLineLastNonWhitespaceColumn(int lineNumber) => model.GetLineLastNonWhitespaceColumn(lineNumber);

    public Position NormalizePosition(Position position, PositionAffinity affinity) => TextModel.NormalizePosition(position, affinity);

    public int GetLineIndentColumn(int lineNumber) => model.GetLineIndentColumn(lineNumber);
}
