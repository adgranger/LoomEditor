using LoomEditor.Core.Languages;
using LoomEditor.Core.Tokens;
using LoomEditor.Themes;
using LoomEditor.View.Features;
using LoomEditor.View.Tests.Highlighting.TestHelpers;

namespace LoomEditor.View.Tests.Highlighting;

// No upstream unit tests: textMateTokenizationFeatureImpl.ts is only covered by VS Code's integration tests.
public class SyntaxHighlighterTests
{
    private const string Code = "class A { }";

    private static EditorContext CreateContext(ManualUiDispatcher? dispatcher = null) =>
        new(_ => new NoopDisposable(), TimeProvider.System, dispatcher);

    // The ARGB foreground of the first token whose text is "class".
    private static uint ClassColor(EditorContext context)
    {
        LineTokens tokens = context.Model.Tokenization.GetLineTokens(1);
        for (int i = 0; i < tokens.GetCount(); i++)
        {
            if (tokens.GetTokenText(i) == "class")
            {
                return context.Highlighter.ColorMap![tokens.GetForeground(i)];
            }
        }

        throw new InvalidOperationException("No token 'class'.");
    }

    [Fact]
    public void SetLanguage_Csharp_ModelGetsCsharpTokens()
    {
        using EditorContext context = CreateContext();
        context.SetText(Code);

        context.Highlighter.SetLanguage("csharp");
        context.Model.Tokenization.ForceTokenization(1);

        LineTokens tokens = context.Model.Tokenization.GetLineTokens(1);
        Assert.Equal("csharp", context.Model.Tokenization.LanguageId);
        Assert.True(tokens.GetCount() > 1);
        Assert.Equal("csharp", tokens.GetLanguageId(0));
    }

    [Fact]
    public void SetLanguage_Unknown_FallsBackToPlainText()
    {
        using EditorContext context = CreateContext();
        context.SetText(Code);

        context.Highlighter.SetLanguage("no-such-language");

        Assert.Equal("plaintext", context.Model.Tokenization.LanguageId);
        Assert.Equal(1, context.Model.Tokenization.GetLineTokens(1).GetCount());
    }

    [Fact]
    public void SetTheme_ChangesTokenColors()
    {
        using EditorContext context = CreateContext();
        context.SetText(Code);
        context.Highlighter.SetLanguage("csharp");
        context.Highlighter.SetTheme(EditorTheme.Dark);
        context.Model.Tokenization.ForceTokenization(1);
        uint dark = ClassColor(context);

        context.Highlighter.SetTheme(EditorTheme.Light);
        context.Model.Tokenization.ForceTokenization(1);
        uint light = ClassColor(context);

        Assert.NotEqual(dark, light);
    }

    [Fact]
    public void WithDispatcher_WorkerTokensArriveOnDispatch()
    {
        var dispatcher = new ManualUiDispatcher();
        using EditorContext context = CreateContext(dispatcher);
        context.SetText(string.Join('\n', Enumerable.Repeat(Code, 50)));
        context.Model.AttachView();
        context.Highlighter.SetLanguage("csharp");
        Assert.Equal(1, context.Highlighter.WorkerControllerCount);

        // The worker thread posts its batches to the dispatcher; the UI side runs them.
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (!context.Model.Tokenization.HasAccurateTokensForLine(50) && DateTime.UtcNow < deadline)
        {
            dispatcher.RunAll();
            Thread.Sleep(10);
        }

        Assert.True(context.Model.Tokenization.HasAccurateTokensForLine(50), "The worker tokens did not arrive.");
    }

    [Fact]
    public void Dispose_StopsTheWorker()
    {
        var dispatcher = new ManualUiDispatcher();
        EditorContext context = CreateContext(dispatcher);
        context.Model.AttachView();
        context.Highlighter.SetLanguage("csharp");
        Assert.Equal(1, context.Highlighter.WorkerControllerCount);

        context.Dispose();

        Assert.Equal(0, context.Highlighter.WorkerControllerCount);
    }

    [Fact]
    public void SetLanguage_RegistersTheLanguageConfiguration()
    {
        using EditorContext context = CreateContext();

        context.Highlighter.SetLanguage("csharp");

        Assert.Equal("//", context.Model.LanguageConfigurationRegistry.GetLanguageConfiguration("csharp").Comments?.LineCommentToken);
    }

    [Fact]
    public void SetLanguage_Html_RegistersTheEmbeddedLanguagesConfigurations()
    {
        using EditorContext context = CreateContext();
        LanguageConfigurationRegistry registry = context.Model.LanguageConfigurationRegistry;

        context.Highlighter.SetLanguage("html");

        Assert.Equal("<!--", registry.GetLanguageConfiguration("html").Comments?.BlockCommentStartToken);
        Assert.Equal("/*", registry.GetLanguageConfiguration("css").Comments?.BlockCommentStartToken);
        Assert.Equal("//", registry.GetLanguageConfiguration("javascript").Comments?.LineCommentToken);
    }

    [Fact]
    public void SetLanguage_Twice_RegistersOnce()
    {
        using EditorContext context = CreateContext();
        context.Highlighter.SetLanguage("csharp");
        var changed = new List<string>();
        context.Model.LanguageConfigurationRegistry.Changed += (_, e) => changed.Add(e.LanguageId);

        context.Highlighter.SetLanguage("csharp");

        Assert.Empty(changed);
    }

    [Fact]
    public void Dispose_UnregistersTheConfigurations()
    {
        EditorContext context = CreateContext();
        LanguageConfigurationRegistry registry = context.Model.LanguageConfigurationRegistry;
        context.Highlighter.SetLanguage("csharp");

        context.Dispose();

        Assert.Null(registry.GetLanguageConfiguration("csharp").Comments?.LineCommentToken);
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
