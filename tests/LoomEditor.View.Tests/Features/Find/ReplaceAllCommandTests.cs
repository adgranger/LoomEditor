using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Tests.Features.Find.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Find;

public class ReplaceAllCommandTests
{
    private sealed class RecordingBuilder : IEditOperationBuilder
    {
        public List<(Range Range, string? Text)> Operations { get; } = [];

        public void AddEditOperation(Range range, string? text, bool forceMoveMarkers = false) => Operations.Add((range, text));

        public void AddTrackedEditOperation(Range range, string? text, bool forceMoveMarkers = false) => Operations.Add((range, text));

        public string TrackSelection(Selection selection, bool? trackPreviousOnEmpty = null) => "tracked";
    }

    [Fact]
    public void ReplaceAllCommand_TouchingRanges_AreMergedIntoOneEdit()
    {
        var model = new TextModel("abcdef");
        var builder = new RecordingBuilder();
        var command = new ReplaceAllCommand(
            new Selection(1, 1, 1, 1),
            [new Range(1, 5, 1, 7), new Range(1, 1, 1, 3), new Range(1, 3, 1, 5)],
            ["a", "b", "c"]);

        command.GetEditOperations(model, builder);

        var operation = Assert.Single(builder.Operations);
        Assert.Equal(new Range(1, 1, 1, 7), operation.Range);
        Assert.Equal("bca", operation.Text);
    }

    [Fact]
    public void ReplaceAllCommand_SeparatedRanges_StaySeparate()
    {
        var builder = new RecordingBuilder();
        var command = new ReplaceAllCommand(new Selection(1, 1, 1, 1), [new Range(1, 5, 1, 6), new Range(1, 1, 1, 2)], ["x", "y"]);

        command.GetEditOperations(new TextModel("abcdef"), builder);

        Assert.Equal([(new Range(1, 1, 1, 2), "y"), (new Range(1, 5, 1, 6), "x")], builder.Operations);
    }

    [Fact]
    public void ReplaceAll_IsOneUndoStep() => FindModelTestHelpers.FindTest(false, (editor, scheduler) =>
    {
        var original = editor.Model.GetValue();
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", ReplaceString = "hi" }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        findModel.ReplaceAll();
        Assert.NotEqual(original, editor.Model.GetValue());

        editor.Model.Undo();

        Assert.Equal(original, editor.Model.GetValue());
    });

    [Fact]
    public void LargeReplaceAll_EmptyMatches_StepOverSurrogatePairs() => TestCodeEditor.WithTestCodeEditor(
        new string('a', 19999) + char.ConvertFromUtf32(0x1F4BB),
        editor =>
        {
            var findState = new FindReplaceState();
            findState.Change(new NewFindReplaceState { SearchString = "()", ReplaceString = "-", IsRegex = true }, false);
            var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, new Features.TestHelpers.ManualEditorScheduler());
            Assert.True(findState.MatchesCount >= FindConstants.MatchesLimit);

            findModel.ReplaceAll();

            // a match before every character and at the end, none inside the pair
            var expected = string.Concat(Enumerable.Repeat("-a", 19999)) + "-" + char.ConvertFromUtf32(0x1F4BB) + "-";
            Assert.Equal(expected, editor.Model.GetValue());
        });
}
