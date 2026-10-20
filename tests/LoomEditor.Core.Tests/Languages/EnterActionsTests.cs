using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Languages;

public sealed class EnterActionsTests : IDisposable
{
    private const string LanguageId = TestLanguages.TypeScript;

    private readonly List<IDisposable> _disposables = [];
    private readonly LanguageIdCodec _codec = new();
    private readonly TokenizationRegistry _tokenization = new();
    private readonly LanguageConfigurationRegistry _configurations = new();

    public EnterActionsTests()
    {
        TestLanguages.RegisterLanguage(_codec, LanguageId);
        _disposables.Add(TestLanguages.RegisterLanguageConfiguration(_configurations, LanguageId));
    }

    public void Dispose()
    {
        foreach (IDisposable disposable in _disposables)
        {
            disposable.Dispose();
        }
    }

    [Fact]
    public void GetEnterAction_BetweenBraces_IsIndentOutdentWithTab()
    {
        TextModel model = CreateModel("    function f() {}");

        CompleteEnterAction? action = EnterActions.GetEnterAction(EditorAutoIndentStrategy.Full, model, new Range(1, 19, 1, 19), _configurations);

        Assert.Equal(new CompleteEnterAction(IndentAction.IndentOutdent, "\t", 0, "    "), action);
    }

    [Fact]
    public void GetEnterAction_AfterDocCommentStart_AppendsStar()
    {
        TextModel model = CreateModel("  /**");

        CompleteEnterAction? action = EnterActions.GetEnterAction(EditorAutoIndentStrategy.Full, model, new Range(1, 6, 1, 6), _configurations);

        Assert.Equal(new CompleteEnterAction(IndentAction.None, " * ", 0, "  "), action);
    }

    [Fact]
    public void GetEnterAction_RemoveText_ShortensTheIndentation()
    {
        TextModel model = CreateModel("     */");

        CompleteEnterAction? action = EnterActions.GetEnterAction(EditorAutoIndentStrategy.Full, model, new Range(1, 8, 1, 8), _configurations);

        Assert.Equal(new CompleteEnterAction(IndentAction.None, "", 1, "    "), action);
    }

    [Fact]
    public void GetEnterAction_NoRuleMatches_ReturnsNull()
    {
        TextModel model = CreateModel("const x = 1;");

        CompleteEnterAction? action = EnterActions.GetEnterAction(EditorAutoIndentStrategy.Full, model, new Range(1, 13, 1, 13), _configurations);

        Assert.Null(action);
    }

    private TextModel CreateModel(string line)
    {
        TextModel model = TestLanguages.CreateTextModel(line, LanguageId, _tokenization, _codec, _configurations);
        _disposables.Add(TestLanguages.RegisterTokenizationSupport(_tokenization, _codec, [[new(0, StandardTokenType.Other)]], LanguageId));
        return model;
    }
}
