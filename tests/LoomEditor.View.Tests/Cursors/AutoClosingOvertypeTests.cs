// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public class AutoClosingOvertypeTests
{
    private const string LanguageId = EditorControllerLanguages.AutoClosingLanguageId;

    // Monaco: viewModel.executeEdits('snippet', [{ range: new Range(1, 6, 1, 8), text: 'id=""' }], () => [new Selection(1, 10, 1, 10)])
    private static void InsertSnippet(ViewModel viewModel) =>
        viewModel.ExecuteEdits("snippet", [new IdentifiedEditOperation(new Range(1, 6, 1, 8), "id=\"\"")], _ => [new Selection(1, 10, 1, 10)]);

    // Monaco: 'issue #78975 - Parentheses swallowing does not work when parentheses are inserted by autocomplete'
    [Fact]
    public void Issue78975ParenthesesSwallowingDoesNotWorkWhenParenthesesAreInsertedByAutocomplete()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["<div id"], LanguageId), (editor, model, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 8, 1, 8)]);

            InsertSnippet(viewModel);
            Assert.Equal("<div id=\"\"", model.GetLineContent(1));

            viewModel.Type("a", "keyboard");
            Assert.Equal("<div id=\"a\"", model.GetLineContent(1));

            viewModel.Type("\"", "keyboard");
            Assert.Equal("<div id=\"a\"", model.GetLineContent(1));
        });
    }

    [Fact]
    public void Type_CloseCharacterNotAutoClosed_DoesNotOvertype()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["()"], LanguageId), (editor, model, viewModel) =>
        {
            MoveTo(viewModel, 1, 2);

            viewModel.Type(")", "keyboard");

            Assert.Equal("())", model.GetLineContent(1));
        });
    }

    [Fact]
    public void Type_AutoClosingOvertypeNever_DoesNotOvertype()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["<div id"], LanguageId, EditorOpts: new EditorCursorOptions { AutoClosingOvertype = EditorAutoClosingEditStrategy.Never }),
            (editor, model, viewModel) =>
            {
                InsertSnippet(viewModel);

                viewModel.Type("\"", "keyboard");

                Assert.Equal("<div id=\"\"\"", model.GetLineContent(1));
            });
    }

    [Fact]
    public void Type_QuoteAfterBackslash_DoesNotOvertype()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["x\\\"\""], LanguageId, EditorOpts: new EditorCursorOptions { AutoClosingOvertype = EditorAutoClosingEditStrategy.Always }),
            (editor, model, viewModel) =>
            {
                MoveTo(viewModel, 1, 3);

                viewModel.Type("\"", "keyboard");

                Assert.Equal("x\\\"\"\"", model.GetLineContent(1));
            });
    }

    [Fact]
    public void DeleteLeft_InsideSnippetAutoClosedPair_DeletesBothCharacters()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["<div id"], LanguageId), (editor, model, viewModel) =>
        {
            InsertSnippet(viewModel);

            CoreEditingCommands.DeleteLeft.RunCoreEditingCommand(viewModel);

            Assert.Equal("<div id=", model.GetLineContent(1));
        });
    }

    [Fact]
    public void DeleteLeft_AutoClosingDeleteNever_DeletesOneCharacter()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(
            new CursorOpts(["<div id"], LanguageId, EditorOpts: new EditorCursorOptions { AutoClosingDelete = EditorAutoClosingEditStrategy.Never }),
            (editor, model, viewModel) =>
            {
                InsertSnippet(viewModel);

                CoreEditingCommands.DeleteLeft.RunCoreEditingCommand(viewModel);

                Assert.Equal("<div id=\"", model.GetLineContent(1));
            });
    }
}
