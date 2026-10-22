// Ported from microsoft/vscode: src/vs/editor/test/common/model/modelInjectedText.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

public class ModelInjectedTextTests
{
    private static ModelDeltaDecoration After(string description, string content, Range range) =>
        new(range, ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
        {
            Description = description,
            ShowIfCollapsed = true,
            After = new InjectedTextOptions { Content = content },
        }));

    private static string MapChange(ModelRawChange change) => change switch
    {
        ModelRawLineChanged c => $"lineChanged {c.LineNumber} {c.LineNumberPostEdit}",
        ModelRawLinesInserted c => $"linesInserted {c.FromLineNumber} {c.Count}",
        ModelRawLinesDeleted => "linesDeleted",
        ModelRawEolChanged => "eolChanged",
        ModelRawFlush => "flush",
        _ => "unknown",
    };

    // Monaco: 'Basic'
    [Fact]
    public void Basic()
    {
        TextModel model = TestTextModels.Create("First Line\nSecond Line");
        var recorded = new List<string>();
        model.RawContentChanged += (_, e) => recorded.AddRange(e.Changes.Select(MapChange));
        model.InjectedTextChanged += (_, e) => recorded.AddRange(e.Changes.Select(MapChange));
        List<string> Take()
        {
            List<string> copy = [.. recorded];
            recorded.Clear();
            return copy;
        }

        // Initial decoration
        string[] decorations = model.DeltaDecorations(0, [], [After("test1", "injected1", new Range(1, 1, 1, 1))]);
        Assert.Equal(["lineChanged 1 1"], Take());

        // Decoration change
        model.DeltaDecorations(0, decorations,
        [
            After("test1", "injected1", new Range(2, 1, 2, 1)),
            After("test2", "injected2", new Range(2, 2, 2, 2)),
        ]);
        Assert.Equal(["lineChanged 1 1", "lineChanged 2 2"], Take());

        // Simple Insert
        model.ApplyEdits([TextEdit.Replace(new Range(2, 2, 2, 2), "Hello")]);
        Assert.Equal(["lineChanged 2 2"], Take());

        // Multi-Line Insert
        model.PushEditOperations(null, [TextEdit.Replace(new Range(2, 2, 2, 2), "\n\n\n")], null);
        Assert.Equal(
            [("test1", "[2,1 -> 2,1]"), ("test2", "[2,2 -> 5,6]")],
            model.GetAllDecorations().Select(d => (d.Options.Description, d.Range.ToString())));
        Assert.Equal(["lineChanged 2 2", "linesInserted 3 3"], Take());

        // Multi-Line Replace
        model.PushEditOperations(null, [TextEdit.Replace(new Range(3, 1, 5, 1), "\n\n\n\n\n\n\n\n\n\n\n\n\n")], null);
        Assert.Equal(["lineChanged 5 5", "lineChanged 4 4", "lineChanged 3 3", "linesInserted 6 11"], Take());

        // Multi-Line Replace undo
        Assert.Null(model.Undo());
        Assert.Equal(["lineChanged 2 2", "linesDeleted"], Take());
    }
}
