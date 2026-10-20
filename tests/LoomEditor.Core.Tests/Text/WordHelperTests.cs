using System.Text.RegularExpressions;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

public class WordHelperTests
{
    [Fact]
    public void GetWordAtText_LongLine_SearchesAWindowAroundTheColumn()
    {
        // 2000 spaces, "hello" at columns 2001..2005, 2000 spaces: longer than maxLen (1000), so the text is cut.
        string text = new string(' ', 2000) + "hello" + new string(' ', 2000);

        Assert.Equal(new WordAtPosition("hello", 2001, 2006), WordHelper.GetWordAtText(2003, WordHelper.DefaultWordRegExp, text, 0));
        Assert.Null(WordHelper.GetWordAtText(1000, WordHelper.DefaultWordRegExp, text, 0));
    }

    [Fact]
    public void GetWordAtText_EmptyMatchPattern_Terminates()
    {
        var emptyMatches = new Regex("x*");

        Assert.Equal(new WordAtPosition(string.Empty, 2, 2), WordHelper.GetWordAtText(2, emptyMatches, "abc", 0));
    }
}
