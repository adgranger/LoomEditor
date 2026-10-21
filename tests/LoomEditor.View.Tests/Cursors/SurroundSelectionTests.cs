// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public class SurroundSelectionTests
{
    // Monaco: 'Bug #11476: Double bracket surrounding + undo is broken'
    [Fact]
    public void Bug11476_DoubleBracketSurroundingUndoIsBroken()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["hello"], EditorControllerLanguages.SurroundingLanguageId), (editor, model, viewModel) =>
        {
            MoveTo(viewModel, 1, 3);
            MoveTo(viewModel, 1, 5, true);
            AssertCursor(viewModel, new Selection(1, 3, 1, 5));

            viewModel.Type("(", "keyboard");
            AssertCursor(viewModel, new Selection(1, 4, 1, 6));

            viewModel.Type("(", "keyboard");
            AssertCursor(viewModel, new Selection(1, 5, 1, 7));
        });
    }

    // Monaco: 'issue #206774: SurroundSelectionCommand with empty charAfterSelection should not throw'
    [Fact]
    public void Issue206774_SurroundSelectionCommandWithEmptyCharAfterSelectionShouldNotThrow()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["hello world"], EditorControllerLanguages.EmptyClosingSurroundLanguageId), (editor, model, viewModel) =>
        {
            // Select "hello"
            MoveTo(viewModel, 1, 1);
            MoveTo(viewModel, 1, 6, true);
            AssertCursor(viewModel, new Selection(1, 1, 1, 6));

            // Type < which should surround with '<' and empty string
            viewModel.Type("<", "keyboard");

            Assert.Equal("<hello world", model.GetValue());
        });
    }

    // Monaco: 'autoClosingPairs - auto wrapping is configurable'
    [Fact]
    public void AutoWrappingIsConfigurable()
    {
        using var languages = new EditorControllerLanguages();
        const string LanguageId = EditorControllerLanguages.AutoClosingLanguageId;

        languages.UsingCursor(new CursorOpts(["var a = asd"], LanguageId), (editor, model, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 1, 1, 4), new Selection(1, 9, 1, 12)]);

            // type a `
            viewModel.Type("`", "keyboard");
            Assert.Equal("`var` a = `asd`", model.GetValue());

            // type a (
            viewModel.Type("(", "keyboard");
            Assert.Equal("`(var)` a = `(asd)`", model.GetValue());
        });

        languages.UsingCursor(
            new CursorOpts(["var a = asd"], LanguageId, EditorOpts: new EditorCursorOptions { AutoSurround = EditorAutoSurroundStrategy.Never }),
            (editor, model, viewModel) =>
            {
                viewModel.SetSelections("test", [new Selection(1, 1, 1, 4)]);

                // type a `
                viewModel.Type("`", "keyboard");
                Assert.Equal("` a = asd", model.GetValue());
            });

        languages.UsingCursor(
            new CursorOpts(["var a = asd"], LanguageId, EditorOpts: new EditorCursorOptions { AutoSurround = EditorAutoSurroundStrategy.Quotes }),
            (editor, model, viewModel) =>
            {
                viewModel.SetSelections("test", [new Selection(1, 1, 1, 4)]);

                // type a `
                viewModel.Type("`", "keyboard");
                Assert.Equal("`var` a = asd", model.GetValue());

                // type a (
                viewModel.Type("(", "keyboard");
                Assert.Equal("`(` a = asd", model.GetValue());
            });

        languages.UsingCursor(
            new CursorOpts(["var a = asd"], LanguageId, EditorOpts: new EditorCursorOptions { AutoSurround = EditorAutoSurroundStrategy.Brackets }),
            (editor, model, viewModel) =>
            {
                viewModel.SetSelections("test", [new Selection(1, 1, 1, 4)]);

                // type a (
                viewModel.Type("(", "keyboard");
                Assert.Equal("(var) a = asd", model.GetValue());

                // type a `
                viewModel.Type("`", "keyboard");
                Assert.Equal("(`) a = asd", model.GetValue());
            });
    }

    // Monaco: 'issue #41825: Special handling of quotes in surrounding pairs'
    [Fact]
    public void Issue41825_SpecialHandlingOfQuotesInSurroundingPairs()
    {
        using var languages = new EditorControllerLanguages();
        const string MyModeId = "myMode";
        languages.Services.RegisterLanguage(MyModeId);
        languages.Services.Add(languages.Services.LanguageConfigurationRegistry.Register(MyModeId, new LanguageConfiguration
        {
            SurroundingPairs = [new AutoClosingPair("\"", "\""), new AutoClosingPair("'", "'")],
        }));

        TextModel model = languages.CreateTextModel("var x = 'hi';", MyModeId);
        EditorControllerLanguages.WithTestCodeEditor(model, null, (editor, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 9, 1, 10), new Selection(1, 12, 1, 13)]);
            viewModel.Type("\"", "keyboard");
            Assert.Equal("var x = \"hi\";", model.GetValue(EndOfLinePreference.LF));

            viewModel.SetSelections("test", [new Selection(1, 9, 1, 10), new Selection(1, 12, 1, 13)]);
            viewModel.Type("'", "keyboard");
            Assert.Equal("var x = 'hi';", model.GetValue(EndOfLinePreference.LF));
        });
    }

    [Fact]
    public void Type_OpenBracketOverWhitespaceOnlySelection_DoesNotSurround()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["a  b"], EditorControllerLanguages.SurroundingLanguageId), (editor, model, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 2, 1, 4)]);

            viewModel.Type("(", "keyboard");

            Assert.Equal("a(b", model.GetValue());
        });
    }
}
