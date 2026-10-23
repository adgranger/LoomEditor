using LoomEditor.Core.Text;
using LoomEditor.View.Tests.Accessibility.TestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class AutomationTextDocumentTests
{
    [Fact]
    public void DocumentEnd_MultiLineText_IsTheEndOfTheLastLine() =>
        AutomationTestHelpers.WithDocument("ab\ncde", d => Assert.Equal(new Position(2, 4), d.DocumentEnd));

    [Fact]
    public void DocumentEnd_EmptyModel_IsDocumentStart() =>
        AutomationTestHelpers.WithDocument("", d => Assert.Equal(d.DocumentStart, d.DocumentEnd));

    [Fact]
    public void DocumentRange_CoversTheWholeModel() =>
        AutomationTestHelpers.WithDocument("ab\ncde", d => Assert.Equal(new Range(1, 1, 2, 4), d.DocumentRange));

    [Fact]
    public void GetText_CrlfModel_UsesLineFeeds() =>
        AutomationTestHelpers.WithDocument("a\r\nb", d => Assert.Equal("a\nb", d.GetText(d.DocumentRange)));

    [Fact]
    public void GetText_RangeAcrossLines_JoinsWithLineFeed() =>
        AutomationTestHelpers.WithDocument("abc\ndef", d => Assert.Equal("c\nd", d.GetText(new Range(1, 3, 2, 2))));

    [Fact]
    public void GetText_RangeOutsideTheModel_IsClampedToTheModel() =>
        AutomationTestHelpers.WithDocument("abc", d => Assert.Equal("abc", d.GetText(new Range(1, 1, 9, 9))));

    [Fact]
    public void GetText_MaxLength_TruncatesInUtf16Units() =>
        AutomationTestHelpers.WithDocument("abcdef", d => Assert.Equal("abc", d.GetText(d.DocumentRange, 3)));

    [Fact]
    public void GetText_MaxLengthZero_ReturnsEmpty() =>
        AutomationTestHelpers.WithDocument("abc", d => Assert.Equal("", d.GetText(d.DocumentRange, 0)));

    [Fact]
    public void GetText_MaxLengthCutsASurrogatePair_DropsTheHighSurrogate() =>
        AutomationTestHelpers.WithDocument("a😀b", d => Assert.Equal("a", d.GetText(d.DocumentRange, 2)));

    [Fact]
    public void GetText_MaxLengthMinusOne_ReturnsEverything() =>
        AutomationTestHelpers.WithDocument("abc", d => Assert.Equal("abc", d.GetText(d.DocumentRange, -1)));

    [Fact]
    public void GetText_MaxLengthBelowMinusOne_Throws() =>
        AutomationTestHelpers.WithDocument("abc", d => Assert.Throws<ArgumentOutOfRangeException>(() => d.GetText(d.DocumentRange, -2)));
}
