using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

// New tests: Monaco's goToDiff is tested through the whole widget.
public class DiffNavigatorTests
{
    // Changes at modified lines 5, 10 and 20 (one line each); 30 lines.
    private static readonly DiffMapping[] Mappings = [Mapping(5), Mapping(10), Mapping(20)];

    private static DiffMapping Mapping(int modifiedStart) =>
        new(new DetailedLineRangeMapping(new LineRange(modifiedStart, modifiedStart + 1), new LineRange(modifiedStart, modifiedStart + 1), null));

    private static int? Find(int line, NavigationDirection direction) =>
        DiffNavigator.Find(Mappings, line, 30, direction)?.LineRangeMapping.Modified.StartLineNumber;

    [Fact]
    public void Find_Empty_Null() => Assert.Null(DiffNavigator.Find([], 1, 30, NavigationDirection.Next));

    [Fact]
    public void Find_Next_FirstAfterCursor() => Assert.Equal(10, Find(7, NavigationDirection.Next));

    [Fact]
    public void Find_Next_AtLastLine_WrapsToFirst() => Assert.Equal(5, Find(30, NavigationDirection.Next));

    [Fact]
    public void Find_Next_NoneAfter_WrapsToFirst() => Assert.Equal(5, Find(25, NavigationDirection.Next));

    [Fact]
    public void Find_Next_CursorOnChangeStart_GoesToTheNextOne() => Assert.Equal(20, Find(10, NavigationDirection.Next));

    [Fact]
    public void Find_Previous_LastBeforeCursor() => Assert.Equal(10, Find(15, NavigationDirection.Previous));

    [Fact]
    public void Find_Previous_NoneBefore_WrapsToLast() => Assert.Equal(20, Find(3, NavigationDirection.Previous));

    [Fact]
    public void Find_Previous_CursorOnChangeStart_GoesToThePreviousOne() => Assert.Equal(5, Find(10, NavigationDirection.Previous));
}
