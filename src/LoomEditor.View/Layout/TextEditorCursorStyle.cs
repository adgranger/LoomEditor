// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>How the caret is drawn (Monaco: <c>TextEditorCursorStyle</c>).</summary>
public enum TextEditorCursorStyle
{
    /// <summary>A vertical bar as wide as <c>cursorWidth</c> (2 DIPs by default).</summary>
    Line,

    /// <summary>A filled cell the size of the next character, which is drawn on top in the background color.</summary>
    Block,

    /// <summary>A 2 DIPs high bar under the next character.</summary>
    Underline,

    /// <summary>A one device pixel wide vertical bar.</summary>
    LineThin,

    /// <summary>The outline of the cell of the next character.</summary>
    BlockOutline,

    /// <summary>A one device pixel high bar under the next character.</summary>
    UnderlineThin,
}
