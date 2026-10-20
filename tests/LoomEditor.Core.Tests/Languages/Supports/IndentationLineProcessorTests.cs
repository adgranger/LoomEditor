// Ported from microsoft/vscode: src/vs/editor/contrib/indentation/test/browser/indentationLineProcessor.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Languages.Supports;

public sealed class IndentationLineProcessorTests : IDisposable
{
    private const string LanguageId = TestLanguages.TypeScript;

    private readonly List<IDisposable> _disposables = [];
    private readonly LanguageIdCodec _codec = new();
    private readonly TokenizationRegistry _tokenization = new();
    private readonly LanguageConfigurationRegistry _configurations = new();

    public IndentationLineProcessorTests()
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

    private TextModel CreateModel(string[] lines, StandardTokenTypeData[][] tokens)
    {
        TextModel model = TestLanguages.CreateTextModel(string.Join('\n', lines), LanguageId, _tokenization, _codec, _configurations);
        _disposables.Add(TestLanguages.RegisterTokenizationSupport(_tokenization, _codec, tokens, LanguageId));
        return model;
    }

    private ProcessedIndentRulesSupport CreateProcessedIndentRulesSupport(TextModel model)
    {
        IndentRulesSupport? indentationRulesSupport = _configurations.GetLanguageConfiguration(LanguageId).IndentRulesSupport;
        Assert.NotNull(indentationRulesSupport);
        return new ProcessedIndentRulesSupport(model, indentationRulesSupport, _configurations);
    }

    // Indentation Context Processor - TypeScript/JavaScript

    // Monaco: 'brackets inside of string'
    [Fact]
    public void BracketsInsideOfString()
    {
        TextModel model = CreateModel(
            [
                "const someVar = \"{some text}\"",
            ],
            [[
                new(0, StandardTokenType.Other),
                new(16, StandardTokenType.String),
                new(28, StandardTokenType.String),
            ]]);
        var indentationContextProcessor = new IndentationContextProcessor(model, _configurations);
        ProcessedTokenContext processedContext = indentationContextProcessor.GetProcessedTokenContextAroundRange(new Range(1, 23, 1, 23));
        Assert.Equal("const someVar = \"some", processedContext.BeforeRangeProcessedTokens.GetLineContent());
        Assert.Equal(" text\"", processedContext.AfterRangeProcessedTokens.GetLineContent());
        Assert.Equal("", processedContext.PreviousLineProcessedTokens.GetLineContent());
    }

    // Monaco: 'brackets inside of comment'
    [Fact]
    public void BracketsInsideOfComment()
    {
        TextModel model = CreateModel(
            [
                "const someVar2 = /*(a])*/",
                "const someVar = /* [()] some other t{e}xt() */ \"some text\"",
            ],
            [
                [
                    new(0, StandardTokenType.Other),
                    new(17, StandardTokenType.Comment),
                ],
                [
                    new(0, StandardTokenType.Other),
                    new(16, StandardTokenType.Comment),
                    new(46, StandardTokenType.Other),
                    new(47, StandardTokenType.String),
                ],
            ]);
        var indentationContextProcessor = new IndentationContextProcessor(model, _configurations);
        ProcessedTokenContext processedContext = indentationContextProcessor.GetProcessedTokenContextAroundRange(new Range(2, 29, 2, 35));
        Assert.Equal("const someVar = /*  some", processedContext.BeforeRangeProcessedTokens.GetLineContent());
        Assert.Equal(" text */ \"some text\"", processedContext.AfterRangeProcessedTokens.GetLineContent());
        Assert.Equal("const someVar2 = /*a*/", processedContext.PreviousLineProcessedTokens.GetLineContent());
    }

    // Monaco: 'brackets inside of regex'
    [Fact]
    public void BracketsInsideOfRegex()
    {
        TextModel model = CreateModel(
            [
                "const someRegex2 = /(()))]/;",
                "const someRegex = /()a{h}{s}[(a}87(9a9()))]/;",
            ],
            [
                [
                    new(0, StandardTokenType.Other),
                    new(19, StandardTokenType.RegEx),
                    new(27, StandardTokenType.Other),
                ],
                [
                    new(0, StandardTokenType.Other),
                    new(18, StandardTokenType.RegEx),
                    new(44, StandardTokenType.Other),
                ],
            ]);
        var indentationContextProcessor = new IndentationContextProcessor(model, _configurations);
        ProcessedTokenContext processedContext = indentationContextProcessor.GetProcessedTokenContextAroundRange(new Range(1, 25, 2, 33));
        Assert.Equal("const someRegex2 = /", processedContext.BeforeRangeProcessedTokens.GetLineContent());
        Assert.Equal("879a9/;", processedContext.AfterRangeProcessedTokens.GetLineContent());
        Assert.Equal("", processedContext.PreviousLineProcessedTokens.GetLineContent());
    }

    // Processed Indent Rules Support - TypeScript/JavaScript

    // Monaco: 'should increase'
    [Fact]
    public void ShouldIncrease()
    {
        TextModel model = CreateModel(
            [
                "const someVar = {",
                "const someVar2 = \"{\"",
                "const someVar3 = /*{*/",
            ],
            [
                [
                    new(0, StandardTokenType.Other),
                ],
                [
                    new(0, StandardTokenType.Other),
                    new(17, StandardTokenType.String),
                ],
                [
                    new(0, StandardTokenType.Other),
                    new(17, StandardTokenType.Comment),
                ],
            ]);
        ProcessedIndentRulesSupport processedIndentRulesSupport = CreateProcessedIndentRulesSupport(model);
        Assert.True(processedIndentRulesSupport.ShouldIncrease(1));
        Assert.False(processedIndentRulesSupport.ShouldIncrease(2));
        Assert.False(processedIndentRulesSupport.ShouldIncrease(3));
    }

    // Monaco: 'should decrease'
    [Fact]
    public void ShouldDecrease()
    {
        TextModel model = CreateModel(
            [
                "}",
                "\"])some text}\"",
                "])*/",
            ],
            [
                [new(0, StandardTokenType.Other)],
                [new(0, StandardTokenType.String)],
                [new(0, StandardTokenType.Comment)],
            ]);
        ProcessedIndentRulesSupport processedIndentRulesSupport = CreateProcessedIndentRulesSupport(model);
        Assert.True(processedIndentRulesSupport.ShouldDecrease(1));
        Assert.False(processedIndentRulesSupport.ShouldDecrease(2));
        Assert.False(processedIndentRulesSupport.ShouldDecrease(3));
    }

    // Monaco: 'should increase next line'
    [Fact]
    public void ShouldIncreaseNextLine()
    {
        TextModel model = CreateModel(
            [
                "if()",
                "const someString = \"if()\"",
                "const someRegex = /if()/",
            ],
            [
                [
                    new(0, StandardTokenType.Other),
                ],
                [
                    new(0, StandardTokenType.Other),
                    new(19, StandardTokenType.String),
                ],
                [
                    new(0, StandardTokenType.Other),
                    new(18, StandardTokenType.RegEx),
                ],
            ]);
        ProcessedIndentRulesSupport processedIndentRulesSupport = CreateProcessedIndentRulesSupport(model);
        Assert.True(processedIndentRulesSupport.ShouldIndentNextLine(1));
        Assert.False(processedIndentRulesSupport.ShouldIndentNextLine(2));
        Assert.False(processedIndentRulesSupport.ShouldIndentNextLine(3));
    }

    [Fact]
    public void IsLanguageDifferentFromLineStart_EmbeddedLanguageInTheMiddle_IsTrue()
    {
        const string InnerLanguageId = "inner-test";
        _codec.Register(InnerLanguageId);
        TextModel model = TestLanguages.CreateTextModel("ab<xx>ab", LanguageId, _tokenization, _codec, _configurations);
        _disposables.Add(_tokenization.Register(LanguageId, new EmbeddedTokenizationSupport(_codec, LanguageId, InnerLanguageId)));

        Assert.True(IndentationContextProcessor.IsLanguageDifferentFromLineStart(model, new Position(1, 5)));
        Assert.False(IndentationContextProcessor.IsLanguageDifferentFromLineStart(model, new Position(1, 2)));
    }

    [Fact]
    public void GetProcessedLine_NewIndentation_ReplacesTheLeadingWhitespace()
    {
        TextModel model = CreateModel(
            [
                "    foo(\"{\")",
            ],
            [[
                new(0, StandardTokenType.Other),
                new(8, StandardTokenType.String),
                new(11, StandardTokenType.Other),
            ]]);
        var processor = new IndentationLineProcessor(model, _configurations);

        Assert.Equal("\tfoo(\"\")", processor.GetProcessedLine(1, "\t"));
    }

    /// <summary>Tokenizes the <c>x</c> characters in the inner language and the others in the outer language.</summary>
    private sealed class EmbeddedTokenizationSupport(ILanguageIdCodec codec, string outerLanguageId, string innerLanguageId) : ITokenizationSupport
    {
        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
        {
            var tokens = new List<uint>();
            string? previousLanguageId = null;
            for (int i = 0; i < line.Length; i++)
            {
                string languageId = line[i] == 'x' ? innerLanguageId : outerLanguageId;
                if (previousLanguageId != languageId)
                {
                    tokens.Add((uint)i);
                    tokens.Add((uint)codec.EncodeLanguageId(languageId) << MetadataConsts.LanguageIdOffset);
                }

                previousLanguageId = languageId;
            }

            return new EncodedTokenizationResult([.. tokens], state);
        }
    }
}
