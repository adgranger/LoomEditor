// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelLines.ts (ViewModelLinesFromProjectedModel, wrapping)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

public class ViewModelLinesWrappingTests
{
    private const string Long = "aaaaaaaaaabbbbbbbbbbccccc";

    private static readonly MonospaceLineBreaksComputerFactory Monospace = new("", "");

    private static ViewModelLinesFromProjectedModel Create(
        TextModel model,
        ILineBreaksComputerFactory? factory = null,
        int wrappingColumn = 10,
        WrappingIndent indent = WrappingIndent.None,
        int editorId = 1) =>
        new(editorId, model, factory ?? Monospace, FontInfo.Default, 4, wrappingColumn, indent, WordBreak.Normal, false, false);

    private static List<string> Contents(ViewModelLinesFromProjectedModel lines) =>
        [.. Enumerable.Range(1, lines.GetViewLineCount()).Select(lines.GetViewLineContent)];

    [Fact]
    public void Constructor_WrappingColumn_WrapsLongLines()
    {
        TextModel model = new(Long, options: null);
        using ViewModelLinesFromProjectedModel lines = Create(model);

        Assert.Equal(3, lines.GetViewLineCount());
        Assert.Equal(["aaaaaaaaaa", "bbbbbbbbbb", "ccccc"], Contents(lines));
    }

    [Fact]
    public void Constructor_NoWrapping_HasOneViewLinePerModelLine()
    {
        TextModel model = new(Long + "\nshort", options: null);
        using ViewModelLinesFromProjectedModel lines = Create(model, wrappingColumn: -1);

        Assert.Equal(2, lines.GetViewLineCount());
        Assert.Equal([Long, "short"], Contents(lines));
    }

    [Fact]
    public void SetWrappingSettings_SameSettings_ReturnsFalse()
    {
        TextModel model = new(Long, options: null);
        using ViewModelLinesFromProjectedModel lines = Create(model);

        Assert.False(lines.SetWrappingSettings(FontInfo.Default, 10, WrappingIndent.None, WordBreak.Normal, false));
        Assert.Equal(3, lines.GetViewLineCount());
    }

    [Fact]
    public void SetWrappingSettings_OnlyColumnChanged_PassesThePreviousBreaks()
    {
        TextModel model = new(Long, options: null);
        var factory = new RecordingLineBreaksComputerFactory(Monospace);
        using ViewModelLinesFromProjectedModel lines = Create(model, factory);
        factory.Clear();

        Assert.True(lines.SetWrappingSettings(FontInfo.Default, 5, WrappingIndent.None, WordBreak.Normal, false));

        Assert.NotNull(Assert.Single(factory.PreviousLineBreaks));
        Assert.Equal(5, lines.GetViewLineCount());
    }

    [Fact]
    public void SetWrappingSettings_IndentChanged_DoesNotPassPreviousBreaks()
    {
        TextModel model = new(Long, options: null);
        var factory = new RecordingLineBreaksComputerFactory(Monospace);
        using ViewModelLinesFromProjectedModel lines = Create(model, factory);
        factory.Clear();

        Assert.True(lines.SetWrappingSettings(FontInfo.Default, 10, WrappingIndent.Same, WordBreak.Normal, false));

        Assert.Null(Assert.Single(factory.PreviousLineBreaks));
    }

    [Fact]
    public void SetWrappingSettings_KeepsHiddenAreas()
    {
        TextModel model = new(Long + "\n" + Long, options: null);
        using ViewModelLinesFromProjectedModel lines = Create(model);
        lines.SetHiddenAreas([new Range(1, 1, 1, 1)]);
        Assert.Equal(3, lines.GetViewLineCount());

        lines.SetWrappingSettings(FontInfo.Default, 5, WrappingIndent.None, WordBreak.Normal, false);

        Assert.Equal(5, lines.GetViewLineCount());
        Assert.Single(lines.GetHiddenAreas());
    }

    [Fact]
    public void SetTabSize_RebuildsWithoutPreviousBreaks()
    {
        TextModel model = new(Long, options: null);
        var factory = new RecordingLineBreaksComputerFactory(Monospace);
        using ViewModelLinesFromProjectedModel lines = Create(model, factory);
        factory.Clear();

        Assert.True(lines.SetTabSize(2));

        Assert.Null(Assert.Single(factory.PreviousLineBreaks));
    }

    [Fact]
    public void Constructor_InjectedTextOfAnotherEditor_IsIgnored()
    {
        TextModel model = new("abcdef", options: null);
        var options = ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
        {
            Description = "test",
            After = new InjectedTextOptions { Content = "XY" },
        });
        model.DeltaDecorations(2, [], [new ModelDeltaDecoration(new Range(1, 1, 1, 3), options)]);

        using ViewModelLinesFromProjectedModel editor1 = Create(model, wrappingColumn: 4, editorId: 1);
        using ViewModelLinesFromProjectedModel editor2 = Create(model, wrappingColumn: 4, editorId: 2);

        Assert.Equal(["abcd", "ef"], Contents(editor1));
        Assert.Equal(["abXY", "cdef"], Contents(editor2));
    }

    private sealed class TextContext(string text) : ILineBreaksComputerContext
    {
        public string GetLineContent(int lineNumber) => text;

        public IReadOnlyList<LoomEditor.Core.Model.Events.LineInjectedText>? GetLineInjectedText(int lineNumber) => null;
    }

    private static ModelLineProjectionData? BreakData(string text)
    {
        ILineBreaksComputer computer = Monospace.CreateLineBreaksComputer(
            new TextContext(text), FontInfo.Default, 4, 10, WrappingIndent.None, WordBreak.Normal, false, false);
        computer.AddRequest(1, null);
        return computer.Finalize()[0];
    }

    [Fact]
    public void OnModelLinesInserted_WithLineBreaks_InsertsTheWrappedViewLines()
    {
        TextModel model = new("a\nb", options: null);
        using ViewModelLinesFromProjectedModel lines = Create(model);

        ViewLineRange? range = lines.OnModelLinesInserted(100, 2, 2, [BreakData(Long)]);

        Assert.Equal(new ViewLineRange(2, 4), range);
        Assert.Equal(5, lines.GetViewLineCount());
    }

    [Fact]
    public void OnModelLineChanged_LineNowWraps_ReportsInsertedViewLines()
    {
        TextModel model = new("x\ny", options: null);
        using ViewModelLinesFromProjectedModel lines = Create(model);

        var (mappingChanged, changed, inserted, deleted) = lines.OnModelLineChanged(100, 1, BreakData(Long));

        Assert.True(mappingChanged);
        Assert.Equal(new ViewLineRange(1, 1), changed);
        Assert.Equal(new ViewLineRange(2, 3), inserted);
        Assert.Null(deleted);
    }

    [Fact]
    public void OnModelLineChanged_LineNoLongerWraps_ReportsDeletedViewLines()
    {
        TextModel model = new(Long, options: null);
        using ViewModelLinesFromProjectedModel lines = Create(model);

        var (mappingChanged, changed, inserted, deleted) = lines.OnModelLineChanged(100, 1, null);

        Assert.True(mappingChanged);
        Assert.Equal(new ViewLineRange(1, 1), changed);
        Assert.Null(inserted);
        Assert.Equal(new ViewLineRange(2, 3), deleted);
    }

    [Fact]
    public void OnModelLineChanged_SameCount_ReportsChangedOnly()
    {
        TextModel model = new(Long, options: null);
        using ViewModelLinesFromProjectedModel lines = Create(model);

        var (mappingChanged, changed, inserted, deleted) = lines.OnModelLineChanged(100, 1, BreakData(Long));

        Assert.False(mappingChanged);
        Assert.Equal(new ViewLineRange(1, 3), changed);
        Assert.Null(inserted);
        Assert.Null(deleted);
    }

    private static ViewModelLinesFromProjectedModel CreateWithInjectedText()
    {
        TextModel model = new("abcdef", options: null);
        var options = ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
        {
            Description = "test",
            After = new InjectedTextOptions { Content = "XY" },
        });
        model.DeltaDecorations(1, [], [new ModelDeltaDecoration(new Range(1, 1, 1, 3), options)]);
        return Create(model, wrappingColumn: 4);
    }

    [Fact]
    public void GetInjectedTextAt_OnInjectedText_ReturnsIt()
    {
        using ViewModelLinesFromProjectedModel lines = CreateWithInjectedText();

        Assert.Equal("XY", lines.GetInjectedTextAt(new Position(1, 4))!.Options.Content);
    }

    [Fact]
    public void GetInjectedTextAt_OnText_ReturnsNull()
    {
        using ViewModelLinesFromProjectedModel lines = CreateWithInjectedText();

        Assert.Null(lines.GetInjectedTextAt(new Position(1, 1)));
        Assert.Null(lines.GetInjectedTextAt(new Position(2, 2)));
    }
}
