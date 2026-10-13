using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Undo;

namespace LoomEditor.Core.Tests.Undo;

// Monaco has no test of its own for the undo/redo service of one model (editStack.test.ts only tests binary
// serialization, which is not ported); these are LoomEditor additions.
public class UndoRedoStackTests
{
    private static SingleModelEditStackElement NewElement(TextModel model) => new(model, null);

    [Fact]
    public void GetLastElement_Empty_ReturnsNull()
    {
        var stack = new UndoRedoStack();

        Assert.Null(stack.GetLastElement());
        Assert.False(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Push_Element_BecomesLastElement()
    {
        var model = TestTextModels.Create("a");
        var stack = new UndoRedoStack();
        SingleModelEditStackElement element = NewElement(model);

        stack.Push(element);

        Assert.Same(element, stack.GetLastElement());
        Assert.True(stack.CanUndo);
    }

    [Fact]
    public void Undo_LastElement_ClosesItAndMovesItToRedo()
    {
        var model = TestTextModels.Create("a");
        var stack = new UndoRedoStack();
        SingleModelEditStackElement element = NewElement(model);
        stack.Push(element);

        SingleModelEditStackElement? undone = stack.Undo();

        Assert.Same(element, undone);
        Assert.False(element.IsOpen);
        Assert.False(stack.CanUndo);
        Assert.True(stack.CanRedo);
        Assert.Null(stack.GetLastElement());
    }

    [Fact]
    public void Redo_AfterUndo_MovesElementBackToUndo()
    {
        var model = TestTextModels.Create("a");
        var stack = new UndoRedoStack();
        SingleModelEditStackElement element = NewElement(model);
        stack.Push(element);
        stack.Undo();

        SingleModelEditStackElement? redone = stack.Redo();

        Assert.Same(element, redone);
        Assert.True(stack.CanUndo);
        Assert.False(stack.CanRedo);
        Assert.Same(element, stack.GetLastElement());
    }

    [Fact]
    public void UndoAndRedo_EmptyLists_ReturnNull()
    {
        var stack = new UndoRedoStack();

        Assert.Null(stack.Undo());
        Assert.Null(stack.Redo());
    }

    [Fact]
    public void Push_AfterUndo_ClearsRedo()
    {
        var model = TestTextModels.Create("a");
        var stack = new UndoRedoStack();
        stack.Push(NewElement(model));
        stack.Undo();

        stack.Push(NewElement(model));

        Assert.False(stack.CanRedo);
        Assert.True(stack.CanUndo);
    }

    [Fact]
    public void Clear_NonEmptyStack_EmptiesBothLists()
    {
        var model = TestTextModels.Create("a");
        var stack = new UndoRedoStack();
        stack.Push(NewElement(model));
        stack.Push(NewElement(model));
        stack.Undo();

        stack.Clear();

        Assert.False(stack.CanUndo);
        Assert.False(stack.CanRedo);
        Assert.Null(stack.GetLastElement());
    }

    [Fact]
    public void Undo_TwoElements_ReturnsMostRecentFirst()
    {
        var model = TestTextModels.Create("a");
        var stack = new UndoRedoStack();
        SingleModelEditStackElement first = NewElement(model);
        SingleModelEditStackElement second = NewElement(model);
        stack.Push(first);
        stack.Push(second);

        Assert.Same(second, stack.Undo());
        Assert.Same(first, stack.Undo());
    }
}
