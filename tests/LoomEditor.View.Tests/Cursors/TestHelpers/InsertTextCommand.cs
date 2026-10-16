using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Tests.Cursors.TestHelpers;

/// <summary>
/// A test-only command that replaces a range with a text and puts the cursor after it. It can also track a selection
/// and return it as the cursor state, to check <c>TrackSelection</c>.
/// </summary>
internal sealed class InsertTextCommand(Range range, string? text, Selection? trackedSelection = null, bool insertsAutoWhitespace = false) : IEditCommand
{
    private string? _trackedId;

    public bool InsertsAutoWhitespace => insertsAutoWhitespace;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        builder.AddEditOperation(range, text);
        if (trackedSelection is { } selection)
        {
            _trackedId = builder.TrackSelection(selection);
        }
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        if (_trackedId is not null)
        {
            return helper.GetTrackedSelection(_trackedId);
        }

        Range inverse = helper.GetInverseEditOperations()[0].Range;
        return Selection.Collapsed(inverse.End);
    }
}

/// <summary>A test-only command that replaces several ranges with "-" and puts the cursor after the first inverse operation.</summary>
internal sealed class MultiOperationCommand(IReadOnlyList<Range> ranges) : IEditCommand
{
    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        foreach (Range range in ranges)
        {
            builder.AddEditOperation(range, "-");
        }
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper) =>
        Selection.Collapsed(helper.GetInverseEditOperations()[0].Range.End);
}

/// <summary>A test-only command whose <c>GetEditOperations</c> throws after adding an operation.</summary>
internal sealed class ThrowingCommand : IEditCommand
{
    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        builder.AddEditOperation(new Range(1, 1, 1, 1), "never");
        throw new InvalidOperationException("The command fails on purpose.");
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper) =>
        throw new InvalidOperationException("Must not be called.");
}

/// <summary>A test-only command that inserts a text, then throws when asked for the cursor state (not caught by the executor).</summary>
internal sealed class ThrowingCursorStateCommand : IEditCommand
{
    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder) =>
        builder.AddEditOperation(new Range(1, 1, 1, 1), "X");

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper) =>
        throw new InvalidOperationException("The command fails on purpose.");
}
