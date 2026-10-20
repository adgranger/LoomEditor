using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Languages;

public sealed class AutoIndentInheritTests : IDisposable
{
    private const string LanguageId = TestLanguages.TypeScript;

    private readonly List<IDisposable> _disposables = [];
    private readonly LanguageIdCodec _codec = new();
    private readonly TokenizationRegistry _tokenization = new();
    private readonly LanguageConfigurationRegistry _configurations = new();

    public AutoIndentInheritTests()
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
    public void GetInheritIndentForLine_BelowFull_ReturnsNull()
    {
        TextModel model = CreateModel("  if (a) {", "x");

        Assert.Null(AutoIndent.GetInheritIndentForLine(EditorAutoIndentStrategy.Advanced, model, 2, true, _configurations));
    }

    [Fact]
    public void GetInheritIndentForLine_AfterOpenBrace_IsIndent()
    {
        TextModel model = CreateModel("  if (a) {", "x");

        InheritIndentResult? result = AutoIndent.GetInheritIndentForLine(EditorAutoIndentStrategy.Full, model, 2, true, _configurations);

        Assert.Equal(new InheritIndentResult("  ", IndentAction.Indent, 1), result);
    }

    [Fact]
    public void GetInheritIndentForLine_FirstNonBlankLine_HasNoIndentation()
    {
        TextModel model = CreateModel("", "", "    x");

        InheritIndentResult? result = AutoIndent.GetInheritIndentForLine(EditorAutoIndentStrategy.Full, model, 3, true, _configurations);

        Assert.Equal(new InheritIndentResult("", null), result);
    }

    [Fact]
    public void GetInheritIndentForLine_AfterIndentNextLine_UsesTheLineBefore()
    {
        TextModel model = CreateModel("  const a = 1;", "  if (a)", "      foo();", "x");

        InheritIndentResult? result = AutoIndent.GetInheritIndentForLine(EditorAutoIndentStrategy.Full, model, 4, true, _configurations);

        Assert.Equal(new InheritIndentResult("  ", null, 2), result);
    }

    [Fact]
    public void GetGoodIndentForLine_ClosingBrace_IsUnshifted()
    {
        TextModel model = CreateModel("function f() {", "    foo();", "}");

        string? indentation = AutoIndent.GetGoodIndentForLine(EditorAutoIndentStrategy.Full, model, LanguageId, 3, new TestIndentConverter(), _configurations);

        Assert.Equal("", indentation);
    }

    [Fact]
    public void GetGoodIndentForLine_AfterOnEnterRule_AppliesTheEnterAction()
    {
        // The rule of '/** ...|' appends " * ": the indentation gains its leading space.
        TextModel model = CreateModel("  /**", "x");

        string? indentation = AutoIndent.GetGoodIndentForLine(EditorAutoIndentStrategy.Full, model, LanguageId, 2, new TestIndentConverter(), _configurations);

        Assert.Equal("   ", indentation);
    }

    private TextModel CreateModel(params string[] lines)
    {
        TextModel model = TestLanguages.CreateTextModel(string.Join('\n', lines), LanguageId, _tokenization, _codec, _configurations);
        StandardTokenTypeData[][] tokens = [.. lines.Select(_ => new StandardTokenTypeData[] { new(0, StandardTokenType.Other) })];
        _disposables.Add(TestLanguages.RegisterTokenizationSupport(_tokenization, _codec, tokens, LanguageId));
        return model;
    }
}
