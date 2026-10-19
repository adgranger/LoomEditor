// Ported from microsoft/vscode-textmate: src/tests/matcher.test.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Grammar;

namespace LoomEditor.TextMate.Tests.Grammar;

public class MatcherTests
{
    public static TheoryData<int, string, string[], bool> Tests => new()
    {
        { 0, "foo", ["foo"], true },
        { 1, "foo", ["bar"], false },
        { 2, "- foo", ["foo"], false },
        { 3, "- foo", ["bar"], true },
        { 4, "- - foo", ["bar"], false },
        { 5, "bar foo", ["foo"], false },
        { 6, "bar foo", ["bar"], false },
        { 7, "bar foo", ["bar", "foo"], true },
        { 8, "bar - foo", ["bar"], true },
        { 9, "bar - foo", ["foo", "bar"], false },
        { 10, "bar - foo", ["foo"], false },
        { 11, "bar, foo", ["foo"], true },
        { 12, "bar, foo", ["bar"], true },
        { 13, "bar, foo", ["bar", "foo"], true },
        { 14, "bar, -foo", ["bar", "foo"], true },
        { 15, "bar, -foo", ["yo"], true },
        { 16, "bar, -foo", ["foo"], false },
        { 17, "(foo)", ["foo"], true },
        { 18, "(foo - bar)", ["foo"], true },
        { 19, "(foo - bar)", ["foo", "bar"], false },
        { 20, "foo bar - (yo man)", ["foo", "bar"], true },
        { 21, "foo bar - (yo man)", ["foo", "bar", "yo"], true },
        { 22, "foo bar - (yo man)", ["foo", "bar", "yo", "man"], false },
        { 23, "foo bar - (yo | man)", ["foo", "bar", "yo", "man"], false },
        { 24, "foo bar - (yo | man)", ["foo", "bar", "yo"], false },
        { 25, "R:text.html - (comment.block, text.html source)", ["text.html", "bar", "source"], false },
        { 26, "text.html.php - (meta.embedded | meta.tag), L:text.html.php meta.tag, L:source.js.embedded.html", ["text.html.php", "bar", "source.js"], true },
    };

    // vscode-textmate: 'Matcher Test #<i>' (one case per index)
    [Theory]
    [MemberData(nameof(Tests))]
    public void MatcherTest(int index, string expression, string[] input, bool expected)
    {
        List<MatcherWithPriority<string[]>> matchers = Matcher.CreateMatchers<string[]>(expression, NameMatcher);
        bool result = matchers.Any(m => m.Matcher(input));
        Assert.True(expected == result, $"Matcher Test #{index}");
    }

    [Fact]
    public void CreateMatchers_Prefixes_SetThePriority()
    {
        List<MatcherWithPriority<string[]>> matchers = Matcher.CreateMatchers<string[]>("L:a, R:b, c", NameMatcher);

        Assert.Equal([-1, 1, 0], matchers.Select(m => m.Priority));
    }

    private static bool NameMatcher(string[] identifers, string[] stackElements)
    {
        int lastIndex = 0;
        return identifers.All(identifier =>
        {
            for (int i = lastIndex; i < stackElements.Length; i++)
            {
                if (stackElements[i] == identifier)
                {
                    lastIndex = i + 1;
                    return true;
                }
            }

            return false;
        });
    }
}
