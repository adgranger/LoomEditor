// Ported from microsoft/vscode: src/vs/editor/test/browser/testCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Cursors.TestHelpers;

/// <summary>Helpers to test an <see cref="IEditCommand"/> (Monaco: <c>testCommand</c> and <c>getEditOperation</c>).</summary>
/// <remarks>
/// <c>TestCommand</c> runs the command through <see cref="CommandExecutor"/> directly. Monaco runs it through a
/// view model; with one cursor and one command the result is the same.
/// </remarks>
internal static class CommandTester
{
    /// <summary>Runs <paramref name="commandFactory"/>'s command on a model without language.</summary>
    public static void TestCommand(
        string[] lines,
        Selection selection,
        Func<Selection, IEditCommand> commandFactory,
        string[] expectedLines,
        Selection expectedSelection,
        TextModelOptions? options = null) =>
        TestCommand(lines, selection, (_, sel) => commandFactory(sel), expectedLines, expectedSelection, options);

    /// <summary>
    /// Runs <paramref name="commandFactory"/>'s command on a model made of <paramref name="lines"/> and checks the lines and the
    /// selection (Monaco: <c>testCommand</c>; the services are its <c>accessor</c>, <paramref name="prepare"/> registers the languages).
    /// </summary>
    public static void TestCommand(
        string[] lines,
        Selection selection,
        Func<TestEditorServices, Selection, IEditCommand> commandFactory,
        string[] expectedLines,
        Selection expectedSelection,
        TextModelOptions? options = null,
        string? languageId = null,
        Action<TestEditorServices>? prepare = null)
    {
        using var services = new TestEditorServices();
        prepare?.Invoke(services);
        TextModel model = services.CreateTextModel(string.Join('\n', lines), languageId, options);

        IEditCommand command = commandFactory(services, selection);
        IReadOnlyList<Selection>? result = CommandExecutor.ExecuteCommands(model, [selection], [command]);

        Assert.Equal(expectedLines, model.GetLinesContent());

        // No edit operations: the cursor keeps its selection (Monaco reads it back with viewModel.getSelection()).
        Assert.Equal(expectedSelection, result is null ? selection : Assert.Single(result));
    }

    /// <summary>The edit operations that <paramref name="command"/> would add if it were executed on <paramref name="model"/>.</summary>
    public static List<(Range Range, string? Text, bool ForceMoveMarkers)> GetEditOperation(TextModel model, IEditCommand command)
    {
        var builder = new CollectingBuilder();
        command.GetEditOperations(model, builder);
        return builder.Operations;
    }

    private sealed class CollectingBuilder : IEditOperationBuilder
    {
        public List<(Range Range, string? Text, bool ForceMoveMarkers)> Operations { get; } = [];

        public void AddEditOperation(Range range, string? text, bool forceMoveMarkers = false) =>
            Operations.Add((range, text, forceMoveMarkers));

        public void AddTrackedEditOperation(Range range, string? text, bool forceMoveMarkers = false) =>
            Operations.Add((range, text, forceMoveMarkers));

        public string TrackSelection(Selection selection, bool? trackPreviousOnEmpty = null) => string.Empty;
    }
}
