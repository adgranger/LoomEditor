// Ported from microsoft/vscode: src/vs/editor/test/common/modes/languageConfiguration.test.ts (suite 'StandardAutoClosingPairConditional')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;

namespace LoomEditor.Core.Tests.Languages;

// The first test is ported; the others are new.
public class LanguageConfigurationRegistryTests
{
    private static LanguageConfiguration LineComment(string token) =>
        new() { Comments = new CommentRule(new LineCommentConfig(token)) };

    // Monaco: 'language configurations priorities'
    [Fact]
    public void LanguageConfigurationsPriorities()
    {
        var languageConfigurationService = new LanguageConfigurationRegistry();
        const string id = "testLang1";
        IDisposable d1 = languageConfigurationService.Register(id, LineComment("1"), 100);
        IDisposable d2 = languageConfigurationService.Register(id, LineComment("2"), 10);
        Assert.Equal("1", languageConfigurationService.GetLanguageConfiguration(id).Comments?.LineCommentToken);
        d1.Dispose();
        d2.Dispose();
    }

    [Fact]
    public void GetLanguageConfiguration_Plaintext_HasTheDefaultBracketsAndSurroundingPairs()
    {
        var registry = new LanguageConfigurationRegistry();

        ResolvedLanguageConfiguration config = registry.GetLanguageConfiguration("plaintext");

        Assert.Equal(new CharacterPair[] { new("(", ")"), new("[", "]"), new("{", "}") }, config.UnderlyingConfig.Brackets);
        Assert.Equal("{} [] () <> \"\" '' ``", string.Join(' ', config.GetSurroundingPairs().Select(p => p.Open + p.Close)));
        Assert.True(config.FoldingRules.OffSide);
        Assert.NotNull(config.Brackets);
        Assert.Same(config, registry.GetLanguageConfiguration("plaintext"));
    }

    [Fact]
    public void GetLanguageConfiguration_UnknownLanguage_IsEmpty()
    {
        var registry = new LanguageConfigurationRegistry();

        ResolvedLanguageConfiguration config = registry.GetLanguageConfiguration("unknown");

        Assert.Equal("unknown", config.LanguageId);
        Assert.Null(config.UnderlyingConfig.Brackets);
        Assert.Null(config.Comments);
        Assert.Null(config.Brackets);
        Assert.Empty(config.GetSurroundingPairs());
        Assert.Null(config.OnEnter(EditorAutoIndentStrategy.Full, "", "{", "}"));
        Assert.Same(config, registry.GetLanguageConfiguration("unknown"));
    }

    [Fact]
    public void Register_RaisesChanged_AndRecomputes()
    {
        var registry = new LanguageConfigurationRegistry();
        ResolvedLanguageConfiguration before = registry.GetLanguageConfiguration("test");
        var events = new List<LanguageConfigurationChangedEventArgs>();
        registry.Changed += (_, e) =>
        {
            events.Add(e);
            Assert.NotSame(before, registry.GetLanguageConfiguration("test"));
        };

        registry.Register("test", LineComment("//"));

        LanguageConfigurationChangedEventArgs args = Assert.Single(events);
        Assert.True(args.Affects("test"));
        Assert.False(args.Affects("other"));
        Assert.Equal("//", registry.GetLanguageConfiguration("test").Comments?.LineCommentToken);
    }

    [Fact]
    public void DisposeRegistration_RaisesChanged_AndRestoresTheOtherEntry()
    {
        var registry = new LanguageConfigurationRegistry();
        registry.Register("test", LineComment("#"));
        IDisposable registration = registry.Register("test", LineComment("//"));
        Assert.Equal("//", registry.GetLanguageConfiguration("test").Comments?.LineCommentToken);
        var events = new List<string>();
        registry.Changed += (_, e) => events.Add(e.LanguageId);

        registration.Dispose();
        registration.Dispose();

        Assert.Equal("test", Assert.Single(events));
        Assert.Equal("#", registry.GetLanguageConfiguration("test").Comments?.LineCommentToken);
    }

    [Fact]
    public void Combine_EmptyAutoCloseBefore_FallsBack()
    {
        var registry = new LanguageConfigurationRegistry();
        registry.Register("test", new LanguageConfiguration { AutoCloseBefore = ";" });
        registry.Register("test", new LanguageConfiguration { AutoCloseBefore = "" });

        Assert.Equal(";", registry.GetLanguageConfiguration("test").GetAutoCloseBeforeSet(forQuotes: true));
    }

    [Fact]
    public void GetIndentationAtPosition_CutsAtTheColumn()
    {
        var model = new TextModel("\t  foo\nbar");

        Assert.Equal("\t ", LanguageConfigurationRegistry.GetIndentationAtPosition(model, 1, 3));
        Assert.Equal("\t  ", LanguageConfigurationRegistry.GetIndentationAtPosition(model, 1, 10));
        Assert.Equal("", LanguageConfigurationRegistry.GetIndentationAtPosition(model, 2, 1));
    }
}
