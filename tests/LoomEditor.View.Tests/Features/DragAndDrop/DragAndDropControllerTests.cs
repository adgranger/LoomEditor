using LoomEditor.Core.Text;
using LoomEditor.View.Controller;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features;
using LoomEditor.View.Features.DragAndDrop;

namespace LoomEditor.View.Tests.Features.DragAndDrop;

// Monaco has no test for dnd.ts: all tests are new. The view events are raised by hand, as the mouse operation does.
public class DragAndDropControllerTests
{
    private const string Text = "0123456789\nabcdefghij\nABCDEFGHIJ";

    private static EditorContext Create(out EditorFeatureHost host, EditorCursorOptions? options = null)
    {
        var context = new EditorContext();
        context.SetText(Text);
        if (options is not null)
        {
            context.ViewModel.SetCursorOptions(options);
        }

        host = new EditorFeatureHost(context, [() => new DragAndDropFeature()]);
        context.SetSelections([new Selection(1, 2, 1, 5)]);
        return context;
    }

    private static EditorMouseEvent Mouse(bool ctrl = false, bool shift = false) =>
        new(0, 0, true, false, false, ctrl, shift, false, 1, 0);

    private static MouseTarget Target(int line, int column, MouseTargetType type = MouseTargetType.ContentText) =>
        new(type, new Position(line, column), column, new Range(line, column, line, column), false, null);

    private static void Press(EditorContext context) =>
        context.UserInputEvents.EmitMouseDown(new EditorMouseEventArgs(Mouse(), Target(1, 3)));

    // The press, then the first drag event inside the selection: it becomes the drag selection.
    private static void StartDrag(EditorContext context)
    {
        Press(context);
        context.UserInputEvents.EmitMouseDrag(new EditorMouseEventArgs(Mouse(), Target(1, 3)));
    }

    private static void Drag(EditorContext context, int line, int column, bool ctrl = false) =>
        context.UserInputEvents.EmitMouseDrag(new EditorMouseEventArgs(Mouse(ctrl), Target(line, column)));

    private static void Drop(EditorContext context, MouseTarget? target, bool ctrl = false, bool shift = false) =>
        context.UserInputEvents.EmitMouseDrop(new EditorPartialMouseEventArgs(Mouse(ctrl, shift), target));

    private static int TargetDecorations(EditorContext context)
    {
        int count = 0;
        for (int line = 1; line <= 3; line++)
        {
            count += context.GetLineDecorations(line).Count(decoration => decoration.Options.ClassName == "dnd-target");
        }

        return count;
    }

    [Fact]
    public void Drag_ShowsDecorationAtTarget_RemovedWhenInsideSelection()
    {
        using EditorContext context = Create(out EditorFeatureHost host);
        using (host)
        {
            StartDrag(context);
            Drag(context, 2, 4);
            Assert.Equal(1, TargetDecorations(context));

            Drag(context, 1, 3);

            Assert.Equal(0, TargetDecorations(context));
        }
    }

    [Fact]
    public void Drop_OutsideSelection_MovesText_OneUndoStep()
    {
        using EditorContext context = Create(out EditorFeatureHost host);
        using (host)
        {
            StartDrag(context);
            Drag(context, 2, 4);
            Drop(context, Target(2, 4));

            Assert.Equal("0456789\nabc123defghij\nABCDEFGHIJ", context.Model.GetValue());
            Assert.Equal(new Selection(2, 4, 2, 7), context.ViewModel.GetSelection());
            Assert.Equal(0, TargetDecorations(context));

            context.Model.Undo();

            Assert.Equal(Text, context.Model.GetValue());
        }
    }

    [Fact]
    public void Drop_WithCtrl_CopiesText()
    {
        using EditorContext context = Create(out EditorFeatureHost host);
        using (host)
        {
            StartDrag(context);
            Drag(context, 2, 4, ctrl: true);
            Drop(context, Target(2, 4), ctrl: true);

            Assert.Equal("0123456789\nabc123defghij\nABCDEFGHIJ", context.Model.GetValue());
        }
    }

    [Fact]
    public void Drop_InsideSelection_DoesNothing()
    {
        using EditorContext context = Create(out EditorFeatureHost host);
        using (host)
        {
            StartDrag(context);
            Drag(context, 1, 4);
            Drop(context, Target(1, 4));

            Assert.Equal(Text, context.Model.GetValue());
            Assert.Equal(new Selection(1, 2, 1, 5), context.ViewModel.GetSelection());
        }
    }

    [Fact]
    public void Drop_InsideSelection_CollapsesToDropPosition()
    {
        using EditorContext context = Create(out EditorFeatureHost host);
        using (host)
        {
            // A click without drag: the drop arrives with no drag selection.
            Press(context);
            Drop(context, Target(1, 3));

            Assert.Equal(Text, context.Model.GetValue());
            Assert.Equal(new Selection(1, 3, 1, 3), context.ViewModel.GetSelection());
        }
    }

    [Fact]
    public void Drop_Shift_ExtendsPrimarySelection()
    {
        using EditorContext context = Create(out EditorFeatureHost host);
        using (host)
        {
            Press(context);
            Drop(context, Target(2, 3), shift: true);

            Assert.Equal(new Selection(1, 2, 2, 3), context.ViewModel.GetSelection());
        }
    }

    [Fact]
    public void Drop_OnGutter_Works()
    {
        using EditorContext context = Create(out EditorFeatureHost host);
        using (host)
        {
            StartDrag(context);
            Drag(context, 2, 1);
            Drop(context, Target(2, 1, MouseTargetType.GutterLineNumbers));

            Assert.Equal("0456789\n123abcdefghij\nABCDEFGHIJ", context.Model.GetValue());
        }
    }

    [Fact]
    public void Drop_OnScrollBarOrOutside_DoesNothing()
    {
        using EditorContext context = Create(out EditorFeatureHost host);
        using (host)
        {
            StartDrag(context);
            Drag(context, 2, 4);
            Drop(context, Target(2, 4, MouseTargetType.OutsideEditor));
            Assert.Equal(Text, context.Model.GetValue());

            StartDrag(context);
            Drag(context, 2, 4);
            Drop(context, null);
            Assert.Equal(Text, context.Model.GetValue());
            Assert.Equal(0, TargetDecorations(context));
        }
    }

    [Fact]
    public void Escape_Cancels_NoEdit()
    {
        using EditorContext context = Create(out EditorFeatureHost host);
        using (host)
        {
            StartDrag(context);
            Drag(context, 2, 4);

            context.UserInputEvents.EmitMouseDropCanceled();

            Assert.Equal(Text, context.Model.GetValue());
            Assert.Equal(0, TargetDecorations(context));
            Assert.Equal(EditorMouseStyle.Text, context.MouseStyle);
        }
    }

    [Fact]
    public void Blur_ResetsState()
    {
        using EditorContext context = Create(out EditorFeatureHost host);
        using (host)
        {
            context.ViewModel.SetHasFocus(true);
            StartDrag(context);
            Drag(context, 2, 4);

            context.ViewModel.SetHasFocus(false);

            Assert.Equal(0, TargetDecorations(context));
            // The drag selection is forgotten: a drop is now a plain click in the selection.
            Drop(context, Target(1, 3));
            Assert.Equal(Text, context.Model.GetValue());
        }
    }

    [Fact]
    public void Ctrl_WhileDragging_SetsCopyMouseStyle()
    {
        using EditorContext context = Create(out EditorFeatureHost host);
        using (host)
        {
            StartDrag(context);
            Drag(context, 2, 4);
            Assert.Equal(EditorMouseStyle.Default, context.MouseStyle);

            context.UserInputEvents.EmitKeyDown(new EditorKeyEventArgs(true, true));
            Assert.Equal(EditorMouseStyle.Copy, context.MouseStyle);

            context.UserInputEvents.EmitKeyUp(new EditorKeyEventArgs(false, true));
            Assert.Equal(EditorMouseStyle.Default, context.MouseStyle);
        }
    }

    [Fact]
    public void Release_ResetsMouseStyleToText()
    {
        using EditorContext context = Create(out EditorFeatureHost host);
        using (host)
        {
            StartDrag(context);
            Drag(context, 2, 4, ctrl: true);
            Assert.Equal(EditorMouseStyle.Copy, context.MouseStyle);

            context.UserInputEvents.EmitMouseUp(new EditorMouseEventArgs(Mouse(), Target(2, 4)));

            Assert.Equal(EditorMouseStyle.Text, context.MouseStyle);
        }
    }

    [Fact]
    public void DragAndDropOff_NoDecorationNoEdit()
    {
        // The mouse operation never starts a drag with the option off, so no drag events arrive: only the key handlers are gated.
        using EditorContext context = Create(out EditorFeatureHost host, new EditorCursorOptions { DragAndDrop = false });
        using (host)
        {
            Press(context);
            context.UserInputEvents.EmitKeyDown(new EditorKeyEventArgs(true, true));

            Assert.Equal(EditorMouseStyle.Text, context.MouseStyle);
            Assert.Equal(0, TargetDecorations(context));
            Assert.Equal(Text, context.Model.GetValue());
        }
    }

    [Fact]
    public void ColumnSelection_KeyHandlersIgnored()
    {
        using EditorContext context = Create(out EditorFeatureHost host, new EditorCursorOptions { ColumnSelection = true });
        using (host)
        {
            Press(context);
            context.UserInputEvents.EmitKeyDown(new EditorKeyEventArgs(true, true));

            Assert.Equal(EditorMouseStyle.Text, context.MouseStyle);
        }
    }
}
