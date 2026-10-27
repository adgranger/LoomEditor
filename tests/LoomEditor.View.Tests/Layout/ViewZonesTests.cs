using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

public class ViewZonesTests
{
    private const int LineHeight = 19;
    private const string Text = "aaaa bbbb cccc\nline2\nline3\nline4\nline5";

    private static readonly TestCodeEditorOptions WrapOptions =
        new(Wrapping: new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 10 });

    private static void With(Action<ViewModel> test, TestCodeEditorOptions? options = null) =>
        TestCodeEditor.WithTestCodeEditor(Text, editor => test(editor.ViewModel), options);

    private static string Add(ViewModel vm, ViewZone zone)
    {
        string id = "";
        vm.ViewZones.Change(a => id = a.AddZone(zone));
        return id;
    }

    private static IEditorWhitespace Whitespace(ViewModel vm, string id) => vm.ViewLayout.GetWhitespaces().Single(w => w.Id == id);

    [Fact]
    public void AddZone_InsertsWhitespaceWithHeight() => With(vm =>
    {
        string id = Add(vm, new ViewZone { AfterLineNumber = 2, HeightInPx = 50 });

        IEditorWhitespace w = Whitespace(vm, id);
        Assert.Equal(2, w.AfterLineNumber);
        Assert.Equal(50, w.Height);
        Assert.Equal(id, vm.ViewZones.GetZone(id)!.Id);
    });

    [Fact]
    public void AddZone_HeightInLines_UsesLineHeight() => With(vm =>
    {
        string id = Add(vm, new ViewZone { AfterLineNumber = 2, HeightInLines = 3 });
        string defaultId = Add(vm, new ViewZone { AfterLineNumber = 3 });

        Assert.Equal(3 * LineHeight, Whitespace(vm, id).Height);
        Assert.Equal(LineHeight, Whitespace(vm, defaultId).Height);
    });

    [Fact]
    public void AddZone_AfterLineZero_AtTop() => With(vm =>
    {
        string id = Add(vm, new ViewZone { AfterLineNumber = 0, HeightInPx = 40 });

        Assert.Equal(0, Whitespace(vm, id).AfterLineNumber);
        Assert.Equal(40, vm.ViewLayout.GetVerticalOffsetForLineNumber(1));
    });

    [Fact]
    public void AddZone_Ordinal_OrdersZonesAfterTheSameLine() => With(vm =>
    {
        string second = Add(vm, new ViewZone { AfterLineNumber = 2, HeightInPx = 10, Ordinal = 20 });
        string first = Add(vm, new ViewZone { AfterLineNumber = 2, HeightInPx = 10, Ordinal = 5 });
        string[] order = vm.ViewLayout.GetWhitespaces().Select(w => w.Id).ToArray();

        Assert.Equal([first, second], order);
    });

    [Fact]
    public void RemoveZone_RemovesWhitespace() => With(vm =>
    {
        string id = Add(vm, new ViewZone { AfterLineNumber = 2, HeightInPx = 50 });

        Assert.True(vm.ViewZones.Change(a => a.RemoveZone(id)));

        Assert.Empty(vm.ViewLayout.GetWhitespaces());
        Assert.Null(vm.ViewZones.GetZone(id));
        Assert.False(vm.ViewZones.Change(a => a.RemoveZone(id)));
    });

    [Fact]
    public void LayoutZone_AfterHeightChange_UpdatesLayout() => With(vm =>
    {
        var zone = new ViewZone { AfterLineNumber = 2, HeightInPx = 50 };
        string id = Add(vm, zone);

        zone.HeightInPx = 80;
        zone.AfterLineNumber = 3;
        vm.ViewZones.Change(a => a.LayoutZone(id));

        Assert.Equal(80, Whitespace(vm, id).Height);
        Assert.Equal(3, Whitespace(vm, id).AfterLineNumber);
    });

    [Fact]
    public void Change_Accessor_InvalidAfterCallback_Throws() => With(vm =>
    {
        IViewZoneChangeAccessor? kept = null;
        vm.ViewZones.Change(a => kept = a);

        Assert.Throws<InvalidOperationException>(() => kept!.AddZone(new ViewZone()));
        Assert.Throws<InvalidOperationException>(() => kept!.RemoveZone("x"));
        Assert.Throws<InvalidOperationException>(() => kept!.LayoutZone("x"));
    });

    [Fact]
    public void Change_CallbackThrows_LayoutStillConsistent() => With(vm =>
    {
        IViewZoneChangeAccessor? kept = null;
        Assert.Throws<ArgumentException>(() => vm.ViewZones.Change(a =>
        {
            kept = a;
            a.AddZone(new ViewZone { AfterLineNumber = 1, HeightInPx = 30 });
            throw new ArgumentException("boom");
        }));

        Assert.Throws<InvalidOperationException>(() => kept!.AddZone(new ViewZone()));
        string id = Add(vm, new ViewZone { AfterLineNumber = 2, HeightInPx = 20 });
        Assert.Equal(2, vm.ViewLayout.GetWhitespaces().Count);
        Assert.Equal(20, Whitespace(vm, id).Height);
    });

    [Fact]
    public void HiddenArea_ZoneHasZeroHeight_UnlessShowInHiddenAreas() => With(vm =>
    {
        vm.SetHiddenAreas([new Range(2, 1, 3, 1)]);

        string hidden = Add(vm, new ViewZone { AfterLineNumber = 2, HeightInPx = 50 });
        string shown = Add(vm, new ViewZone { AfterLineNumber = 2, HeightInPx = 50, ShowInHiddenAreas = true });

        Assert.Equal(0, Whitespace(vm, hidden).Height);
        Assert.Equal(50, Whitespace(vm, shown).Height);
    });

    [Fact]
    public void HiddenArea_Changes_ZoneRecomputed() => With(vm =>
    {
        string id = Add(vm, new ViewZone { AfterLineNumber = 2, HeightInPx = 50 });
        Assert.Equal(50, Whitespace(vm, id).Height);

        vm.SetHiddenAreas([new Range(2, 1, 3, 1)]);
        Assert.Equal(0, Whitespace(vm, id).Height);

        vm.SetHiddenAreas([]);
        Assert.Equal(50, Whitespace(vm, id).Height);
    });

    // A zone after a wrapped model line is anchored after its last view line.
    [Fact]
    public void WordWrap_LineMappingChange_ZoneMovesToWrappedLine() => With(vm =>
    {
        Assert.Equal(6, vm.GetLineCount());
        string id = Add(vm, new ViewZone { AfterLineNumber = 1, HeightInPx = 50 });
        Assert.Equal(2, Whitespace(vm, id).AfterLineNumber);

        vm.SetOptions(new EditorLayoutOptions(), vm.ViewLayoutOptions);

        Assert.Equal(1, Whitespace(vm, id).AfterLineNumber);
    }, WrapOptions);

    [Fact]
    public void OnComputedHeight_CalledWithHeight() => With(vm =>
    {
        var heights = new List<int>();
        var zone = new ViewZone { AfterLineNumber = 2, HeightInPx = 50, OnComputedHeight = heights.Add };
        string id = Add(vm, zone);

        zone.HeightInPx = 70;
        vm.ViewZones.Change(a => a.LayoutZone(id));
        vm.SetHiddenAreas([new Range(2, 1, 3, 1)]);

        Assert.Equal([50, 70, 0], heights);
    });

    [Fact]
    public void ZonesChanged_RaisedOnceForABatch() => With(vm =>
    {
        int raised = 0;
        vm.ViewZones.ZonesChanged += (_, _) => raised++;

        vm.ViewZones.Change(a =>
        {
            a.AddZone(new ViewZone { AfterLineNumber = 1 });
            a.AddZone(new ViewZone { AfterLineNumber = 2 });
        });
        Assert.Equal(1, raised);

        vm.ViewZones.Change(_ => { });
        Assert.Equal(1, raised);
    });

    [Fact]
    public void LineHeightChange_RecomputesHeightInLinesZones() => With(vm =>
    {
        string lines = Add(vm, new ViewZone { AfterLineNumber = 2, HeightInLines = 2 });
        string px = Add(vm, new ViewZone { AfterLineNumber = 2, HeightInPx = 7 });

        vm.SetFontInfo(vm.FontInfo with { LineHeight = 30 });
        vm.SetOptions(new EditorLayoutOptions(), new ViewLayoutOptions(30));

        Assert.Equal(60, Whitespace(vm, lines).Height);
        Assert.Equal(7, Whitespace(vm, px).Height);
    });

    [Fact]
    public void ShouldSuppressMouseDown_ZoneWithFlag_True_UnknownId_False() => With(vm =>
    {
        string flagged = Add(vm, new ViewZone { AfterLineNumber = 1, SuppressMouseDown = true });
        string plain = Add(vm, new ViewZone { AfterLineNumber = 1 });

        Assert.True(vm.ViewZones.ShouldSuppressMouseDown(flagged));
        Assert.False(vm.ViewZones.ShouldSuppressMouseDown(plain));
        Assert.False(vm.ViewZones.ShouldSuppressMouseDown("unknown"));
    });
}
