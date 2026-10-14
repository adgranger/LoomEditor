namespace LoomEditor.View.Layout;

/// <summary>
/// The options <see cref="ViewLayout"/> reads. Monaco reads them from <c>IEditorConfiguration</c>; there is no options
/// system yet. The defaults are Monaco's.
/// </summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
/// <param name="LineHeight">Default line height, in pixels.</param>
/// <param name="PaddingTop">Space above the first line.</param>
/// <param name="PaddingBottom">Space below the last line.</param>
/// <param name="ScrollBeyondLastLine">Whether the editor can scroll until the last line is at the top.</param>
/// <param name="ScrollBeyondLastColumn">Extra horizontal space after the longest line, in typical characters.</param>
/// <param name="SmoothScrolling">Whether <see cref="ScrollType.Smooth"/> animates.</param>
/// <param name="HorizontalScrollbarVisibility">Visibility of the horizontal scrollbar.</param>
/// <param name="HorizontalScrollbarSize">Height of the horizontal scrollbar.</param>
/// <param name="IgnoreHorizontalScrollbarInContentHeight">Whether the horizontal scrollbar is left out of the content height.</param>
internal sealed record ViewLayoutOptions(
    int LineHeight,
    int PaddingTop = 0,
    int PaddingBottom = 0,
    bool ScrollBeyondLastLine = true,
    int ScrollBeyondLastColumn = 4,
    bool SmoothScrolling = false,
    ScrollbarVisibility HorizontalScrollbarVisibility = ScrollbarVisibility.Auto,
    int HorizontalScrollbarSize = 12,
    bool IgnoreHorizontalScrollbarInContentHeight = false);
