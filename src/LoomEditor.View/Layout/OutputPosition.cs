// Ported from microsoft/vscode: src/vs/editor/common/modelLineProjectionData.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

/// <summary>A position in the output of a wrapped line: a view line index and an offset in it (Monaco: <c>OutputPosition</c>).</summary>
/// <param name="OutputLineIndex">The index of the view line among those of the model line.</param>
/// <param name="OutputOffset">The 0-based offset in the view line, indent included.</param>
/// <remarks>Threading: a value type.</remarks>
internal readonly record struct OutputPosition(int OutputLineIndex, int OutputOffset)
{
    /// <summary>The position in the view, given the view line number of the first output line.</summary>
    public Position ToPosition(int baseLineNumber) => new(baseLineNumber + OutputLineIndex, OutputOffset + 1);

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{OutputLineIndex}:{OutputOffset}");
}
