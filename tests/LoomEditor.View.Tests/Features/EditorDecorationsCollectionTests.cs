using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;
using LoomEditor.View.Features;

namespace LoomEditor.View.Tests.Features;

// No Monaco test covers the collection: all tests are new.
public class EditorDecorationsCollectionTests
{
    private static readonly ModelDecorationOptions Options =
        ModelDecorationOptions.Register(new ModelDecorationOptionsData { Description = "test", ClassName = "test" });

    private static ModelDeltaDecoration Decoration(int line, int startColumn, int endColumn) =>
        new(new Range(line, startColumn, line, endColumn), Options);

    private readonly EditorContext _editor = CreateEditor();

    // The cursor keeps its selection as a tracked range, which is a decoration of owner 0 (as in Monaco).
    private static List<ModelDecoration> OwnedDecorations(IEnumerable<ModelDecoration> decorations) => [.. decorations.Where(d => d.OwnerId != 0)];

    private static EditorContext CreateEditor()
    {
        var editor = new EditorContext();
        editor.SetText("hello world\nsecond line");
        return editor;
    }

    [Fact]
    public void Set_ReplacesThePreviousDecorations()
    {
        EditorDecorationsCollection collection = _editor.CreateDecorationsCollection();
        collection.Set([Decoration(1, 1, 3), Decoration(1, 5, 7)]);

        collection.Set([Decoration(2, 1, 4)]);

        Assert.Equal(1, collection.Count);
        Assert.Equal([new Range(2, 1, 2, 4)], collection.GetRanges());
        Assert.Single(OwnedDecorations(_editor.Model.GetAllDecorations()));
    }

    [Fact]
    public void Append_KeepsThePreviousDecorations()
    {
        EditorDecorationsCollection collection = _editor.CreateDecorationsCollection([Decoration(1, 1, 3)]);

        string[] appended = collection.Append([Decoration(2, 1, 4)]);

        Assert.Single(appended);
        Assert.Equal(2, collection.Count);
        Assert.Equal([new Range(1, 1, 1, 3), new Range(2, 1, 2, 4)], collection.GetRanges());
    }

    [Fact]
    public void Clear_RemovesEverything()
    {
        EditorDecorationsCollection collection = _editor.CreateDecorationsCollection([Decoration(1, 1, 3), Decoration(2, 1, 4)]);

        collection.Clear();

        Assert.Equal(0, collection.Count);
        Assert.Empty(collection.GetRanges());
        Assert.Empty(OwnedDecorations(_editor.Model.GetAllDecorations()));
    }

    [Fact]
    public void GetRange_IndexOutOfRange_IsNull()
    {
        EditorDecorationsCollection collection = _editor.CreateDecorationsCollection([Decoration(1, 1, 3)]);

        Assert.Equal(new Range(1, 1, 1, 3), collection.GetRange(0));
        Assert.Null(collection.GetRange(1));
        Assert.Null(collection.GetRange(-1));
    }

    [Fact]
    public void GetRanges_AfterAnEdit_FollowTheText()
    {
        EditorDecorationsCollection collection = _editor.CreateDecorationsCollection([Decoration(1, 7, 12)]);

        _editor.Model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "oh, ")]);

        Assert.Equal([new Range(1, 11, 1, 16)], collection.GetRanges());
    }

    [Fact]
    public void Has_OwnDecoration_IsTrue_OtherDecoration_IsFalse()
    {
        EditorDecorationsCollection collection = _editor.CreateDecorationsCollection([Decoration(1, 1, 3)]);
        _editor.Model.DeltaDecorations(99, [], [new ModelDeltaDecoration(new Range(1, 1, 1, 3), Options)]);

        List<ModelDecoration> all = OwnedDecorations(_editor.Model.GetAllDecorations());

        Assert.Equal(2, all.Count);
        Assert.Single(all, collection.Has);
    }

    [Fact]
    public void OnDidChange_NotRaisedForItsOwnChanges_RaisedForOtherChanges()
    {
        EditorDecorationsCollection collection = _editor.CreateDecorationsCollection();
        var events = new List<ModelDecorationsChangedEventArgs>();
        using IDisposable subscription = collection.OnDidChange(events.Add);

        collection.Set([Decoration(1, 1, 3)]);
        Assert.Empty(events);

        _editor.Model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "x")]);
        Assert.Single(events);
    }

    [Fact]
    public void OnDidChange_AfterDispose_IsNotRaised()
    {
        EditorDecorationsCollection collection = _editor.CreateDecorationsCollection([Decoration(1, 1, 3)]);
        int raised = 0;
        IDisposable subscription = collection.OnDidChange(_ => raised++);
        subscription.Dispose();

        _editor.Model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "x")]);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Decorations_AreOwnedByTheEditor()
    {
        _editor.CreateDecorationsCollection([Decoration(1, 1, 3)]);
        var range = new Range(1, 1, 1, 12);

        Assert.Single(OwnedDecorations(_editor.GetDecorationsInRange(range)));
        Assert.Single(OwnedDecorations(_editor.Model.GetDecorationsInRange(range, ownerId: _editor.Id)));
        Assert.Empty(OwnedDecorations(_editor.Model.GetDecorationsInRange(range, ownerId: _editor.Id + 1)));
    }
}
