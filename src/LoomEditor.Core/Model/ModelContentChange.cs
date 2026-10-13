// Ported from microsoft/vscode: src/vs/editor/common/model/mirrorTextModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model;

/// <summary>A change of the model content: the old range that was replaced and its new text.</summary>
/// <remarks>
/// <para>Monaco: <c>IModelContentChange</c>.</para>
/// <para>Threading: immutable.</para>
/// </remarks>
/// <param name="Range">The old range that got replaced.</param>
/// <param name="RangeOffset">The offset of the range that got replaced.</param>
/// <param name="RangeLength">The length of the range that got replaced.</param>
/// <param name="Text">The new text for the range.</param>
public sealed record ModelContentChange(Range Range, int RangeOffset, int RangeLength, string Text);
