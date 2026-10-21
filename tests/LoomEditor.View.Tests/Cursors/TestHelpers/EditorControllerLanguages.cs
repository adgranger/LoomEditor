// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller': setup, helpers)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Cursors.TestHelpers;

/// <summary>The options of <see cref="EditorControllerLanguages.UsingCursor"/> (Monaco: <c>ICursorOpts</c>).</summary>
internal sealed record CursorOpts(
    string[] Text,
    string? LanguageId = null,
    TextModelOptions? ModelOpts = null,
    EditorCursorOptions? EditorOpts = null);

/// <summary>What <see cref="EditorControllerLanguages.ExtractAutoClosingSpecialColumns"/> says about a column.</summary>
internal enum AutoClosingColumnType
{
    Normal = 0,
    Special1 = 1,
    Special2 = 2,
}

/// <summary>
/// The setup of the suite <c>Editor Controller</c>: its five test languages and its helpers. Create one per test, with
/// <c>using</c> (Monaco: <c>setup</c> and <c>teardown</c>).
/// </summary>
/// <remarks>Threading: use from one test thread.</remarks>
internal sealed class EditorControllerLanguages : IDisposable
{
    public const string SurroundingLanguageId = "surroundingLanguage";
    public const string IndentRulesLanguageId = "indentRulesLanguage";
    public const string ElectricCharLanguageId = "electricCharLanguage";
    public const string AutoClosingLanguageId = "autoClosingLanguage";
    public const string EmptyClosingSurroundLanguageId = "emptyClosingSurroundLanguage";

    private const RegexOptions Js = RegexOptions.CultureInvariant;

    public EditorControllerLanguages()
    {
        Services.RegisterLanguage(SurroundingLanguageId);
        Services.Add(Services.LanguageConfigurationRegistry.Register(SurroundingLanguageId, new LanguageConfiguration
        {
            AutoClosingPairs = [new AutoClosingPairConditional("(", ")")],
        }));

        Services.RegisterLanguage(EmptyClosingSurroundLanguageId);
        Services.Add(Services.LanguageConfigurationRegistry.Register(EmptyClosingSurroundLanguageId, new LanguageConfiguration
        {
            SurroundingPairs = [new AutoClosingPair("<", string.Empty)],
        }));

        SetupIndentRulesLanguage(IndentRulesLanguageId, new IndentationRule(
            DecreaseIndentPattern: new Regex(@"^\s*((?!\S.*\/[*]).*[*]\/\s*)?[})\]]|^\s*(case\b.*|default):\s*(\/\/.*|\/[*].*[*]\/\s*)?$", Js),
            IncreaseIndentPattern: new Regex(@"^((?!\/\/).)*(\{[^}""'`]*|\([^)""']*|\[[^\]""']*|^\s*(\{\}|\(\)|\[\]|(case\b.*|default):))\s*(\/\/.*|\/[*].*[*]\/\s*)?$", Js),
            IndentNextLinePattern: new Regex(@"^\s*(for|while|if|else)\b(?!.*[;{}]\s*(\/\/.*|\/[*].*[*]\/\s*)?$)", Js),
            UnIndentedLinePattern: new Regex(@"^(?!.*([;{}]|\S:)\s*(\/\/.*|\/[*].*[*]\/\s*)?$)(?!.*(\{[^}""']*|\([^)""']*|\[[^\]""']*|^\s*(\{\}|\(\)|\[\]|(case\b.*|default):))\s*(\/\/.*|\/[*].*[*]\/\s*)?$)(?!^\s*((?!\S.*\/[*]).*[*]\/\s*)?[})\]]|^\s*(case\b.*|default):\s*(\/\/.*|\/[*].*[*]\/\s*)?$)(?!^\s*(for|while|if|else)\b(?!.*[;{}]\s*(\/\/.*|\/[*].*[*]\/\s*)?$))", Js)));

        Services.RegisterLanguage(ElectricCharLanguageId);
        Services.Add(Services.LanguageConfigurationRegistry.Register(ElectricCharLanguageId, new LanguageConfiguration
        {
            // Monaco: __electricCharacterSupport: { docComment }. It has no effect there either (BracketElectricCharacterSupport ignores it).
            DocComment = new DocComment("/**", " */"),
            Brackets = [new("{", "}"), new("[", "]"), new("(", ")")],
        }));

        SetupAutoClosingLanguage();
    }

    public TestEditorServices Services { get; } = new();

    public void Dispose() => Services.Dispose();

    public string SetupOnEnterLanguage(IndentAction indentAction)
    {
        const string OnEnterLanguageId = "onEnterMode";

        Services.RegisterLanguage(OnEnterLanguageId);
        Services.Add(Services.LanguageConfigurationRegistry.Register(OnEnterLanguageId, new LanguageConfiguration
        {
            OnEnterRules = [new OnEnterRule(new Regex(".*", Js), new EnterAction(indentAction))],
        }));
        return OnEnterLanguageId;
    }

    public string SetupIndentRulesLanguage(string languageId, IndentationRule indentationRules)
    {
        Services.RegisterLanguage(languageId);
        Services.Add(Services.LanguageConfigurationRegistry.Register(languageId, new LanguageConfiguration
        {
            IndentationRules = indentationRules,
        }));
        return languageId;
    }

    private void SetupAutoClosingLanguage()
    {
        Services.RegisterLanguage(AutoClosingLanguageId);
        Services.Add(Services.LanguageConfigurationRegistry.Register(AutoClosingLanguageId, new LanguageConfiguration
        {
            Comments = new CommentRule(BlockComment: new CharacterPair("/*", "*/")),
            AutoClosingPairs =
            [
                new("{", "}"),
                new("[", "]"),
                new("(", ")"),
                new("'", "'", ["string", "comment"]),
                new("\"", "\"", ["string"]),
                new("`", "`", ["string", "comment"]),
                new("/**", " */", ["string"]),
                new("begin", "end", ["string"]),
            ],
            DocComment = new DocComment("/**", " */"),
        }));
    }

    public void SetupAutoClosingLanguageTokenization()
    {
        int encodedLanguageId = Services.LanguageIdCodec.EncodeLanguageId(AutoClosingLanguageId);
        Services.Add(Services.TokenizationRegistry.Register(AutoClosingLanguageId, new AutoClosingLanguageTokenizationSupport(encodedLanguageId)));
    }

    public void SetAutoClosingLanguageEnabledSet(string chars) =>
        Services.Add(Services.LanguageConfigurationRegistry.Register(AutoClosingLanguageId, new LanguageConfiguration
        {
            AutoCloseBefore = chars,
            AutoClosingPairs =
            [
                new("{", "}"),
                new("[", "]"),
                new("(", ")"),
                new("'", "'", ["string", "comment"]),
                new("\"", "\"", ["string"]),
                new("`", "`", ["string", "comment"]),
                new("/**", " */", ["string"]),
            ],
        }));

    public TextModel CreateTextModel(string text, string? languageId = null, TextModelOptions? options = null) =>
        Services.CreateTextModel(text, languageId, options);

    public static void WithTestCodeEditor(TextModel model, TestCodeEditorOptions? options, Action<TestCodeEditor, ViewModel> callback) =>
        TestCodeEditor.WithTestCodeEditor(model, editor => callback(editor, editor.ViewModel), options);

    public void WithTestCodeEditor(string text, TestCodeEditorOptions? options, Action<TestCodeEditor, ViewModel> callback) =>
        WithTestCodeEditor(CreateTextModel(text), options, callback);

    public void UsingCursor(CursorOpts opts, Action<TestCodeEditor, TextModel, ViewModel> callback)
    {
        TextModel model = CreateTextModel(string.Join('\n', opts.Text), opts.LanguageId, opts.ModelOpts);
        var editorOptions = new TestCodeEditorOptions(CursorOptions: opts.EditorOpts);
        WithTestCodeEditor(model, editorOptions, (editor, viewModel) => callback(editor, model, viewModel));
    }

    /// <summary>Monaco: <c>extractAutoClosingSpecialColumns</c>. The result is indexed by column (index 0 is unused).</summary>
    public static AutoClosingColumnType[] ExtractAutoClosingSpecialColumns(int maxColumn, string annotatedLine)
    {
        var result = new AutoClosingColumnType[maxColumn + 1];
        int column = 1;
        foreach (char c in annotatedLine)
        {
            if (c == '|')
            {
                result[column] = AutoClosingColumnType.Special1;
            }
            else if (c == '!')
            {
                result[column] = AutoClosingColumnType.Special2;
            }
            else
            {
                column++;
            }
        }

        return result;
    }

    /// <summary>Monaco: <c>assertType</c>.</summary>
    public static void AssertType(TextModel model, ViewModel viewModel, int lineNumber, int column, string chr, string expectedInsert, string message)
    {
        string lineContent = model.GetLineContent(lineNumber);
        string expected = lineContent[..(column - 1)] + expectedInsert + lineContent[(column - 1)..];
        CoreNavigationCommands.MoveTo.RunCoreEditorCommand(viewModel, new MoveCommandArgs(new Position(lineNumber, column)));
        viewModel.Type(chr, "keyboard");
        Assert.True(expected == model.GetLineContent(lineNumber), message + $" (expected '{expected}', got '{model.GetLineContent(lineNumber)}')");
        model.Undo();
    }
}
