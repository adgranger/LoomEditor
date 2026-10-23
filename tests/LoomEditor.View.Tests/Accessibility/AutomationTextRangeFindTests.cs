using LoomEditor.View.Accessibility;
using LoomEditor.View.Tests.Accessibility.TestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class AutomationTextRangeFindTests
{
    private static void Find(string text, string search, bool backward, bool ignoreCase, Range? within, Range? expected) =>
        AutomationTestHelpers.WithDocument(text, d =>
        {
            var range = new AutomationTextRange(d, within ?? d.DocumentRange);
            AutomationTextRange? found = range.FindText(search, backward, ignoreCase);
            if (expected is null)
            {
                Assert.Null(found);
            }
            else
            {
                Assert.NotNull(found);
                Assert.Equal(expected, found.Range);
                Assert.Same(d, found.Document);
            }
        });

    [Fact]
    public void FindText_Forward_ReturnsTheFirstMatch() => Find("abc abc", "abc", false, false, null, new Range(1, 1, 1, 4));

    [Fact]
    public void FindText_Backward_ReturnsTheLastMatch() => Find("abc abc", "abc", true, false, null, new Range(1, 5, 1, 8));

    [Fact]
    public void FindText_IgnoreCase() => Find("xABC", "abc", false, true, null, new Range(1, 2, 1, 5));

    [Fact]
    public void FindText_CaseSensitive_NoMatch_ReturnsNull() => Find("xABC", "abc", false, false, null, null);

    [Fact]
    public void FindText_Empty_ReturnsNull() => Find("abc", "", false, false, null, null);

    [Fact]
    public void FindText_OnlyInsideTheRange() => Find("abc abc", "abc", true, false, new Range(1, 1, 1, 4), new Range(1, 1, 1, 4));

    [Fact]
    public void FindText_AcrossLines() => Find("a\nb", "a\nb", false, false, null, new Range(1, 1, 2, 2));

    [Fact]
    public void FindText_Crlf_SearchTextWithCarriageReturn_ReturnsNull() => Find("a\r\nb", "a\r\nb", false, false, null, null);

    [Fact]
    public void FindText_DoesNotChangeTheRange() =>
        AutomationTestHelpers.WithDocument("abc abc", d =>
        {
            var range = new AutomationTextRange(d, d.DocumentRange);
            range.FindText("abc", true, false);
            Assert.Equal(d.DocumentRange, range.Range);
        });

    [Fact]
    public void FindText_Unicode_Ordinal() => Find("日本語", "本", false, false, null, new Range(1, 2, 1, 3));

    [Fact]
    public void FindText_SurrogatePair() => Find("a😀b", "😀", false, false, null, new Range(1, 2, 1, 4));

    [Fact]
    public void FindText_Null_Throws() =>
        AutomationTestHelpers.WithDocument("abc", d =>
            Assert.Throws<ArgumentNullException>(() => AutomationTextRange.ForDocument(d).FindText(null!, false, false)));
}
