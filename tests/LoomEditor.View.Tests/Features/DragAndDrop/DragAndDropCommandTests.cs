using LoomEditor.Core.Text;
using LoomEditor.View.Features.DragAndDrop;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Features.DragAndDrop;

// Monaco has no test for dragAndDropCommand.ts: all tests are new.
public class DragAndDropCommandTests
{
    private static void Test(string[] lines, Selection selection, Position target, bool copy, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(lines, selection, sel => new DragAndDropCommand(sel, target, copy), expectedLines, expectedSelection);

    [Fact]
    public void Move_Down_SelectionFollowsText() =>
        Test(["01234567", "abcdefgh"], new Selection(1, 2, 1, 4), new Position(2, 5), false, ["034567", "abcd12efgh"], new Selection(2, 5, 2, 7));

    [Fact]
    public void Move_Up_SelectionFollowsText() =>
        Test(["01234567", "abcdefgh"], new Selection(2, 2, 2, 4), new Position(1, 5), false, ["0123bc4567", "adefgh"], new Selection(1, 5, 1, 7));

    [Fact]
    public void Move_WithinSameLine_Right() =>
        Test(["0123456789"], new Selection(1, 2, 1, 4), new Position(1, 7), false, ["0345126789"], new Selection(1, 5, 1, 7));

    [Fact]
    public void Move_WithinSameLine_Left() =>
        Test(["0123456789"], new Selection(1, 6, 1, 8), new Position(1, 3), false, ["0156234789"], new Selection(1, 3, 1, 5));

    [Fact]
    public void Move_MultiLineSelection_Down() =>
        Test(["ab", "cd", "ef", "gh"], new Selection(1, 2, 2, 2), new Position(4, 2), false, ["ad", "ef", "gb", "ch"], new Selection(3, 2, 4, 2));

    [Fact]
    public void Copy_SingleLine_SelectsCopy() =>
        Test(["0123456789"], new Selection(1, 2, 1, 4), new Position(1, 7), true, ["012345126789"], new Selection(1, 7, 1, 9));

    [Fact]
    public void Copy_MultiLine_SelectsCopy() =>
        Test(["ab", "cd", "ef"], new Selection(1, 2, 2, 2), new Position(3, 2), true, ["ab", "cd", "eb", "cf"], new Selection(3, 2, 4, 2));

    [Fact]
    public void Copy_DropAtSelectionEnd_Allowed() =>
        Test(["0123456789"], new Selection(1, 2, 1, 4), new Position(1, 4), true, ["012123456789"], new Selection(1, 4, 1, 6));

    [Fact]
    public void Copy_DropAtSelectionStart_Allowed() =>
        Test(["0123456789"], new Selection(1, 2, 1, 4), new Position(1, 2), true, ["012123456789"], new Selection(1, 2, 1, 4));

    // The controller never runs the command for a move inside the selection (see DragAndDropControllerTests); the command
    // then keeps the selection, like Monaco.
    [Fact]
    public void Copy_DropInsideSelection_KeepsTheSelection() =>
        Test(["0123456789"], new Selection(1, 2, 1, 6), new Position(1, 4), true, ["01212343456789"], new Selection(1, 2, 1, 6));
}
