using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Grammars;
using LoomEditor.TextMate.Oniguruma;
using LoomEditor.TextMate.Tokenization;
using LoomEditor.Themes;
using LoomEditor.View.Highlighting;

namespace LoomEditor.TextMate.Tests.Golden;

// The phase 4 "done" criterion. No upstream test: the expected files are produced by the real vscode-textmate 9.3.2 and
// vscode-oniguruma 1.7.0 (eng/golden/generate.mjs) from the same grammar and theme files.
public class GoldenTests
{
    public static TheoryData<string> LanguageData() => [.. GoldenFile.Languages];

    public static TheoryData<string, string> ThemeData()
    {
        var data = new TheoryData<string, string>();
        foreach (string language in GoldenFile.Languages)
        {
            data.Add(language, "darkModern");
            data.Add(language, "lightModern");
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(LanguageData))]
    public void Golden_Scopes(string language)
    {
        using GrammarFixture fixture = GrammarFixture.Create(language, EditorTheme.Dark);
        IReadOnlyList<GoldenLine> expected = GoldenFile.Read(language);
        HashSet<int> skipped = GoldenFile.KnownDifferences(language);

        StateStack stack = fixture.InitialState;
        for (int i = 0; i < expected.Count; i++)
        {
            TokenizeLineResult result = fixture.Grammar.TokenizeLine(expected[i].Text, stack);
            stack = result.RuleStack;
            if (skipped.Contains(i + 1))
            {
                continue;
            }

            List<GoldenToken> actual = [.. result.Tokens.Select(t => new GoldenToken(t.StartIndex, t.EndIndex, string.Join(' ', t.Scopes)))];
            AssertSame(language, i + 1, expected[i].Text, expected[i].Tokens, actual);
        }
    }

    [Theory]
    [MemberData(nameof(ThemeData))]
    public void Golden_Colors(string language, string themeKey)
    {
        EditorTheme theme = themeKey == "darkModern" ? EditorTheme.Dark : EditorTheme.Light;
        using GrammarFixture fixture = GrammarFixture.Create(language, theme);
        IReadOnlyList<GoldenLine> expected = GoldenFile.Read(language);
        HashSet<int> skipped = GoldenFile.KnownDifferences(language);
        IReadOnlyList<string> colorMap = ThemeToTextMate.ToTextColorMap(theme);

        StateStack stack = fixture.InitialState;
        for (int i = 0; i < expected.Count; i++)
        {
            TokenizeLineResult2 result = fixture.Grammar.TokenizeLine2(expected[i].Text, stack);
            stack = result.RuleStack;
            if (skipped.Contains(i + 1))
            {
                continue;
            }

            var actual = new List<GoldenColor>();
            for (int t = 0; t < result.Tokens.Length; t += 2)
            {
                uint metadata = result.Tokens[t + 1];
                int foreground = EncodedTokenAttributes.GetForeground(metadata);
                actual.Add(new GoldenColor((int)result.Tokens[t], foreground < colorMap.Count ? colorMap[foreground] : string.Empty, FontStyleText(EncodedTokenAttributes.GetFontStyle(metadata))));
            }

            AssertSame(language, i + 1, expected[i].Text, expected[i].Colors[themeKey], actual);
        }
    }

    private static string FontStyleText(int fontStyle)
    {
        var names = new List<string>();
        if ((fontStyle & 1) != 0)
        {
            names.Add("italic");
        }

        if ((fontStyle & 2) != 0)
        {
            names.Add("bold");
        }

        if ((fontStyle & 4) != 0)
        {
            names.Add("underline");
        }

        if ((fontStyle & 8) != 0)
        {
            names.Add("strikethrough");
        }

        return string.Join(' ', names);
    }

    // A failure names the sample, the line, its text and the first token that differs.
    private static void AssertSame<T>(string language, int lineNumber, string text, IReadOnlyList<T> expected, IReadOnlyList<T> actual)
    {
        for (int i = 0; i < Math.Max(expected.Count, actual.Count); i++)
        {
            T? e = i < expected.Count ? expected[i] : default;
            T? a = i < actual.Count ? actual[i] : default;
            if (!EqualityComparer<T>.Default.Equals(e, a))
            {
                Assert.Fail($"{language} sample, line {lineNumber} `{text}`, token {i}:{Environment.NewLine}  expected {e}{Environment.NewLine}  actual   {a}");
            }
        }
    }

    // The grammar of one language, made the way the editor makes it: BuiltInLanguages, TMGrammarFactory, the theme of an EditorTheme.
    private sealed class GrammarFixture : IDisposable
    {
        private readonly TMGrammarFactory _factory;

        private GrammarFixture(TMGrammarFactory factory, IGrammar grammar, StateStack initialState)
        {
            _factory = factory;
            Grammar = grammar;
            InitialState = initialState;
        }

        public IGrammar Grammar { get; }

        public StateStack InitialState { get; }

        public static GrammarFixture Create(string language, EditorTheme theme)
        {
            var languages = new LanguageRegistry();
            BuiltInLanguages.RegisterAll(languages);
            var factory = new TMGrammarFactory(languages.CreateGrammarDefinitions(), BuiltInLanguages.ReadFile, new RawGrammarCache(), new OnigurumaEngine());
            factory.SetTheme(ThemeToTextMate.ToRawTheme(theme), ThemeToTextMate.ToTextColorMap(theme));
            CreateGrammarResult result = factory.CreateGrammar(language, languages.Codec.EncodeLanguageId(language));
            return new GrammarFixture(factory, result.Grammar ?? throw new InvalidOperationException($"No grammar for {language}."), StateStack.Initial);
        }

        public void Dispose() => _factory.Dispose();
    }
}
