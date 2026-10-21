using LoomEditor.View.Controller;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

// The underlines of the text being composed with an IME. Monaco: `nativeEditContext.css`, `.edit-context-composition-primary`
// (2px) and `-secondary` (1px) borders under the text, drawn through inline decorations; here they are view model state.
internal sealed partial class TextViewRenderer
{
    private void DrawCompositionUnderlines(
        IRenderSurface surface, ViewModel viewModel, ViewportData data, ILineLayout[] layouts,
        EditorLayoutInfo layoutInfo, double scrollLeft, int topShift, FontInfo font)
    {
        foreach (CompositionFormat format in viewModel.CompositionFormats)
        {
            if (format.Underline == CompositionUnderline.None)
            {
                continue;
            }

            float thickness = format.Underline == CompositionUnderline.Primary ? 2 : 1;
            Range range = viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(format.Range);
            int firstLine = Math.Max(range.StartLineNumber, data.StartLineNumber);
            int lastLine = Math.Min(range.EndLineNumber, data.EndLineNumber);
            for (int lineNumber = firstLine; lineNumber <= lastLine; lineNumber++)
            {
                int index = lineNumber - data.StartLineNumber;
                ILineLayout layout = layouts[index];
                int startColumn = lineNumber == range.StartLineNumber ? range.StartColumn : 1;
                int startOffset = Math.Min(startColumn - 1, layout.Text.Length);
                int endOffset = lineNumber == range.EndLineNumber ? Math.Min(range.EndColumn - 1, layout.Text.Length) : layout.Text.Length;
                double startX = layout.GetCharacterX(startOffset);
                double endX = layout.GetCharacterX(endOffset);
                if (endX <= startX)
                {
                    continue;
                }

                float top = data.RelativeVerticalOffset[index] - topShift + font.LineHeight - thickness;
                surface.FillRectangle(
                    (float)(layoutInfo.ContentLeft + startX - scrollLeft), top, (float)(endX - startX), thickness, Theme.CompositionBorder);
            }
        }
    }
}
