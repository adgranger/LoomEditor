using LoomEditor.Core.Markers;
using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.Highlighting.TestHelpers;

namespace LoomEditor.View.Tests.Features;

// The selections live in the cursor: all tests are new.
public class EditorContextTests
{
    private static Selection Caret(int line, int column) => Selection.Collapsed(new Position(line, column));

    [Fact]
    public void Selection_NewContext_IsAtTheStart()
    {
        var context = new EditorContext();

        Assert.Equal(Caret(1, 1), context.Selection);
    }

    [Fact]
    public void SetSelections_TwoSelections_AreReturnedPrimaryFirstAndRaiseOneEvent()
    {
        var context = new EditorContext();
        context.SetText("abcdef");
        int raised = 0;
        context.SelectionChanged += (_, _) => raised++;

        context.SetSelections([Caret(1, 2), Caret(1, 5)]);

        Assert.Equal([Caret(1, 2), Caret(1, 5)], context.Selections);
        Assert.Equal(Caret(1, 2), context.Selection);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void SetText_AfterASelectionOnLineTwo_ResetsToTheStartAndRaisesSelectionChanged()
    {
        var context = new EditorContext();
        context.SetText("x\ny");
        context.Selection = Caret(2, 2);
        int raised = 0;
        context.SelectionChanged += (_, _) => raised++;

        context.SetText("new\ntext");

        Assert.Equal(Caret(1, 1), context.Selection);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Scheduler_PublicConstructor_NeverRunsActions()
    {
        var context = new EditorContext();
        int runs = 0;

        context.Scheduler.Schedule(TimeSpan.Zero, () => runs++);

        Assert.Equal(0, runs);
    }

    [Fact]
    public void Scheduler_InternalConstructor_IsTheGivenScheduler()
    {
        var scheduler = new ManualEditorScheduler();
        var context = new EditorContext(scheduler);

        Assert.Same(scheduler, context.Scheduler);
    }

    [Fact]
    public void Id_TwoContexts_AreDifferentAndNotZero()
    {
        var first = new EditorContext();
        var second = new EditorContext();

        Assert.NotEqual(0, first.Id);
        Assert.NotEqual(0, second.Id);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.Id, first.ViewModel.EditorId);
    }

    [Fact]
    public void DeltaDecorations_BothEmpty_ReturnsTheOldIds()
    {
        var context = new EditorContext();
        IReadOnlyList<string> oldIds = [];

        Assert.Same(oldIds, context.DeltaDecorations(oldIds, []));
    }

    [Fact]
    public void SetSelections_EmptyList_Throws()
    {
        var context = new EditorContext();

        Assert.Throws<ArgumentException>(() => context.SetSelections([]));
    }

    [Fact]
    public void SetText_TabIndentedText_UsesTabs()
    {
        var context = new EditorContext();

        context.SetText("a {\n\tb;\n\tc;\n\td;\n}\n");

        Assert.False(context.Model.Options.InsertSpaces);
    }

    [Fact]
    public void SetText_TwoSpaceIndentedText_TabSizeTwo()
    {
        var context = new EditorContext();

        context.SetText("a {\n  b;\n  c;\n  d;\n}\n");

        Assert.True(context.Model.Options.InsertSpaces);
        Assert.Equal(2, context.Model.Options.TabSize);
    }

    [Fact]
    public void SetText_DetectIndentationOff_KeepsTheOptions()
    {
        var context = new EditorContext { DetectIndentation = false };

        context.SetText("a {\n\tb;\n\tc;\n\td;\n}\n");

        Assert.True(context.Model.Options.InsertSpaces);
        Assert.Equal(4, context.Model.Options.TabSize);
    }

    private static int MarkerCount(EditorContext context) =>
        context.Model.GetAllDecorations(ownerId: context.Id).Count(d => d.Options.Description == "marker-decoration");

    [Fact]
    public void Markers_ProduceDecorations_OnTheModel()
    {
        var context = new EditorContext();
        context.SetText("let value = 1");

        context.Markers.Change("test", [new MarkerData(MarkerSeverity.Error, "x", 1, 1, 1, 4)]);

        Assert.Equal(1, MarkerCount(context));
    }

    [Fact]
    public void Markers_AreRemovedOnDispose()
    {
        var context = new EditorContext();
        context.SetText("let value = 1");
        context.Markers.Change("test", [new MarkerData(MarkerSeverity.Error, "x", 1, 1, 1, 4)]);

        context.Dispose();

        Assert.Equal(0, MarkerCount(context));
    }

    [Fact]
    public void DocumentUri_Default_UsesTheEditorId()
    {
        var context = new EditorContext();

        Assert.Equal("inmemory://model/" + context.Id, context.DocumentUri);
        Assert.Equal(context.DocumentUri, context.GetDocumentUri());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DocumentUri_NullOrEmpty_Throws(string? value)
    {
        var context = new EditorContext();

        Assert.ThrowsAny<ArgumentException>(() => context.DocumentUri = value!);
    }

    [Fact]
    public void LanguageFeatures_IsNotNull_AndPerEditor()
    {
        var first = new EditorContext();
        var second = new EditorContext();

        Assert.NotNull(first.LanguageFeatures);
        Assert.NotSame(first.LanguageFeatures, second.LanguageFeatures);
    }

    [Fact]
    public void UiDispatcher_Default_IsInline()
    {
        Assert.Same(InlineUiDispatcher.Instance, new EditorContext().GetUiDispatcher());
    }

    [Fact]
    public void UiDispatcher_Given_IsUsed()
    {
        var dispatcher = new ManualUiDispatcher();
        var context = new EditorContext(_ => new NoopDisposable(), TimeProvider.System, dispatcher);

        Assert.Same(dispatcher, context.GetUiDispatcher());
    }

    [Fact]
    public void GetLanguageId_ReturnsTheModelLanguage()
    {
        var context = new EditorContext();
        Assert.Equal("plaintext", context.GetLanguageId());

        context.Model.Tokenization.SetLanguageId("csharp");

        Assert.Equal("csharp", context.GetLanguageId());
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    [Fact]
    public void SetText_NoIndentation_KeepsTheDefaults()
    {
        var context = new EditorContext();
        context.SetText("a {\n  b;\n  c;\n  d;\n}\n");

        context.SetText("no\nindentation\n");

        Assert.True(context.Model.Options.InsertSpaces);
        Assert.Equal(4, context.Model.Options.TabSize);
    }
}
