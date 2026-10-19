// Ported from microsoft/vscode-textmate: src/tests/tokenization.test.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.Json;
using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Tests.TestHelpers;

namespace LoomEditor.TextMate.Tests.Grammar;

/// <summary>
/// <c>assertTokenizationSuite</c>: runs the data-driven cases of a <c>tests.json</c> (<c>TestCases/</c>) against the
/// tokenizer. A case names the grammars to load, the grammar to tokenize with, optional injections, and for each line
/// the expected <c>{ value, scopes }</c> tokens.
/// </summary>
internal static class TokenizationSuiteRunner
{
    private sealed record Token(string Value, string[] Scopes);

    private sealed record TestLine(string Line, List<Token> Tokens);

    private sealed record TestCase(
        string Desc,
        List<string> Grammars,
        string? GrammarPath,
        string? GrammarScopeName,
        List<string>? GrammarInjections,
        List<TestLine> Lines);

    private static readonly Dictionary<string, List<TestCase>> Suites = [];
    private static readonly Dictionary<string, RawGrammar> ParsedGrammars = [];

    /// <summary>One theory row per case of <paramref name="testsFile"/> (e.g. <c>first-mate/tests.json</c>), named by its <c>desc</c>.</summary>
    public static IEnumerable<TheoryDataRow<string, int>> Rows(string testsFile)
    {
        List<TestCase> cases = Load(testsFile);
        for (int i = 0; i < cases.Count; i++)
        {
            yield return new TheoryDataRow<string, int>(testsFile, i) { TestDisplayName = cases[i].Desc };
        }
    }

    /// <summary><c>performTest</c> for case <paramref name="index"/> of <paramref name="testsFile"/>.</summary>
    public static void Run(string testsFile, int index)
    {
        TestCase test = Load(testsFile)[index];
        string directory = Path.GetDirectoryName(testsFile)!.Replace('\\', '/');

        string? grammarScopeName = test.GrammarScopeName;
        var grammarByScope = new Dictionary<string, RawGrammar>(StringComparer.Ordinal);
        foreach (string grammarPath in test.Grammars)
        {
            RawGrammar rawGrammar = ParseGrammar($"{directory}/{grammarPath}", grammarPath);
            grammarByScope[rawGrammar.ScopeName!] = rawGrammar;
            if (string.IsNullOrEmpty(grammarScopeName) && grammarPath == test.GrammarPath)
            {
                grammarScopeName = rawGrammar.ScopeName;
            }
        }

        if (string.IsNullOrEmpty(grammarScopeName))
        {
            throw new InvalidOperationException("I HAVE NO GRAMMAR FOR TEST");
        }

        var options = new RegistryOptions(
            OnigTestLib.GetOniguruma(),
            scopeName => grammarByScope.GetValueOrDefault(scopeName),
            GetInjections: scopeName => scopeName == grammarScopeName ? test.GrammarInjections : null);
        using var registry = new Registry(options);
        IGrammar grammar = registry.LoadGrammar(grammarScopeName)
            ?? throw new InvalidOperationException("I HAVE NO GRAMMAR FOR TEST");

        StateStack? prevState = null;
        foreach (TestLine line in test.Lines)
        {
            prevState = AssertLineTokenization(grammar, line, prevState);
        }
    }

    private static StateStack AssertLineTokenization(IGrammar grammar, TestLine testCase, StateStack? prevState)
    {
        TokenizeLineResult actual = grammar.TokenizeLine(testCase.Line, prevState);

        List<Token> actualTokens = [.. actual.Tokens.Select(token => new Token(
            Substring(testCase.Line, token.StartIndex, token.EndIndex),
            [.. token.Scopes]))];

        List<Token> expectedTokens = testCase.Tokens;
        // TODO@Alex: fix tests instead of working around (upstream)
        if (testCase.Line.Length > 0)
        {
            // Remove empty tokens...
            expectedTokens = [.. expectedTokens.Where(token => token.Value.Length > 0)];
        }

        Assert.True(
            actualTokens.Count == expectedTokens.Count && actualTokens.Zip(expectedTokens).All(p => Same(p.First, p.Second)),
            $"Tokenizing line '{testCase.Line}'\nexpected: {Format(expectedTokens)}\nactual:   {Format(actualTokens)}");

        StateStack ruleStack = actual.RuleStack;
        if (prevState is not null)
        {
            StackDiff diff = DiffStateStacks.DiffStateStacksRefEq(prevState, actual.RuleStack);
            ruleStack = DiffStateStacks.ApplyStateStackDiff(prevState, diff)!;
        }

        return ruleStack;
    }

    // JavaScript's substring clamps both indices to the string length.
    private static string Substring(string line, int start, int end)
    {
        start = Math.Min(start, line.Length);
        end = Math.Min(end, line.Length);
        return line[start..end];
    }

    private static bool Same(Token a, Token b) => a.Value == b.Value && a.Scopes.AsSpan().SequenceEqual(b.Scopes);

    private static string Format(List<Token> tokens) =>
        string.Join(", ", tokens.Select(t => $"{{'{t.Value}' [{string.Join(", ", t.Scopes)}]}}"));

    private static RawGrammar ParseGrammar(string relativePath, string grammarPath)
    {
        // Parsed once per file: the grammar works on a clone, so sharing the raw grammar between cases is safe.
        if (!ParsedGrammars.TryGetValue(relativePath, out RawGrammar? grammar))
        {
            grammar = Registry.ParseRawGrammar(TestCaseFiles.ReadAllText(relativePath), grammarPath);
            ParsedGrammars[relativePath] = grammar;
        }

        return grammar;
    }

    private static List<TestCase> Load(string testsFile)
    {
        lock (Suites)
        {
            if (!Suites.TryGetValue(testsFile, out List<TestCase>? cases))
            {
                using JsonDocument document = JsonDocument.Parse(TestCaseFiles.ReadAllText(testsFile));
                cases = [.. document.RootElement.EnumerateArray().Select(ReadCase)];
                Suites[testsFile] = cases;
            }

            return cases;
        }
    }

    private static TestCase ReadCase(JsonElement element) => new(
        element.GetProperty("desc").GetString()!,
        ReadStrings(element.GetProperty("grammars"))!,
        OptionalString(element, "grammarPath"),
        OptionalString(element, "grammarScopeName"),
        element.TryGetProperty("grammarInjections", out JsonElement injections) ? ReadStrings(injections) : null,
        [.. element.GetProperty("lines").EnumerateArray().Select(line => new TestLine(
            line.GetProperty("line").GetString()!,
            [.. line.GetProperty("tokens").EnumerateArray().Select(token => new Token(
                token.GetProperty("value").GetString()!,
                [.. ReadStrings(token.GetProperty("scopes"))!]))]))]);

    private static string? OptionalString(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) ? value.GetString() : null;

    private static List<string>? ReadStrings(JsonElement array) => [.. array.EnumerateArray().Select(e => e.GetString()!)];
}
