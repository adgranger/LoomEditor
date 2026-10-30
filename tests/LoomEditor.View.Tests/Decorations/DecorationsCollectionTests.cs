using LoomEditor.Core.Decorations;
using LoomEditor.Core.Text;
using LoomEditor.View.Decorations;
using LoomEditor.View.Features;

namespace LoomEditor.View.Tests.Decorations;

public class DecorationsCollectionTests
{
    private static readonly EditorDecorationOptions Options = new() { ClassName = "x" };

    private readonly EditorContext _editor = CreateEditor();

    private static EditorContext CreateEditor()
    {
        var editor = new EditorContext();
        editor.SetText("a\nb\nc\nd");
        return editor;
    }

    private DecorationsCollection Create() => new(_editor.CreateDecorationsCollection());

    private static EditorDecoration Line(int line) => new(new Range(line, 1, line, 2), Options);

    // The cursor keeps its selection as a tracked range, which is a decoration of owner 0.
    private List<ModelDecoration> Owned() => [.. _editor.Model.GetAllDecorations().Where(d => d.OwnerId != 0)];

    [Fact]
    public void Set_TwoThenOne_ReplacesTheDecorations()
    {
        DecorationsCollection collection = Create();

        collection.Set([Line(1), Line(2)]);
        Assert.Equal(2, collection.Count);
        Assert.Equal(2, Owned().Count);
        Assert.All(Owned(), d => Assert.Equal("x", d.Options.ClassName));

        collection.Set([Line(3)]);
        Assert.Equal(1, collection.Count);
    }

    [Fact]
    public void Append_KeepsInsertionOrderAndClearEmpties()
    {
        DecorationsCollection collection = Create();
        collection.Set([Line(1)]);

        collection.Append([Line(3)]);

        Assert.Equal(2, collection.Count);
        Assert.Equal([new Range(1, 1, 1, 2), new Range(3, 1, 3, 2)], collection.GetRanges());

        collection.Clear();
        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void GetRange_AfterInsertingALineAtTheTop_FollowsTheText()
    {
        DecorationsCollection collection = Create();
        collection.Set([Line(2)]);

        _editor.Model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "new\n")]);

        Assert.Equal(new Range(3, 1, 3, 2), collection.GetRange(0));
    }

    [Fact]
    public void GetRange_TypingAtTheEnd_DoesNotGrowTheRange()
    {
        DecorationsCollection collection = Create();
        collection.Set([Line(1)]);

        _editor.Model.ApplyEdits([TextEdit.Insert(new Position(1, 2), "zz")]);

        Assert.Equal(new Range(1, 1, 1, 2), collection.GetRange(0));
    }

    [Fact]
    public void GetRange_BadIndex_ReturnsNull()
    {
        DecorationsCollection collection = Create();
        collection.Set([Line(1)]);

        Assert.Null(collection.GetRange(-1));
        Assert.Null(collection.GetRange(collection.Count));
    }

    [Fact]
    public void Set_NullList_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Create().Set(null!));
    }

    [Fact]
    public void Set_DefaultDecoration_ThrowsWithTheIndex()
    {
        ArgumentException e = Assert.Throws<ArgumentException>(() => Create().Set([default]));

        Assert.Equal("decorations", e.ParamName);
        Assert.Contains("0", e.Message);
    }

    [Fact]
    public void Set_SharedOptions_ShareOneInternalInstance()
    {
        Create().Set([Line(1), Line(2)]);

        List<ModelDecoration> owned = Owned();
        Assert.Same(owned[0].Options, owned[1].Options);
    }
}
