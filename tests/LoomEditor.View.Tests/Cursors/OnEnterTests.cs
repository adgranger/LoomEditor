// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// Not ported here: 'Enter honors indentNextLinePattern 2' and 'Type honors decreaseIndentPattern' (they type characters that
// are auto-indented: P5-45) and the two 'onEnter works if there are no indentation rules' tests (they are in TypingTests).
public class OnEnterTests
{
    private static readonly EditorCursorOptions FullAutoIndent = new() { AutoIndent = EditorAutoIndentStrategy.Full };

    // Monaco's creation options with the given overrides (indentSize stays 4 unless given).
    private static TextModelOptions Opts(bool insertSpaces = true, int tabSize = 4, int indentSize = 4) =>
        new(tabSize: tabSize, indentSize: indentSize, insertSpaces: insertSpaces);

    private static void Type(ViewModel viewModel, string text = "\n") => viewModel.Type(text, "keyboard");

    // Monaco: 'Enter auto-indents with insertSpaces setting 1'
    [Fact]
    public void EnterAutoIndentsWithInsertSpacesSetting1()
    {
        using var languages = new EditorControllerLanguages();
        string languageId = languages.SetupOnEnterLanguage(IndentAction.Indent);
        languages.UsingCursor(new CursorOpts(["\thello"], languageId), (editor, model, viewModel) =>
        {
            MoveTo(viewModel, 1, 7);
            AssertCursor(viewModel, new Selection(1, 7, 1, 7));

            Type(viewModel);
            Assert.Equal("\thello\r\n        ", model.GetValue(EndOfLinePreference.CRLF));
        });
    }

    // Monaco: 'Enter auto-indents with insertSpaces setting 2'
    [Fact]
    public void EnterAutoIndentsWithInsertSpacesSetting2()
    {
        using var languages = new EditorControllerLanguages();
        string languageId = languages.SetupOnEnterLanguage(IndentAction.None);
        languages.UsingCursor(new CursorOpts(["\thello"], languageId), (editor, model, viewModel) =>
        {
            MoveTo(viewModel, 1, 7);
            AssertCursor(viewModel, new Selection(1, 7, 1, 7));

            Type(viewModel);
            Assert.Equal("\thello\r\n    ", model.GetValue(EndOfLinePreference.CRLF));
        });
    }

    // Monaco: 'Enter auto-indents with insertSpaces setting 3'
    [Fact]
    public void EnterAutoIndentsWithInsertSpacesSetting3()
    {
        using var languages = new EditorControllerLanguages();
        string languageId = languages.SetupOnEnterLanguage(IndentAction.IndentOutdent);
        languages.UsingCursor(new CursorOpts(["\thell()"], languageId), (editor, model, viewModel) =>
        {
            MoveTo(viewModel, 1, 7);
            AssertCursor(viewModel, new Selection(1, 7, 1, 7));

            Type(viewModel);
            Assert.Equal("\thell(\r\n        \r\n    )", model.GetValue(EndOfLinePreference.CRLF));
        });
    }

    // Monaco: 'issue #115033: indent and appendText'
    [Fact]
    public void Issue115033IndentAndAppendText()
    {
        using var languages = new EditorControllerLanguages();
        const string LanguageId = "onEnterMode";
        languages.Services.RegisterLanguage(LanguageId);
        languages.Services.Add(languages.Services.LanguageConfigurationRegistry.Register(LanguageId, new LanguageConfiguration
        {
            OnEnterRules = [new OnEnterRule(new Regex(".*", RegexOptions.CultureInvariant), new EnterAction(IndentAction.Indent, AppendText: "x"))],
        }));
        languages.UsingCursor(new CursorOpts(["text"], LanguageId), (editor, model, viewModel) =>
        {
            MoveTo(viewModel, 1, 5);
            Type(viewModel);

            Assert.Equal("text", model.GetLineContent(1));
            Assert.Equal("    x", model.GetLineContent(2));
            AssertCursor(viewModel, new Position(2, 6));
        });
    }

    private sealed class RemoveCharacterCommand(ViewModel viewModel) : IEditCommand
    {
        private string? _selectionId;

        public bool InsertsAutoWhitespace => false;

        public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
        {
            builder.AddEditOperation(new Range(1, 13, 1, 14), string.Empty);
            _selectionId = builder.TrackSelection(viewModel.GetSelection());
        }

        public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper) => helper.GetTrackedSelection(_selectionId!);
    }

    // Monaco: 'issue #6862: Editor removes auto inserted indentation when formatting on type'
    [Fact]
    public void Issue6862EditorRemovesAutoInsertedIndentationWhenFormattingOnType()
    {
        using var languages = new EditorControllerLanguages();
        string languageId = languages.SetupOnEnterLanguage(IndentAction.IndentOutdent);
        languages.UsingCursor(new CursorOpts(["function foo (params: string) {}"], languageId), (editor, model, viewModel) =>
        {
            MoveTo(viewModel, 1, 32);
            Type(viewModel);
            Assert.Equal("function foo (params: string) {", model.GetLineContent(1));
            Assert.Equal("    ", model.GetLineContent(2));
            Assert.Equal("}", model.GetLineContent(3));

            viewModel.ExecuteCommand(new RemoveCharacterCommand(viewModel), "autoFormat");
            Assert.Equal("function foo(params: string) {", model.GetLineContent(1));
            Assert.Equal("    ", model.GetLineContent(2));
            Assert.Equal("}", model.GetLineContent(3));
        });
    }

    // Monaco: 'Enter honors increaseIndentPattern'
    [Fact]
    public void EnterHonorsIncreaseIndentPattern()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["if (true) {", "\tif (true) {"], EditorControllerLanguages.IndentRulesLanguageId, Opts(insertSpaces: false), FullAutoIndent),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 1, 12);
                AssertCursor(viewModel, new Selection(1, 12, 1, 12));

                Type(viewModel);
                model.Tokenization.ForceTokenization(model.LineCount);
                AssertCursor(viewModel, new Selection(2, 2, 2, 2));

                MoveTo(viewModel, 3, 13);
                AssertCursor(viewModel, new Selection(3, 13, 3, 13));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(4, 3, 4, 3));
            });
    }

    // Monaco: 'Enter honors unIndentedLinePattern'
    [Fact]
    public void EnterHonorsUnIndentedLinePattern()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["if (true) {", "\t\t\treturn true"], EditorControllerLanguages.IndentRulesLanguageId, Opts(insertSpaces: false), FullAutoIndent),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 2, 15);
                AssertCursor(viewModel, new Selection(2, 15, 2, 15));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(3, 2, 3, 2));
            });
    }

    // Monaco: 'Enter honors indentNextLinePattern'
    [Fact]
    public void EnterHonorsIndentNextLinePattern()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(
                ["if (true)", "\treturn true;", "if (true)", "\t\t\t\treturn true"],
                EditorControllerLanguages.IndentRulesLanguageId,
                Opts(insertSpaces: false),
                FullAutoIndent),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 2, 14);
                AssertCursor(viewModel, new Selection(2, 14, 2, 14));

                Type(viewModel);
                model.Tokenization.ForceTokenization(model.LineCount);
                AssertCursor(viewModel, new Selection(3, 1, 3, 1));

                MoveTo(viewModel, 5, 16);
                AssertCursor(viewModel, new Selection(5, 16, 5, 16));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(6, 2, 6, 2));
            });
    }

    // Monaco: 'Enter honors intential indent'
    [Fact]
    public void EnterHonorsIntentialIndent()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["if (true) {", "\tif (true) {", "return true;", "}}"], EditorControllerLanguages.IndentRulesLanguageId, EditorOpts: FullAutoIndent),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 3, 13);
                AssertCursor(viewModel, new Selection(3, 13, 3, 13));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(4, 1, 4, 1));
                Assert.Equal("return true;", model.GetLineContent(3));
            });
    }

    // Monaco: 'Enter supports selection 1'
    [Fact]
    public void EnterSupportsSelection1()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(
                ["if (true) {", "\tif (true) {", "\t\treturn true;", "\t}a}"],
                EditorControllerLanguages.IndentRulesLanguageId,
                Opts(insertSpaces: false)),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 4, 3);
                MoveTo(viewModel, 4, 4, true);
                AssertCursor(viewModel, new Selection(4, 3, 4, 4));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(5, 1, 5, 1));
                Assert.Equal("\t}", model.GetLineContent(4));
            });
    }

    // Monaco: 'Enter supports selection 2'
    [Fact]
    public void EnterSupportsSelection2()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["if (true) {", "\tif (true) {"], EditorControllerLanguages.IndentRulesLanguageId, Opts(insertSpaces: false)),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 2, 12);
                MoveTo(viewModel, 2, 13, true);
                AssertCursor(viewModel, new Selection(2, 12, 2, 13));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(3, 3, 3, 3));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(4, 3, 4, 3));
            });
    }

    // Monaco: 'Enter honors tabSize and insertSpaces 1'
    [Fact]
    public void EnterHonorsTabSizeAndInsertSpaces1()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["if (true) {", "\tif (true) {"], EditorControllerLanguages.IndentRulesLanguageId),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 1, 12);
                AssertCursor(viewModel, new Selection(1, 12, 1, 12));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(2, 5, 2, 5));
                model.Tokenization.ForceTokenization(model.LineCount);

                MoveTo(viewModel, 3, 13);
                AssertCursor(viewModel, new Selection(3, 13, 3, 13));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(4, 9, 4, 9));
            });
    }

    // Monaco: 'Enter honors tabSize and insertSpaces 2'
    [Fact]
    public void EnterHonorsTabSizeAndInsertSpaces2()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["if (true) {", "    if (true) {"], EditorControllerLanguages.IndentRulesLanguageId),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 1, 12);
                AssertCursor(viewModel, new Selection(1, 12, 1, 12));

                Type(viewModel);
                model.Tokenization.ForceTokenization(model.LineCount);
                AssertCursor(viewModel, new Selection(2, 5, 2, 5));

                MoveTo(viewModel, 3, 16);
                AssertCursor(viewModel, new Selection(3, 16, 3, 16));

                Type(viewModel);
                Assert.Equal("    if (true) {", model.GetLineContent(3));
                AssertCursor(viewModel, new Selection(4, 9, 4, 9));
            });
    }

    // Monaco: 'Enter honors tabSize and insertSpaces 3'
    [Fact]
    public void EnterHonorsTabSizeAndInsertSpaces3()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["if (true) {", "    if (true) {"], EditorControllerLanguages.IndentRulesLanguageId, Opts(insertSpaces: false)),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 1, 12);
                AssertCursor(viewModel, new Selection(1, 12, 1, 12));

                Type(viewModel);
                model.Tokenization.ForceTokenization(model.LineCount);
                AssertCursor(viewModel, new Selection(2, 2, 2, 2));

                MoveTo(viewModel, 3, 16);
                AssertCursor(viewModel, new Selection(3, 16, 3, 16));

                Type(viewModel);
                Assert.Equal("    if (true) {", model.GetLineContent(3));
                AssertCursor(viewModel, new Selection(4, 3, 4, 3));
            });
    }

    // Monaco: 'Enter supports intentional indentation'
    [Fact]
    public void EnterSupportsIntentionalIndentation()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(
                ["\tif (true) {", "\t\tswitch(true) {", "\t\t\tcase true:", "\t\t\t\tbreak;", "\t\t}", "\t}"],
                EditorControllerLanguages.IndentRulesLanguageId,
                Opts(insertSpaces: false),
                FullAutoIndent),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 5, 4);
                AssertCursor(viewModel, new Selection(5, 4, 5, 4));

                Type(viewModel);
                Assert.Equal("\t\t}", model.GetLineContent(5));
                AssertCursor(viewModel, new Selection(6, 3, 6, 3));
            });
    }

    private static readonly string[] MiddleOfLineTabs = ["if (true) {", "\tif (true) {", "\t\treturn true;", "\t}a}"];

    // Monaco: 'Enter should not adjust cursor position when press enter in the middle of a line 1'
    [Fact]
    public void EnterShouldNotAdjustCursorPositionWhenPressEnterInTheMiddleOfALine1()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(MiddleOfLineTabs, EditorControllerLanguages.IndentRulesLanguageId, Opts(insertSpaces: false)),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 3, 9);
                AssertCursor(viewModel, new Selection(3, 9, 3, 9));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(4, 3, 4, 3));
                Assert.Equal("\t\t true;", model.GetLineContent(4));
            });
    }

    // Monaco: 'Enter should not adjust cursor position when press enter in the middle of a line 2'
    [Fact]
    public void EnterShouldNotAdjustCursorPositionWhenPressEnterInTheMiddleOfALine2()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(MiddleOfLineTabs, EditorControllerLanguages.IndentRulesLanguageId, Opts(insertSpaces: false)),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 3, 3);
                AssertCursor(viewModel, new Selection(3, 3, 3, 3));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(4, 3, 4, 3));
                Assert.Equal("\t\treturn true;", model.GetLineContent(4));
            });
    }

    // Monaco: 'Enter should not adjust cursor position when press enter in the middle of a line 3'
    [Fact]
    public void EnterShouldNotAdjustCursorPositionWhenPressEnterInTheMiddleOfALine3()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["if (true) {", "  if (true) {", "    return true;", "  }a}"], EditorControllerLanguages.IndentRulesLanguageId),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 3, 11);
                AssertCursor(viewModel, new Selection(3, 11, 3, 11));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(4, 5, 4, 5));
                Assert.Equal("     true;", model.GetLineContent(4));
            });
    }

    // Monaco: 'Enter should adjust cursor position when press enter in the middle of leading whitespaces 1'
    [Fact]
    public void EnterShouldAdjustCursorPositionWhenPressEnterInTheMiddleOfLeadingWhitespaces1()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(MiddleOfLineTabs, EditorControllerLanguages.IndentRulesLanguageId, Opts(insertSpaces: false)),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 3, 2);
                AssertCursor(viewModel, new Selection(3, 2, 3, 2));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(4, 2, 4, 2));
                Assert.Equal("\t\treturn true;", model.GetLineContent(4));

                MoveTo(viewModel, 4, 1);
                AssertCursor(viewModel, new Selection(4, 1, 4, 1));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(5, 1, 5, 1));
                Assert.Equal("\t\treturn true;", model.GetLineContent(5));
            });
    }

    // Monaco: 'Enter should adjust cursor position when press enter in the middle of leading whitespaces 2'
    [Fact]
    public void EnterShouldAdjustCursorPositionWhenPressEnterInTheMiddleOfLeadingWhitespaces2()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(
                ["\tif (true) {", "\t\tif (true) {", "\t    \treturn true;", "\t\t}a}"],
                EditorControllerLanguages.IndentRulesLanguageId,
                Opts(insertSpaces: false)),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 3, 4);
                AssertCursor(viewModel, new Selection(3, 4, 3, 4));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(4, 3, 4, 3));
                Assert.Equal("\t\t\treturn true;", model.GetLineContent(4));

                MoveTo(viewModel, 4, 1);
                AssertCursor(viewModel, new Selection(4, 1, 4, 1));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(5, 1, 5, 1));
                Assert.Equal("\t\t\treturn true;", model.GetLineContent(5));
            });
    }

    // Monaco: 'Enter should adjust cursor position when press enter in the middle of leading whitespaces 3'
    [Fact]
    public void EnterShouldAdjustCursorPositionWhenPressEnterInTheMiddleOfLeadingWhitespaces3()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["if (true) {", "  if (true) {", "    return true;", "}a}"], EditorControllerLanguages.IndentRulesLanguageId),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 3, 2);
                AssertCursor(viewModel, new Selection(3, 2, 3, 2));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(4, 2, 4, 2));
                Assert.Equal("    return true;", model.GetLineContent(4));

                MoveTo(viewModel, 4, 3);
                Type(viewModel);
                AssertCursor(viewModel, new Selection(5, 3, 5, 3));
                Assert.Equal("    return true;", model.GetLineContent(5));
            });
    }

    // Monaco: 'Enter should adjust cursor position when press enter in the middle of leading whitespaces 4'
    [Fact]
    public void EnterShouldAdjustCursorPositionWhenPressEnterInTheMiddleOfLeadingWhitespaces4()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(
                [
                    "if (true) {",
                    "  if (true) {",
                    "\t  return true;",
                    "}a}",
                    string.Empty,
                    "if (true) {",
                    "  if (true) {",
                    "\t  return true;",
                    "}a}",
                ],
                EditorControllerLanguages.IndentRulesLanguageId,
                Opts(tabSize: 2, indentSize: 2)),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 3, 3);
                AssertCursor(viewModel, new Selection(3, 3, 3, 3));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(4, 4, 4, 4));
                Assert.Equal("    return true;", model.GetLineContent(4));

                MoveTo(viewModel, 9, 4);
                Type(viewModel);
                AssertCursor(viewModel, new Selection(10, 5, 10, 5));
                Assert.Equal("    return true;", model.GetLineContent(10));
            });
    }

    // Monaco: 'Enter should adjust cursor position when press enter in the middle of leading whitespaces 5'
    [Fact]
    public void EnterShouldAdjustCursorPositionWhenPressEnterInTheMiddleOfLeadingWhitespaces5()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(
                ["if (true) {", "  if (true) {", "    return true;", "    return true;", string.Empty],
                EditorControllerLanguages.IndentRulesLanguageId,
                Opts(tabSize: 2)),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 3, 5);
                MoveTo(viewModel, 4, 3, true);
                AssertCursor(viewModel, new Selection(3, 5, 4, 3));

                Type(viewModel);
                AssertCursor(viewModel, new Selection(4, 3, 4, 3));
                Assert.Equal("    return true;", model.GetLineContent(4));
            });
    }

    private static readonly string[] Monaco108Text = ["function baz() {", "\tvar x = 1;", "\t\t\t\t\t\t\treturn x;", "}"];

    // Monaco: 'issue microsoft/monaco-editor#108 part 1/2: Auto indentation on Enter with selection is half broken'
    [Fact]
    public void IssueMonacoEditor108Part1AutoIndentationOnEnterWithSelectionIsHalfBroken()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(Monaco108Text, EditorControllerLanguages.IndentRulesLanguageId, Opts(insertSpaces: false)),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 3, 8);
                MoveTo(viewModel, 2, 12, true);
                AssertCursor(viewModel, new Selection(3, 8, 2, 12));

                Type(viewModel);
                Assert.Equal("\treturn x;", model.GetLineContent(3));
                AssertCursor(viewModel, new Position(3, 2));
            });
    }

    // Monaco: 'issue microsoft/monaco-editor#108 part 2/2: Auto indentation on Enter with selection is half broken'
    [Fact]
    public void IssueMonacoEditor108Part2AutoIndentationOnEnterWithSelectionIsHalfBroken()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(Monaco108Text, EditorControllerLanguages.IndentRulesLanguageId, Opts(insertSpaces: false)),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 2, 12);
                MoveTo(viewModel, 3, 8, true);
                AssertCursor(viewModel, new Selection(2, 12, 3, 8));

                Type(viewModel);
                Assert.Equal("\treturn x;", model.GetLineContent(3));
                AssertCursor(viewModel, new Position(3, 2));
            });
    }

    // Monaco: 'issue #57197: indent rules regex should be stateless'
    [Fact]
    public void Issue57197IndentRulesRegexShouldBeStateless()
    {
        using var languages = new EditorControllerLanguages();
        string languageId = languages.SetupIndentRulesLanguage("lang", new IndentationRule(
            DecreaseIndentPattern: new Regex(@"^\s*}$", RegexOptions.CultureInvariant | RegexOptions.Multiline),
            IncreaseIndentPattern: new Regex(
                @"^(?![^\S\n]*(?!--|––|——)(?:[-❍❑■⬜□☐▪▫–—≡→›✘xX✔✓☑+]|\[[ xX+-]?\])\s[^\n]*)[^\S\n]*(.+:)[^\S\n]*(?:(?=@[^\s*~(]+(?::\/\/[^\s*~(:]+)?(?:\([^)]*\))?)|$)",
                RegexOptions.CultureInvariant | RegexOptions.Multiline)));
        languages.UsingCursor(
            new CursorOpts(["Project:"], languageId, Opts(insertSpaces: false), FullAutoIndent),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 1, 9);
                AssertCursor(viewModel, new Selection(1, 9, 1, 9));

                Type(viewModel);
                model.Tokenization.ForceTokenization(model.LineCount);
                AssertCursor(viewModel, new Selection(2, 2, 2, 2));

                MoveTo(viewModel, 1, 9);
                AssertCursor(viewModel, new Selection(1, 9, 1, 9));
                Type(viewModel);
                model.Tokenization.ForceTokenization(model.LineCount);
                AssertCursor(viewModel, new Selection(2, 2, 2, 2));
            });
    }

    // Monaco: 'typing in json'
    [Fact]
    public void TypingInJson()
    {
        using var languages = new EditorControllerLanguages();
        const string LanguageId = "indentRulesMode";
        languages.Services.RegisterLanguage(LanguageId);
        languages.Services.Add(languages.Services.LanguageConfigurationRegistry.Register(LanguageId, new LanguageConfiguration
        {
            Brackets = [new("{", "}"), new("[", "]"), new("(", ")")],
            IndentationRules = new IndentationRule(
                DecreaseIndentPattern: new Regex(@"^\s*[}\]],?\s*$", RegexOptions.CultureInvariant),
                IncreaseIndentPattern: new Regex(@"({+(?=([^""]*""[^""]*"")*[^""}]*$))|(\[+(?=([^""]*""[^""]*"")*[^""\]]*$))", RegexOptions.CultureInvariant)),
        }));
        TextModel model = languages.CreateTextModel(
            string.Join('\n', "{", "  \"scripts: {\"", "    \"watch\": \"a {\"", "    \"build{\": \"b\"", "    \"tasks\": []", "    \"tasks\": [\"a\"]", "  \"}\"", "\"}\""),
            LanguageId,
            Opts(tabSize: 2, indentSize: 2));

        EditorControllerLanguages.WithTestCodeEditor(model, new TestCodeEditorOptions(CursorOptions: FullAutoIndent), (editor, viewModel) =>
        {
            MoveTo(viewModel, 3, 19);
            AssertCursor(viewModel, new Selection(3, 19, 3, 19));

            Type(viewModel);
            Assert.Equal("    ", model.GetLineContent(4));

            MoveTo(viewModel, 5, 18);
            AssertCursor(viewModel, new Selection(5, 18, 5, 18));

            Type(viewModel);
            Assert.Equal("    ", model.GetLineContent(6));

            MoveTo(viewModel, 7, 15);
            AssertCursor(viewModel, new Selection(7, 15, 7, 15));

            Type(viewModel);
            Assert.Equal("      ", model.GetLineContent(8));
            Assert.Equal("    ]", model.GetLineContent(9));

            MoveTo(viewModel, 10, 18);
            AssertCursor(viewModel, new Selection(10, 18, 10, 18));

            Type(viewModel);
            Assert.Equal("    ]", model.GetLineContent(11));
        });
    }

    // Monaco: 'issue #111128: Multicursor `Enter` issue with indentation'
    [Fact]
    public void Issue111128MulticursorEnterIssueWithIndentation()
    {
        using var languages = new EditorControllerLanguages();
        TextModel model = languages.CreateTextModel("    let a, b, c;", EditorControllerLanguages.IndentRulesLanguageId, Opts(insertSpaces: false));

        EditorControllerLanguages.WithTestCodeEditor(model, null, (editor, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 11, 1, 11), new Selection(1, 14, 1, 14)]);
            Type(viewModel);

            Assert.Equal("    let a,\n\t b,\n\t c;", model.GetValue());
        });
    }
}
