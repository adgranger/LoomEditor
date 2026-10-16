using LoomEditor.Core.Text;
using LoomEditor.View.Features;

namespace LoomEditor.View.Tests.Commands;

public class CoreCommandRegistrationTests
{
    [Fact]
    public void Execute_CursorDown_MovesThePrimaryCursorToTheNextLine()
    {
        var context = new EditorContext();
        context.SetText("abc\ndef\nghi");

        bool ran = context.Commands.Execute("cursorDown");

        Assert.True(ran);
        Assert.Equal(new Position(2, 1), context.Selection.Position);
    }

    [Fact]
    public void Execute_RemoveSecondaryCursors_KeepsThePrimaryCursor()
    {
        var context = new EditorContext();
        context.SetText("abc\ndef\nghi");
        context.SetSelections([new Selection(2, 2, 2, 2), new Selection(1, 1, 1, 1), new Selection(3, 3, 3, 3)]);

        context.Commands.Execute("removeSecondaryCursors");

        Assert.Single(context.Selections);
        Assert.Equal(new Selection(2, 2, 2, 2), context.Selection);
    }

    [Fact]
    public void Constructor_RegistersTheCoreNavigationIds()
    {
        var context = new EditorContext();

        string[] ids = ["cursorLeft", "cursorRightSelect", "cursorPageDown", "cursorHome", "cursorLineEnd", "cursorEnd", "cursorTop", "cursorBottomSelect", "cancelSelection", "removeSecondaryCursors"];

        Assert.All(ids, id => Assert.True(context.Commands.Contains(id), id));
        Assert.False(context.Commands.Contains("cursorMove"));
    }
}
