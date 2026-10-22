using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Cursors.TestHelpers;

/// <summary>Builds a <see cref="CursorContext"/> for a text: the model, its simple model, a converter without hidden lines and a configuration.</summary>
/// <remarks>
/// The <c>FontInfo</c> and the <c>EditorLayoutInfo</c> are built directly with fixed values (8 px per character, 19 px
/// per line, 800 × 600 editor) instead of through <c>FontMeasurements</c> and <c>EditorLayoutInfoComputer</c>: the
/// cursor only reads the line height, the character width and the height.
/// </remarks>
internal static class CursorTestContext
{
    public static CursorContext Create(TextModel model, EditorCursorOptions? options = null)
    {
        var font = new FontInfo(1, "Consolas", "normal", 14, 19, 0, true, 8, 16, true, 8, 8, 8, 8);
        var layout = new EditorLayoutInfo(
            Width: 800,
            Height: 600,
            GlyphMarginLeft: 0,
            GlyphMarginWidth: 0,
            GlyphMarginDecorationLaneCount: 1,
            LineNumbersLeft: 0,
            LineNumbersWidth: 42,
            DecorationsLeft: 42,
            DecorationsWidth: 10,
            ContentLeft: 52,
            ContentWidth: 734,
            ViewportColumn: 90,
            IsViewportWrapping: false,
            WrappingColumn: -1,
            VerticalScrollbarWidth: 14,
            HorizontalScrollbarHeight: 12,
            OverviewRuler: new OverviewRulerPosition(14, 600, 0, 0));
        var config = new CursorConfiguration("plaintext", model.Options, options ?? EditorCursorOptions.Default, font, layout, new LanguageConfigurationRegistry());
        return new CursorContext(model, new TextModelCursorSimpleModel(model), new ViewModelLinesFromProjectedModel(0, model, model.Options.TabSize).CreateCoordinatesConverter(), config);
    }

    public static CursorContext Create(string text, EditorCursorOptions? options = null) => Create(new TextModel(text), options);
}
