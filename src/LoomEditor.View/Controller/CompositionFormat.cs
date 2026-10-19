// Ported from microsoft/vscode: src/vs/editor/browser/controller/editContext/native/nativeEditContext.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Controller;

/// <summary>How the IME underlines a part of the composed text (Monaco: <c>CompositionClassName</c>).</summary>
internal enum CompositionUnderline
{
    /// <summary>No underline.</summary>
    None,

    /// <summary>A thin underline: text that is typed but not converted (Monaco: <c>secondary</c>).</summary>
    Secondary,

    /// <summary>A thick underline: the segment being converted (Monaco: <c>primary</c>).</summary>
    Primary,
}

/// <summary>A part of the composed text with its underline. Monaco keeps these as model decorations; LoomEditor keeps them on the view model.</summary>
/// <param name="Range">The range in the model.</param>
/// <param name="Underline">The underline to draw.</param>
internal readonly record struct CompositionFormat(Range Range, CompositionUnderline Underline);
