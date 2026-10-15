using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

// Expected values: 0 = Regular, 1 = Whitespace, 2 = WordSeparator (the enum is internal).
public class WordCharacterClassifierTests
{
    [Theory]
    [InlineData('a', 0)]
    [InlineData('é', 0)]
    [InlineData('中', 0)]
    [InlineData('.', 2)]
    [InlineData('(', 2)]
    [InlineData(' ', 1)]
    [InlineData('\t', 1)]
    public void Get_UsualSeparators_ClassifiesCharacter(char c, int expected)
    {
        var classifier = new WordCharacterClassifier(WordHelper.UsualWordSeparators, []);

        Assert.Equal((WordCharacterClass)expected, classifier.Get(c));
    }

    [Fact]
    public void Get_SeparatorsContainSpace_SpaceIsStillWhitespace()
    {
        var classifier = new WordCharacterClassifier(" .", []);

        Assert.Equal(WordCharacterClass.Whitespace, classifier.Get(' '));
        Assert.Equal(WordCharacterClass.WordSeparator, classifier.Get('.'));
    }

    [Fact]
    public void FindIntlWord_Always_ReturnsNull()
    {
        var classifier = new WordCharacterClassifier(WordHelper.UsualWordSeparators, ["ja"]);

        Assert.Null(classifier.FindPrevIntlWordBeforeOrAtOffset("hello", 2));
        Assert.Null(classifier.FindNextIntlWordAtOrAfterOffset("hello", 2));
        Assert.Equal(["ja"], classifier.IntlSegmenterLocales);
    }

    [Fact]
    public void Cache_Get_ReturnsSameInstanceForSameKey()
    {
        var a = WordCharacterClassifierCache.Get("cache-test-a", []);

        Assert.Same(a, WordCharacterClassifierCache.Get("cache-test-a", []));
        Assert.NotSame(a, WordCharacterClassifierCache.Get("cache-test-a", ["fr"]));
    }

    [Fact]
    public void Cache_Get_EvictsLeastRecentlyUsedAfterTenOtherKeys()
    {
        var a = WordCharacterClassifierCache.Get("evict-test-a", []);
        for (var i = 0; i < 10; i++)
        {
            WordCharacterClassifierCache.Get($"evict-test-{i}", []);
        }

        Assert.NotSame(a, WordCharacterClassifierCache.Get("evict-test-a", []));
    }

    [Fact]
    public void UsualWordSeparators_Value_MatchesMonaco()
    {
        Assert.Equal("`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?", WordHelper.UsualWordSeparators);
        Assert.Equal(31, WordHelper.UsualWordSeparators.Length);
        Assert.Equal(1, WordHelper.UsualWordSeparators.Count(c => c == '\\'));
    }
}
