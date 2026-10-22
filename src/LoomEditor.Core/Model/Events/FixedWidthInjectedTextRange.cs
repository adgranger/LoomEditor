// Ported from microsoft/vscode: src/vs/editor/common/textModelEvents.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Events;

/// <summary>A fixed-width injected text range within a line (Monaco: <c>FixedWidthInjectedTextRange</c>).</summary>
/// <param name="StartOffset">The start offset in the injected line.</param>
/// <param name="EndOffset">The end offset in the injected line (equal to the start for width-only text).</param>
/// <param name="WidthInEm">The width in em.</param>
/// <remarks>Threading: a value type.</remarks>
internal readonly record struct FixedWidthInjectedTextRange(int StartOffset, int EndOffset, double WidthInEm);
