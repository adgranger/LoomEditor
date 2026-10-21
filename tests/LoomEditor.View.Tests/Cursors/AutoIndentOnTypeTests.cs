// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public class AutoIndentOnTypeTests
{
    private const RegexOptions Js = RegexOptions.CultureInvariant;

    private static readonly EditorCursorOptions FullAutoIndent = new() { AutoIndent = EditorAutoIndentStrategy.Full };

    // Monaco: 'Type honors decreaseIndentPattern'
    [Fact]
    public void TypeHonorsDecreaseIndentPattern()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["if (true) {", "\t"], EditorControllerLanguages.IndentRulesLanguageId, EditorOpts: FullAutoIndent),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 2, 2);
                AssertCursor(viewModel, new Selection(2, 2, 2, 2));

                viewModel.Type("}", "keyboard");
                AssertCursor(viewModel, new Selection(2, 2, 2, 2));
                Assert.Equal("}", model.GetLineContent(2));
            });
    }

    // Monaco: 'Enter honors indentNextLinePattern 2'
    [Fact]
    public void EnterHonorsIndentNextLinePattern2()
    {
        using var languages = new EditorControllerLanguages();
        TextModel model = languages.CreateTextModel(
            "if (true)\n\tif (true)",
            EditorControllerLanguages.IndentRulesLanguageId,
            new TextModelOptions(insertSpaces: false));

        EditorControllerLanguages.WithTestCodeEditor(model, new TestCodeEditorOptions(CursorOptions: FullAutoIndent), (editor, viewModel) =>
        {
            MoveTo(viewModel, 2, 11);
            AssertCursor(viewModel, new Selection(2, 11, 2, 11));

            viewModel.Type("\n", "keyboard");
            model.Tokenization.ForceTokenization(model.LineCount);
            AssertCursor(viewModel, new Selection(3, 3, 3, 3));

            viewModel.Type("console.log();", "keyboard");
            viewModel.Type("\n", "keyboard");
            AssertCursor(viewModel, new Selection(4, 1, 4, 1));
        });
    }

    // Monaco: 'type honors indentation rules: ruby keywords'
    [Fact]
    public void TypeHonorsIndentationRulesRubyKeywords()
    {
        using var languages = new EditorControllerLanguages();
        string rubyLanguageId = languages.SetupIndentRulesLanguage("ruby", new IndentationRule(
            IncreaseIndentPattern: new Regex(@"^\s*((begin|class|def|else|elsif|ensure|for|if|module|rescue|unless|until|when|while)|(.*\sdo\b))\b[^\{;]*$", Js),
            DecreaseIndentPattern: new Regex(@"^\s*([}\]]([,)]?\s*(#|$)|\.[a-zA-Z_]\w*\b)|(end|rescue|ensure|else|elsif|when)\b)", Js)));
        TextModel model = languages.CreateTextModel("class Greeter\n  def initialize(name)\n    @name = name\n    en", rubyLanguageId);

        EditorControllerLanguages.WithTestCodeEditor(model, new TestCodeEditorOptions(CursorOptions: FullAutoIndent), (editor, viewModel) =>
        {
            MoveTo(viewModel, 4, 7);
            AssertCursor(viewModel, new Selection(4, 7, 4, 7));

            viewModel.Type("d", "keyboard");
            Assert.Equal("  end", model.GetLineContent(4));
        });
    }

    // Monaco: 'Auto indent on type: increaseIndentPattern has higher priority than decreaseIndent when inheriting'
    [Fact]
    public void IncreaseIndentPatternHasHigherPriorityThanDecreaseIndentWhenInheriting()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(
                ["\tif (true) {", "\t\tconsole.log();", "\t} else if {", "\t\tconsole.log()", "\t}"],
                EditorControllerLanguages.IndentRulesLanguageId),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 5, 3);
                AssertCursor(viewModel, new Selection(5, 3, 5, 3));

                viewModel.Type("e", "keyboard");
                AssertCursor(viewModel, new Selection(5, 4, 5, 4));
                Assert.Equal("\t}e", model.GetLineContent(5)); // This line should not decrease indent
            });
    }

    // Monaco: 'type honors users indentation adjustment'
    [Fact]
    public void TypeHonorsUsersIndentationAdjustment()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(
                ["\tif (true ||", "\t ) {", "\t}", "if (true ||", ") {", "}"],
                EditorControllerLanguages.IndentRulesLanguageId),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 2, 3);
                AssertCursor(viewModel, new Selection(2, 3, 2, 3));

                viewModel.Type(" ", "keyboard");
                AssertCursor(viewModel, new Selection(2, 4, 2, 4));
                Assert.Equal("\t  ) {", model.GetLineContent(2)); // This line should not decrease indent
            });
    }

    // Monaco: 'bug 29972: if a line is line comment, open bracket should not indent next line'
    [Fact]
    public void Bug29972_IfALineIsLineCommentOpenBracketShouldNotIndentNextLine()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["if (true) {", "\t// {", "\t\t"], EditorControllerLanguages.IndentRulesLanguageId, EditorOpts: FullAutoIndent),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 3, 3);
                AssertCursor(viewModel, new Selection(3, 3, 3, 3));

                viewModel.Type("}", "keyboard");
                AssertCursor(viewModel, new Selection(3, 2, 3, 2));
                Assert.Equal("}", model.GetLineContent(3));
            });
    }

    // Monaco: 'issue #122714: tabSize=1 prevent typing a string matching decreaseIndentPattern in an empty file'
    [Fact]
    public void Issue122714_TabSize1PreventTypingAStringMatchingDecreaseIndentPatternInAnEmptyFile()
    {
        using var languages = new EditorControllerLanguages();
        string latexLanguageId = languages.SetupIndentRulesLanguage("latex", new IndentationRule(
            IncreaseIndentPattern: new Regex(@"\\begin{(?!document)([^}]*)}(?!.*\\end{\1})", Js),
            DecreaseIndentPattern: new Regex(@"^\s*\\end{(?!document)", Js)));
        TextModel model = languages.CreateTextModel("\\end", latexLanguageId, new TextModelOptions(tabSize: 1));

        EditorControllerLanguages.WithTestCodeEditor(model, new TestCodeEditorOptions(CursorOptions: FullAutoIndent), (editor, viewModel) =>
        {
            MoveTo(viewModel, 1, 5);
            AssertCursor(viewModel, new Selection(1, 5, 1, 5));

            viewModel.Type("{", "keyboard");
            Assert.Equal("\\end{}", model.GetLineContent(1));
        });
    }

    // Monaco: 'issue #338693: undo after multi-character autoclose with auto-indentation first removes only the auto-closed characters'
    [Fact]
    public void Issue338693_UndoAfterMultiCharacterAutocloseWithAutoIndentationFirstRemovesOnlyTheAutoClosedCharacters()
    {
        using var languages = new EditorControllerLanguages();
        const string LanguageId = "autoClosingWithIndentRulesLanguage";
        languages.Services.RegisterLanguage(LanguageId);
        languages.Services.Add(languages.Services.LanguageConfigurationRegistry.Register(LanguageId, new LanguageConfiguration
        {
            AutoClosingPairs = [new("begin", "end")],
            IndentationRules = new IndentationRule(
                IncreaseIndentPattern: new Regex(@"^\s*procedure\b", Js),
                DecreaseIndentPattern: new Regex(@"^\s*begin\b", Js)),
        }));

        languages.UsingCursor(new CursorOpts(["\tfoo", "\tbegi"], LanguageId, EditorOpts: FullAutoIndent), (editor, model, viewModel) =>
        {
            var states = new List<string>();
            void RecordState() => states.Add(model.GetLineContent(2) + "|" + string.Join(" ", viewModel.GetSelections()));

            viewModel.SetSelections("test", [new Selection(2, 6, 2, 6)]);
            viewModel.Type("n", "keyboard");
            RecordState();
            Undo(editor);
            RecordState();
            Undo(editor);
            RecordState();

            Assert.Equal(["beginend|[2,6 -> 2,6]", "begin|[2,6 -> 2,6]", "\tbegi|[2,6 -> 2,6]"], states);
        });
    }
}
