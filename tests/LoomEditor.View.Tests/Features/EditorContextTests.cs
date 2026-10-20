using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Tests.Features.TestHelpers;

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
    public void SetSelections_EmptyList_Throws()
    {
        var context = new EditorContext();

        Assert.Throws<ArgumentException>(() => context.SetSelections([]));
    }
}
