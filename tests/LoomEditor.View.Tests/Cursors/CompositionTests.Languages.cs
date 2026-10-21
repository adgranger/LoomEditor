// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller', IME composition in the auto-closing language)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public partial class CompositionTests
{
    private const string AutoClosingId = EditorControllerLanguages.AutoClosingLanguageId;

    // Monaco: 'issue #15825: accents on mac US intl keyboard'
    [Fact]
    public void Issue15825_AccentsOnMacUsIntlKeyboard()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts([], AutoClosingId), (editor, model, viewModel) =>
        {
            AssertCursor(viewModel, new Position(1, 1));

            // Typing ` + e on the mac US intl kb layout
            viewModel.StartComposition();
            viewModel.Type("`", "keyboard");
            viewModel.CompositionType("è", 1, 0, 0, "keyboard");
            viewModel.EndComposition("keyboard");

            Assert.Equal("è", model.GetValue());
        });
    }

    // Monaco: 'issue #90016: allow accents on mac US intl keyboard to surround selection'
    [Fact]
    public void Issue90016_AllowAccentsOnMacUsIntlKeyboardToSurroundSelection()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["test"], AutoClosingId), (editor, model, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 1, 1, 5)]);

            // Typing ` + e on the mac US intl kb layout
            viewModel.StartComposition();
            viewModel.Type("'", "keyboard");
            viewModel.CompositionType("'", 1, 0, 0, "keyboard");
            viewModel.CompositionType("'", 1, 0, 0, "keyboard");
            viewModel.EndComposition("keyboard");

            Assert.Equal("'test'", model.GetValue());
        });
    }

    // Monaco: 'issue #2773: Accents (´`¨^, others?) are inserted in the wrong position (Mac)'
    [Fact]
    public void Issue2773_AccentsAreInsertedInTheWrongPositionOnMac()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["hello", "world"], AutoClosingId), (editor, model, viewModel) =>
        {
            AssertCursor(viewModel, new Position(1, 1));

            // Typing ` and pressing shift+down on the mac US intl kb layout
            // Here we're just replaying what the cursor gets
            viewModel.StartComposition();
            viewModel.Type("`", "keyboard");
            MoveDown(viewModel, true);
            viewModel.CompositionType("`", 1, 0, 0, "keyboard");
            viewModel.CompositionType("`", 1, 0, 0, "keyboard");
            viewModel.EndComposition("keyboard");

            Assert.Equal("`hello\nworld", model.GetValue());
            AssertCursor(viewModel, new Selection(1, 2, 2, 2));
        });
    }

    // Monaco: 'issue #26820: auto close quotes when not used as accents'
    [Fact]
    public void Issue26820_AutoCloseQuotesWhenNotUsedAsAccents()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts([string.Empty], AutoClosingId), (editor, model, viewModel) =>
        {
            AssertCursor(viewModel, new Position(1, 1));

            void TypeQuoteWithSpace()
            {
                viewModel.StartComposition();
                viewModel.Type("'", "keyboard");
                viewModel.CompositionType("'", 1, 0, 0, "keyboard");
                viewModel.EndComposition("keyboard");
            }

            // on the mac US intl kb layout

            // Typing ' + space
            TypeQuoteWithSpace();
            Assert.Equal("''", model.GetValue());

            // Typing one more ' + space
            TypeQuoteWithSpace();
            Assert.Equal("''", model.GetValue());

            // Typing ' as a closing tag
            model.SetValue("'abc");
            viewModel.SetSelections("test", [new Selection(1, 5, 1, 5)]);
            TypeQuoteWithSpace();
            Assert.Equal("'abc'", model.GetValue());

            // quotes before the newly added character are all paired.
            model.SetValue("'abc'def ");
            viewModel.SetSelections("test", [new Selection(1, 10, 1, 10)]);
            TypeQuoteWithSpace();
            Assert.Equal("'abc'def ''", model.GetValue());

            // No auto closing if there is non-whitespace character after the cursor
            model.SetValue("abc");
            viewModel.SetSelections("test", [new Selection(1, 1, 1, 1)]);
            TypeQuoteWithSpace();

            // No auto closing if it's after a word.
            model.SetValue("abc");
            viewModel.SetSelections("test", [new Selection(1, 4, 1, 4)]);
            TypeQuoteWithSpace();
            Assert.Equal("abc'", model.GetValue());
        });
    }

    // Monaco: 'issue #144690: Quotes do not overtype when using US Intl PC keyboard layout'
    [Fact]
    public void Issue144690_QuotesDoNotOvertypeWhenUsingUsIntlPcKeyboardLayout()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts([string.Empty], AutoClosingId), (editor, model, viewModel) =>
        {
            AssertCursor(viewModel, new Position(1, 1));

            // Pressing ' + ' + ;
            viewModel.StartComposition();
            viewModel.Type("'", "keyboard");
            viewModel.CompositionType("'", 1, 0, 0, "keyboard");
            viewModel.CompositionType("'", 1, 0, 0, "keyboard");
            viewModel.EndComposition("keyboard");
            viewModel.StartComposition();
            viewModel.Type("'", "keyboard");
            viewModel.CompositionType("';", 1, 0, 0, "keyboard");
            viewModel.CompositionType("';", 2, 0, 0, "keyboard");
            viewModel.EndComposition("keyboard");

            Assert.Equal("'';", model.GetValue());
        });
    }

    // Monaco: 'issue #144693: Typing a quote using US Intl PC keyboard layout always surrounds words'
    [Fact]
    public void Issue144693_TypingAQuoteUsingUsIntlPcKeyboardLayoutAlwaysSurroundsWords()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["const hello = 3;"], AutoClosingId), (editor, model, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 7, 1, 12)]);

            // Pressing ' + e
            viewModel.StartComposition();
            viewModel.Type("'", "keyboard");
            viewModel.CompositionType("é", 1, 0, 0, "keyboard");
            viewModel.CompositionType("é", 1, 0, 0, "keyboard");
            viewModel.EndComposition("keyboard");

            Assert.Equal("const é = 3;", model.GetValue());
        });
    }

    // Monaco: 'issue #82701: auto close does not execute when IME is canceled via backspace'
    [Fact]
    public void Issue82701_AutoCloseDoesNotExecuteWhenImeIsCanceledViaBackspace()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["{}"], AutoClosingId), (editor, model, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 2, 1, 2)]);

            // Typing a + backspace
            viewModel.StartComposition();
            viewModel.Type("a", "keyboard");
            viewModel.CompositionType(string.Empty, 1, 0, 0, "keyboard");
            viewModel.EndComposition("keyboard");
            Assert.Equal("{}", model.GetValue());
        });
    }
}
