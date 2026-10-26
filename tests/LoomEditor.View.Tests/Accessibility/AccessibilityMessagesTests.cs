using LoomEditor.Core.Model;
using LoomEditor.View.Accessibility;
using static LoomEditor.View.Tests.Accessibility.TestHelpers.AutomationTestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class AccessibilityMessagesTests
{
    // Monaco: '_getAriaLabel'
    [Fact]
    public void FindResult_NoResultsEmptySearch() =>
        Assert.Equal("No results found", AccessibilityMessages.FindResult("No results", null, "", null));

    // Monaco: '_getAriaLabel'
    [Fact]
    public void FindResult_NoResultsWithSearch() =>
        Assert.Equal("No results found for 'foo'", AccessibilityMessages.FindResult("No results", null, "foo", null));

    // Monaco: '_getAriaLabel'
    [Fact]
    public void FindResult_WithCurrentMatch_PrefixesTheLine() =>
        Assert.Equal(
            "a foo b, 3 of 5 found for 'foo', at 2:7",
            AccessibilityMessages.FindResult("3 of 5", Rng(2, 7, 2, 10), "foo", new TextModel("x\na foo b")));

    // Monaco: '_getAriaLabel'
    [Fact]
    public void FindResult_WithCurrentMatch_WithoutModel_NoLinePrefix() =>
        Assert.Equal("3 of 5 found for 'foo', at 2:7", AccessibilityMessages.FindResult("3 of 5", Rng(2, 7, 2, 10), "foo", null));

    // Monaco: '_getAriaLabel'
    [Fact]
    public void FindResult_CurrentMatchOutsideTheModel_NoLinePrefix() =>
        Assert.Equal("3 of 5 found for 'foo', at 9:1", AccessibilityMessages.FindResult("3 of 5", Rng(9, 1, 9, 4), "foo", new TextModel("x")));

    // Monaco: '_getAriaLabel'
    [Fact]
    public void FindResult_NoCurrentMatch() =>
        Assert.Equal("? of 5 found for 'foo'", AccessibilityMessages.FindResult("? of 5", null, "foo", null));

    // Monaco: 'MatchFindAction'
    [Fact]
    public void FindNoResultsAgain() =>
        Assert.Equal("No results found for 'foo'", AccessibilityMessages.FindNoResultsAgain("foo"));

    // Monaco: 'moveNext'
    [Fact]
    public void WordHighlightPosition() =>
        Assert.Equal("a foo, 2 of 4 for 'foo'", AccessibilityMessages.WordHighlightPosition("a foo", 2, 4, "foo"));

    // Monaco: 'toggleTabFocusMode'
    [Fact]
    public void TabMessages_AreMonacosText()
    {
        Assert.Equal("Pressing Tab will now move focus to the next focusable element", AccessibilityMessages.TabMovesFocusOn);
        Assert.Equal("Pressing Tab will now insert the tab character", AccessibilityMessages.TabMovesFocusOff);
    }

    [Fact]
    public void SuggestAdditionalEdits_Text() =>
        Assert.Equal("Accepting 'foo' made 2 additional edits", AccessibilityMessages.SuggestAdditionalEdits("foo", 2));
}
