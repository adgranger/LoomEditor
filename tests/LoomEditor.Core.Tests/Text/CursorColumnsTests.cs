// Ported from microsoft/vscode: src/vs/editor/test/common/core/cursorColumns.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

public class CursorColumnsTests
{
    // Monaco: 'visibleColumnFromColumn'
    [Fact]
    public void VisibleColumnFromColumn_Monaco()
    {
        static void Visible(string text, int tabSize, int column, int expected)
        {
            Assert.Equal(expected, CursorColumns.VisibleColumnFromColumn(text, column, tabSize));
        }

        Visible("\t\tvar x = 3;", 4, 1, 0);
        Visible("\t\tvar x = 3;", 4, 2, 4);
        Visible("\t\tvar x = 3;", 4, 3, 8);
        Visible("\t\tvar x = 3;", 4, 4, 9);
        Visible("\t\tvar x = 3;", 4, 5, 10);
        Visible("\t\tvar x = 3;", 4, 6, 11);
        Visible("\t\tvar x = 3;", 4, 7, 12);
        Visible("\t\tvar x = 3;", 4, 8, 13);
        Visible("\t\tvar x = 3;", 4, 9, 14);
        Visible("\t\tvar x = 3;", 4, 10, 15);
        Visible("\t\tvar x = 3;", 4, 11, 16);
        Visible("\t\tvar x = 3;", 4, 12, 17);
        Visible("\t\tvar x = 3;", 4, 13, 18);
        Visible("\t \tvar x = 3;", 4, 1, 0);
        Visible("\t \tvar x = 3;", 4, 2, 4);
        Visible("\t \tvar x = 3;", 4, 3, 5);
        Visible("\t \tvar x = 3;", 4, 4, 8);
        Visible("\t \tvar x = 3;", 4, 5, 9);
        Visible("\t \tvar x = 3;", 4, 6, 10);
        Visible("\t \tvar x = 3;", 4, 7, 11);
        Visible("\t \tvar x = 3;", 4, 8, 12);
        Visible("\t \tvar x = 3;", 4, 9, 13);
        Visible("\t \tvar x = 3;", 4, 10, 14);
        Visible("\t \tvar x = 3;", 4, 11, 15);
        Visible("\t \tvar x = 3;", 4, 12, 16);
        Visible("\t \tvar x = 3;", 4, 13, 17);
        Visible("\t \tvar x = 3;", 4, 14, 18);
        Visible("\t  \tx\t", 4, -1, 0);
        Visible("\t  \tx\t", 4, 0, 0);
        Visible("\t  \tx\t", 4, 1, 0);
        Visible("\t  \tx\t", 4, 2, 4);
        Visible("\t  \tx\t", 4, 3, 5);
        Visible("\t  \tx\t", 4, 4, 6);
        Visible("\t  \tx\t", 4, 5, 8);
        Visible("\t  \tx\t", 4, 6, 9);
        Visible("\t  \tx\t", 4, 7, 12);
        Visible("\t  \tx\t", 4, 8, 12);
        Visible("\t  \tx\t", 4, 9, 12);
        Visible("baz", 4, 1, 0);
        Visible("baz", 4, 2, 1);
        Visible("baz", 4, 3, 2);
        Visible("baz", 4, 4, 3);
        Visible("📚az", 4, 1, 0);
        Visible("📚az", 4, 2, 1);
        Visible("📚az", 4, 3, 2);
        Visible("📚az", 4, 4, 3);
        Visible("📚az", 4, 5, 4);
    }

    // Monaco: 'toStatusbarColumn'
    [Fact]
    public void ToStatusbarColumn_Monaco()
    {
        static void Status(string text, int tabSize, int column, int expected)
        {
            Assert.Equal(expected, CursorColumns.ToStatusbarColumn(text, column, tabSize));
        }

        Status("    spaces", 4, 1, 1);
        Status("    spaces", 4, 2, 2);
        Status("    spaces", 4, 3, 3);
        Status("    spaces", 4, 4, 4);
        Status("    spaces", 4, 5, 5);
        Status("    spaces", 4, 6, 6);
        Status("    spaces", 4, 7, 7);
        Status("    spaces", 4, 8, 8);
        Status("    spaces", 4, 9, 9);
        Status("    spaces", 4, 10, 10);
        Status("    spaces", 4, 11, 11);
        Status("\ttab", 4, 1, 1);
        Status("\ttab", 4, 2, 5);
        Status("\ttab", 4, 3, 6);
        Status("\ttab", 4, 4, 7);
        Status("\ttab", 4, 5, 8);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 1, 1);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 2, 2);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 3, 2);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 4, 3);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 5, 3);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 6, 4);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 7, 4);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 8, 5);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 9, 5);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 10, 6);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 11, 6);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 12, 7);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 13, 7);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 14, 8);
        Status("𐌀𐌁𐌂𐌃𐌄𐌅𐌆", 4, 15, 8);
        Status("🎈🎈🎈🎈", 4, 1, 1);
        Status("🎈🎈🎈🎈", 4, 2, 2);
        Status("🎈🎈🎈🎈", 4, 3, 2);
        Status("🎈🎈🎈🎈", 4, 4, 3);
        Status("🎈🎈🎈🎈", 4, 5, 3);
        Status("🎈🎈🎈🎈", 4, 6, 4);
        Status("🎈🎈🎈🎈", 4, 7, 4);
        Status("🎈🎈🎈🎈", 4, 8, 5);
        Status("🎈🎈🎈🎈", 4, 9, 5);
        Status("何何何何", 4, 1, 1);
        Status("何何何何", 4, 2, 2);
        Status("何何何何", 4, 3, 3);
        Status("何何何何", 4, 4, 4);
    }

    // Monaco: 'columnFromVisibleColumn'
    [Fact]
    public void ColumnFromVisibleColumn_Monaco()
    {
        static void Column(string text, int tabSize, int visibleColumn, int expected)
        {
            Assert.Equal(expected, CursorColumns.ColumnFromVisibleColumn(text, visibleColumn, tabSize));
        }

        Column("\t\tvar x = 3;", 4, 1, 1);
        Column("\t\tvar x = 3;", 4, 2, 1);
        Column("\t\tvar x = 3;", 4, 3, 2);
        Column("\t\tvar x = 3;", 4, 4, 2);
        Column("\t\tvar x = 3;", 4, 5, 2);
        Column("\t\tvar x = 3;", 4, 6, 2);
        Column("\t\tvar x = 3;", 4, 7, 3);
        Column("\t\tvar x = 3;", 4, 8, 3);
        Column("\t\tvar x = 3;", 4, 9, 4);
        Column("\t\tvar x = 3;", 4, 10, 5);
        Column("\t\tvar x = 3;", 4, 11, 6);
        Column("\t\tvar x = 3;", 4, 12, 7);
        Column("\t\tvar x = 3;", 4, 13, 8);
        Column("\t\tvar x = 3;", 4, 14, 9);
        Column("\t\tvar x = 3;", 4, 15, 10);
        Column("\t\tvar x = 3;", 4, 16, 11);
        Column("\t\tvar x = 3;", 4, 17, 12);
        Column("\t\tvar x = 3;", 4, 18, 13);
        Column("\t \tvar x = 3;", 4, 0, 1);
        Column("\t \tvar x = 3;", 4, 1, 1);
        Column("\t \tvar x = 3;", 4, 2, 1);
        Column("\t \tvar x = 3;", 4, 3, 2);
        Column("\t \tvar x = 3;", 4, 4, 2);
        Column("\t \tvar x = 3;", 4, 5, 3);
        Column("\t \tvar x = 3;", 4, 6, 3);
        Column("\t \tvar x = 3;", 4, 7, 4);
        Column("\t \tvar x = 3;", 4, 8, 4);
        Column("\t \tvar x = 3;", 4, 9, 5);
        Column("\t \tvar x = 3;", 4, 10, 6);
        Column("\t \tvar x = 3;", 4, 11, 7);
        Column("\t \tvar x = 3;", 4, 12, 8);
        Column("\t \tvar x = 3;", 4, 13, 9);
        Column("\t \tvar x = 3;", 4, 14, 10);
        Column("\t \tvar x = 3;", 4, 15, 11);
        Column("\t \tvar x = 3;", 4, 16, 12);
        Column("\t \tvar x = 3;", 4, 17, 13);
        Column("\t \tvar x = 3;", 4, 18, 14);
        Column("\t  \tx\t", 4, -2, 1);
        Column("\t  \tx\t", 4, -1, 1);
        Column("\t  \tx\t", 4, 0, 1);
        Column("\t  \tx\t", 4, 1, 1);
        Column("\t  \tx\t", 4, 2, 1);
        Column("\t  \tx\t", 4, 3, 2);
        Column("\t  \tx\t", 4, 4, 2);
        Column("\t  \tx\t", 4, 5, 3);
        Column("\t  \tx\t", 4, 6, 4);
        Column("\t  \tx\t", 4, 7, 4);
        Column("\t  \tx\t", 4, 8, 5);
        Column("\t  \tx\t", 4, 9, 6);
        Column("\t  \tx\t", 4, 10, 6);
        Column("\t  \tx\t", 4, 11, 7);
        Column("\t  \tx\t", 4, 12, 7);
        Column("\t  \tx\t", 4, 13, 7);
        Column("\t  \tx\t", 4, 14, 7);
        Column("baz", 4, 0, 1);
        Column("baz", 4, 1, 2);
        Column("baz", 4, 2, 3);
        Column("baz", 4, 3, 4);
        Column("📚az", 4, 0, 1);
        Column("📚az", 4, 1, 1);
        Column("📚az", 4, 2, 3);
        Column("📚az", 4, 3, 4);
        Column("📚az", 4, 4, 5);
    }

    // Monaco: 'nextRenderTabStop', 'nextIndentTabStop'
    [Theory]
    [InlineData("nextRenderTabStop")]
    [InlineData("nextIndentTabStop")]
    public void NextTabStop_Monaco(string name)
    {
        Func<int, int, int> fn = name == "nextRenderTabStop" ? CursorColumns.NextRenderTabStop : CursorColumns.NextIndentTabStop;
        Assert.Equal(4, fn(0, 4));
        Assert.Equal(4, fn(1, 4));
        Assert.Equal(4, fn(2, 4));
        Assert.Equal(4, fn(3, 4));
        Assert.Equal(8, fn(4, 4));
        Assert.Equal(8, fn(5, 4));
        Assert.Equal(8, fn(6, 4));
        Assert.Equal(8, fn(7, 4));
        Assert.Equal(12, fn(8, 4));
        Assert.Equal(2, fn(0, 2));
        Assert.Equal(2, fn(1, 2));
        Assert.Equal(4, fn(2, 2));
        Assert.Equal(4, fn(3, 2));
        Assert.Equal(6, fn(4, 2));
        Assert.Equal(6, fn(5, 2));
        Assert.Equal(8, fn(6, 2));
        Assert.Equal(8, fn(7, 2));
        Assert.Equal(10, fn(8, 2));
        Assert.Equal(1, fn(0, 1));
        Assert.Equal(2, fn(1, 1));
        Assert.Equal(3, fn(2, 1));
        Assert.Equal(4, fn(3, 1));
        Assert.Equal(5, fn(4, 1));
        Assert.Equal(6, fn(5, 1));
        Assert.Equal(7, fn(6, 1));
        Assert.Equal(8, fn(7, 1));
        Assert.Equal(9, fn(8, 1));
    }

    // Monaco: 'prevRenderTabStop', 'prevIndentTabStop'
    [Theory]
    [InlineData("prevRenderTabStop")]
    [InlineData("prevIndentTabStop")]
    public void PrevTabStop_Monaco(string name)
    {
        Func<int, int, int> fn = name == "prevRenderTabStop" ? CursorColumns.PrevRenderTabStop : CursorColumns.PrevIndentTabStop;
        Assert.Equal(0, fn(0, 4));
        Assert.Equal(0, fn(1, 4));
        Assert.Equal(0, fn(2, 4));
        Assert.Equal(0, fn(3, 4));
        Assert.Equal(0, fn(4, 4));
        Assert.Equal(4, fn(5, 4));
        Assert.Equal(4, fn(6, 4));
        Assert.Equal(4, fn(7, 4));
        Assert.Equal(4, fn(8, 4));
        Assert.Equal(8, fn(9, 4));
        Assert.Equal(0, fn(0, 2));
        Assert.Equal(0, fn(1, 2));
        Assert.Equal(0, fn(2, 2));
        Assert.Equal(2, fn(3, 2));
        Assert.Equal(2, fn(4, 2));
        Assert.Equal(4, fn(5, 2));
        Assert.Equal(4, fn(6, 2));
        Assert.Equal(6, fn(7, 2));
        Assert.Equal(6, fn(8, 2));
        Assert.Equal(8, fn(9, 2));
        Assert.Equal(0, fn(0, 1));
        Assert.Equal(0, fn(1, 1));
        Assert.Equal(1, fn(2, 1));
        Assert.Equal(2, fn(3, 1));
        Assert.Equal(3, fn(4, 1));
        Assert.Equal(4, fn(5, 1));
        Assert.Equal(5, fn(6, 1));
        Assert.Equal(6, fn(7, 1));
        Assert.Equal(7, fn(8, 1));
        Assert.Equal(8, fn(9, 1));
    }
}
