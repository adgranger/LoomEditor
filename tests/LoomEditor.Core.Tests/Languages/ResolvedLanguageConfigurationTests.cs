using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Languages;

public class ResolvedLanguageConfigurationTests
{
    [Fact]
    public void Resolved_IndentationRulesWithoutBrackets_GetTheDefaultBrackets()
    {
        var rules = new IndentationRule(new Regex("^\\s*\\}"), new Regex("\\{\\s*$"));

        var config = new ResolvedLanguageConfiguration("test", new LanguageConfiguration { IndentationRules = rules });

        Assert.Same(OnEnterSupport.DefaultBrackets, config.UnderlyingConfig.Brackets);
        Assert.Equal("() {} []", string.Join(' ', config.CharacterPair.GetAutoClosingPairs().Select(p => p.Open + p.Close)));
        Assert.NotNull(config.Brackets);
        Assert.Equal(")}]", string.Concat(config.ElectricCharacter.GetElectricCharacters()));
        Assert.NotNull(config.IndentRulesSupport);
        Assert.Same(rules, config.IndentationRules);
        Assert.Equal(IndentAction.Indent, config.OnEnter(EditorAutoIndentStrategy.Full, "", "{", "")?.IndentAction);
    }

    [Fact]
    public void Resolved_LineCommentObject_KeepsNoIndent()
    {
        var withNoIndent = new ResolvedLanguageConfiguration("test", new LanguageConfiguration
        {
            Comments = new CommentRule(new LineCommentConfig("#", NoIndent: true), new CharacterPair("/*", "*/")),
        });
        var emptyString = new ResolvedLanguageConfiguration("test", new LanguageConfiguration
        {
            Comments = new CommentRule(new LineCommentConfig("")),
        });

        Assert.Equal(new CommentsConfiguration("#", true, "/*", "*/"), withNoIndent.Comments);
        Assert.Equal(new CommentsConfiguration(), emptyString.Comments);
    }

    [Fact]
    public void Resolved_NoWordPattern_UsesTheDefaultWordRegExp()
    {
        var config = new ResolvedLanguageConfiguration("test", new LanguageConfiguration());
        var pattern = new Regex("[a-z]+");
        var custom = new ResolvedLanguageConfiguration("test", new LanguageConfiguration { WordPattern = pattern });

        Assert.Same(WordHelper.DefaultWordRegExp, config.WordDefinition);
        Assert.Same(WordHelper.DefaultWordRegExp, config.GetWordDefinition());
        Assert.Same(pattern, custom.GetWordDefinition());
        Assert.False(config.FoldingRules.OffSide);
    }

    [Fact]
    public void DefaultWordRegExp_MatchesNumbersAndWords()
    {
        Assert.Equal("-1.5e3", WordHelper.DefaultWordRegExp.Match("-1.5e3;").Value);
        Assert.Equal("foo_bar", WordHelper.DefaultWordRegExp.Match("(foo_bar)").Value);
        Assert.Equal("a b", string.Join(' ', WordHelper.DefaultWordRegExp.Matches("a.b").Select(m => m.Value)));
        Assert.Equal("a.b", WordHelper.CreateWordRegExp(".").Match("a.b").Value);
    }
}
