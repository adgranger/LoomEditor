using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Languages;

public sealed class AutoIndentEnterAndTypeTests : IDisposable
{
    private const string LanguageId = TestLanguages.TypeScript;

    private readonly List<IDisposable> _disposables = [];
    private readonly LanguageIdCodec _codec = new();
    private readonly TokenizationRegistry _tokenization = new();
    private readonly LanguageConfigurationRegistry _configurations = new();

    public AutoIndentEnterAndTypeTests()
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
    public void GetIndentForEnter_BelowFull_ReturnsNull()
    {
        TextModel model = CreateModel("  if (a) {");

        Assert.Null(AutoIndent.GetIndentForEnter(EditorAutoIndentStrategy.Advanced, model, new Range(1, 11, 1, 11), new TestIndentConverter(), _configurations));
    }

    [Fact]
    public void GetIndentForEnter_AfterOpenBrace_ShiftsTheNewLine()
    {
        TextModel model = CreateModel("  if (a) {");

        var result = AutoIndent.GetIndentForEnter(EditorAutoIndentStrategy.Full, model, new Range(1, 11, 1, 11), new TestIndentConverter(), _configurations);

        Assert.Equal(("  ", "      "), result);
    }

    [Fact]
    public void GetIndentForEnter_BeforeClosingBrace_UnshiftsTheNewLine()
    {
        TextModel model = CreateModel("  if (a) {}");

        var result = AutoIndent.GetIndentForEnter(EditorAutoIndentStrategy.Full, model, new Range(1, 11, 1, 11), new TestIndentConverter(), _configurations);

        Assert.Equal(("  ", "  "), result);
    }

    [Fact]
    public void GetIndentActionForType_ClosingBrace_Outdents()
    {
        TextModel model = CreateModel("function f() {", "    foo();", "    ");

        string? indentation = AutoIndent.GetIndentActionForType(EditorAutoIndentStrategy.Full, AutoClosingPairs(), model, new Range(3, 5, 3, 5), "}", new TestIndentConverter(), _configurations);

        Assert.Equal("", indentation);
    }

    [Fact]
    public void GetIndentActionForType_AlreadyDecreased_ReturnsNull()
    {
        // The line already matches decreaseIndentPattern: its indentation is intentional and is kept.
        TextModel model = CreateModel("function f() {", "    foo();", "    }");

        string? indentation = AutoIndent.GetIndentActionForType(EditorAutoIndentStrategy.Full, AutoClosingPairs(), model, new Range(3, 6, 3, 6), "}", new TestIndentConverter(), _configurations);

        Assert.Null(indentation);
    }

    [Fact]
    public void GetIndentActionForType_OpenBraceAfterIndentNextLine_UsesTheInheritedIndentation()
    {
        TextModel model = CreateModel("if (a)", "    ");

        string? indentation = AutoIndent.GetIndentActionForType(EditorAutoIndentStrategy.Full, AutoClosingPairs(), model, new Range(2, 5, 2, 5), "{", new TestIndentConverter(), _configurations);

        Assert.Equal("", indentation);
    }

    [Fact]
    public void GetIndentMetadata_OutOfRangeLine_ReturnsNull()
    {
        TextModel model = CreateModel("if (a) {", "}");

        Assert.Null(AutoIndent.GetIndentMetadata(model, 0, _configurations));
        Assert.Null(AutoIndent.GetIndentMetadata(model, 3, _configurations));
        Assert.Equal(IndentConsts.IncreaseMask, AutoIndent.GetIndentMetadata(model, 1, _configurations));
    }

    private AutoClosingPairs AutoClosingPairs() => _configurations.GetLanguageConfiguration(LanguageId).GetAutoClosingPairs();

    private TextModel CreateModel(params string[] lines)
    {
        TextModel model = TestLanguages.CreateTextModel(string.Join('\n', lines), LanguageId, _tokenization, _codec, _configurations);
        StandardTokenTypeData[][] tokens = [.. lines.Select(_ => new StandardTokenTypeData[] { new(0, StandardTokenType.Other) })];
        _disposables.Add(TestLanguages.RegisterTokenizationSupport(_tokenization, _codec, tokens, LanguageId));
        return model;
    }
}
