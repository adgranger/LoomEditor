using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Oniguruma;
using LoomEditor.TextMate.Tests.TestHelpers;

namespace LoomEditor.TextMate.Tests.Grammar;

public class RegExpSourceTests
{
    private const char X = char.MaxValue; // U+FFFF, the disallowed anchor

    private static readonly IOnigLib Lib = OnigTestLib.GetOniguruma();

    [Fact]
    public void RegExpSource_BackslashZ_IsRewritten()
    {
        var source = new RegExpSource(@"a\z|\\z|b\zc", new RuleId(1));

        Assert.Equal(@"a$(?!\n)(?<!\n)|\\z|b$(?!\n)(?<!\n)c", source.Source);
        Assert.False(source.HasAnchor);
        Assert.Equal(new RuleId(1), source.RuleId);
    }

    [Fact]
    public void RegExpSource_Anchors_AreDetected()
    {
        Assert.True(new RegExpSource(@"\Gx", RuleId.End).HasAnchor);
        Assert.True(new RegExpSource(@"\Ax", RuleId.End).HasAnchor);
        Assert.False(new RegExpSource(@"\\Ax", RuleId.End).HasAnchor);
        Assert.False(new RegExpSource(string.Empty, RuleId.End).HasAnchor);
    }

    [Fact]
    public void RegExpSource_ResolveBackReferences_EscapesCapturedText()
    {
        var source = new RegExpSource(@"\1-\2-\9", RuleId.End);
        OnigCaptureIndex[] captures = [new(0, 7), new(0, 3), new(4, 7)];

        Assert.True(source.HasBackReferences);
        Assert.Equal(@"a\.b-c\(\)-", source.ResolveBackReferences("a.b c()", captures));
        Assert.False(new RegExpSource("abc", RuleId.End).HasBackReferences);
    }

    [Fact]
    public void RegExpSource_ResolveAnchors_DisallowedAnchorsBecomeFFFF()
    {
        var source = new RegExpSource(@"\A\G\\A", RuleId.End);

        Assert.Equal($@"\{X}\{X}\\A", source.ResolveAnchors(allowA: false, allowG: false));
        Assert.Equal($@"\{X}\G\\A", source.ResolveAnchors(allowA: false, allowG: true));
        Assert.Equal($@"\A\{X}\\A", source.ResolveAnchors(allowA: true, allowG: false));
        Assert.Equal(@"\A\G\\A", source.ResolveAnchors(allowA: true, allowG: true));
        Assert.Equal("abc", new RegExpSource("abc", RuleId.End).ResolveAnchors(false, false));
    }

    [Fact]
    public void RegExpSource_SetSource_RebuildsTheAnchorCache()
    {
        var source = new RegExpSource(@"\Ga", RuleId.End);
        RegExpSource clone = source.Clone();

        source.SetSource(@"\Gb");

        Assert.Equal($@"\{X}b", source.ResolveAnchors(false, false));
        Assert.Equal($@"\{X}a", clone.ResolveAnchors(false, false));
    }

    [Fact]
    public void RegExpSourceList_CompileAG_CachesOneScannerPerVariant()
    {
        using var list = new RegExpSourceList();
        list.Push(new RegExpSource("a", new RuleId(1)));
        list.Push(new RegExpSource(@"\Gb", new RuleId(2)));

        CompiledRule a0g0 = list.CompileAG(Lib, false, false);
        CompiledRule a0g1 = list.CompileAG(Lib, false, true);
        CompiledRule a1g0 = list.CompileAG(Lib, true, false);
        CompiledRule a1g1 = list.CompileAG(Lib, true, true);

        Assert.Equal(4, new HashSet<CompiledRule>([a0g0, a0g1, a1g0, a1g1]).Count);
        Assert.Same(a0g1, list.CompileAG(Lib, false, true));
        Assert.Same(a1g1, list.CompileAG(Lib, true, true));
        Assert.Equal(2, list.Length);

        // \G is disallowed in A0_G0: the "b" at the start position does not match.
        Assert.Null(a0g0.FindNextMatchSync("b", 0, FindOption.None));
        Assert.Equal(new RuleId(2), a0g1.FindNextMatchSync("b", 0, FindOption.None)!.RuleId);

        list.SetSource(0, "c");
        Assert.NotSame(a0g1, list.CompileAG(Lib, false, true));
    }

    [Fact]
    public void RegExpSourceList_WithoutAnchors_CompileAGIsCompile()
    {
        using var list = new RegExpSourceList();
        list.Push(new RegExpSource("a", new RuleId(1)));
        list.Unshift(new RegExpSource("b", new RuleId(2)));

        CompiledRule compiled = list.Compile(Lib);

        Assert.Same(compiled, list.CompileAG(Lib, true, false));
        Assert.Equal(new RuleId(2), compiled.FindNextMatchSync("ba", 0, FindOption.None)!.RuleId);
    }

    [Fact]
    public void CompiledRule_FindNextMatch_ReturnsRuleIdOfMatchedPattern()
    {
        using var rule = new CompiledRule(Lib, ["x+", "(y)(z)?"], [new RuleId(5), RuleId.End]);

        IFindNextMatchResult? result = rule.FindNextMatchSync("aay", 0, FindOption.None);

        Assert.NotNull(result);
        Assert.Equal(RuleId.End, result.RuleId);
        Assert.Equal([new OnigCaptureIndex(2, 3), new OnigCaptureIndex(2, 3), new OnigCaptureIndex(-1, -1)], result.CaptureIndices);
        Assert.Equal(new RuleId(5), rule.FindNextMatchSync(Lib.CreateOnigString("xy"), 0, FindOption.None)!.RuleId);
        Assert.Null(rule.FindNextMatchSync("abc", 0, FindOption.None));
        Assert.Equal("   - 5: x+\n   - -1: (y)(z)?", rule.ToString());
    }

    [Fact]
    public void RuleId_Constants_AreUpstreams()
    {
        Assert.Equal(-1, RuleId.ToNumber(RuleId.End));
        Assert.Equal(-2, RuleId.ToNumber(RuleId.While));
        Assert.Equal(new RuleId(3), RuleId.FromNumber(3));
        Assert.False(DebugFlags.InDebugMode);
        Assert.False(DebugFlags.UseOnigurumaFindOptions);
    }
}
