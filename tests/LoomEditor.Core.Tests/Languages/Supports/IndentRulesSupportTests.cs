using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;

namespace LoomEditor.Core.Tests.Languages.Supports;

public class IndentRulesSupportTests
{
    [Fact]
    public void GetIndentMetadata_CombinesTheFourMasks()
    {
        var support = new IndentRulesSupport(new IndentationRule(
            DecreaseIndentPattern: new Regex(@"^\s*\}", RegexOptions.CultureInvariant),
            IncreaseIndentPattern: new Regex(@"\{\s*$", RegexOptions.CultureInvariant),
            IndentNextLinePattern: new Regex(@"\bif\b", RegexOptions.CultureInvariant),
            UnIndentedLinePattern: new Regex(@"@ignore", RegexOptions.CultureInvariant)));

        Assert.Equal(0, support.GetIndentMetadata("foo();"));
        Assert.Equal(IndentConsts.IncreaseMask, support.GetIndentMetadata("foo {"));
        Assert.Equal(IndentConsts.DecreaseMask, support.GetIndentMetadata("}"));
        Assert.Equal(IndentConsts.IncreaseMask | IndentConsts.DecreaseMask, support.GetIndentMetadata("} else {"));
        Assert.Equal(IndentConsts.IndentNextLineMask, support.GetIndentMetadata("if (a)"));
        Assert.Equal(IndentConsts.UnIndentMask, support.GetIndentMetadata("@ignore"));
        Assert.Equal(0b1111, support.GetIndentMetadata("} if @ignore {"));
    }

    [Fact]
    public void ShouldIgnore_WithoutUnIndentedLinePattern_IsFalse()
    {
        var support = new IndentRulesSupport(new IndentationRule(
            DecreaseIndentPattern: new Regex(@"^\s*\}", RegexOptions.CultureInvariant),
            IncreaseIndentPattern: new Regex(@"\{\s*$", RegexOptions.CultureInvariant)));

        Assert.False(support.ShouldIgnore(""));
        Assert.False(support.ShouldIgnore("@ignore"));
        Assert.False(support.ShouldIndentNextLine("if (a)"));
        Assert.True(support.ShouldIncrease("foo {"));
    }
}
