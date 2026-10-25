// Ported from microsoft/vscode: src/vs/editor/browser/widget/codeEditor/codeEditorWidget.ts (onDidType)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.View.Events;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Layout;

public class ViewModelTypeEventTests
{
    private const string LanguageId = EditorControllerLanguages.AutoClosingLanguageId;

    [Fact]
    public void Type_RaisesDidTypeOncePerCall()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["abc"], LanguageId), (editor, model, viewModel) =>
        {
            var typed = new List<string>();
            viewModel.DidType += (_, text) => typed.Add(text);
            MoveTo(viewModel, 1, 4);

            viewModel.Type("xy", "keyboard");

            Assert.Equal(["xy"], typed);
        });
    }

    [Fact]
    public void Type_AutoClosedPair_StillRaises()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts([""], LanguageId), (editor, model, viewModel) =>
        {
            var typed = new List<string>();
            viewModel.DidType += (_, text) => typed.Add(text);
            MoveTo(viewModel, 1, 1);

            viewModel.Type("(", "keyboard");
            viewModel.Type(")", "keyboard");

            Assert.Equal("()", model.GetLineContent(1));
            Assert.Equal(["(", ")"], typed);
        });
    }

    [Fact]
    public void Paste_DoesNotRaise()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["abc"], LanguageId), (editor, model, viewModel) =>
        {
            var raised = 0;
            viewModel.DidType += (_, _) => raised++;
            MoveTo(viewModel, 1, 4);

            viewModel.Paste("xy", false);

            Assert.Equal("abcxy", model.GetLineContent(1));
            Assert.Equal(0, raised);
        });
    }

    [Fact]
    public void DidType_RaisedAfterTheCursorEvent()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["abc"], LanguageId), (editor, model, viewModel) =>
        {
            MoveTo(viewModel, 1, 4);
            var order = new List<string>();
            viewModel.EventDispatcher.OnEvent += (_, e) =>
            {
                if (e is CursorStateChangedEvent)
                {
                    order.Add("cursor");
                }
            };
            viewModel.DidType += (_, _) => order.Add("didType");

            viewModel.Type("x", "keyboard");

            Assert.Equal(["cursor", "didType"], order);
        });
    }

    [Fact]
    public void Type_RaisesWillTypeBeforeTheEdit()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["abc"], LanguageId), (editor, model, viewModel) =>
        {
            string? seen = null;
            string? typed = null;
            viewModel.WillType += (_, text) =>
            {
                seen = model.GetLineContent(1);
                typed = text;
            };
            MoveTo(viewModel, 1, 4);

            viewModel.Type("x", "keyboard");

            Assert.Equal("abc", seen);
            Assert.Equal("x", typed);
            Assert.Equal("abcx", model.GetLineContent(1));
        });
    }

    [Fact]
    public void Type_WillTypeHandlerCanEdit()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["abc"], LanguageId), (editor, model, viewModel) =>
        {
            MoveTo(viewModel, 1, 4);
            var edited = false;
            viewModel.WillType += (_, _) =>
            {
                if (!edited)
                {
                    edited = true;
                    viewModel.ExecuteEdits("test", [new IdentifiedEditOperation(new Range(1, 1, 1, 1), ">")], _ => null);
                }
            };

            viewModel.Type("x", "keyboard");

            Assert.Equal(">abcx", model.GetLineContent(1));
        });
    }

    [Fact]
    public void Paste_DoesNotRaiseWillType()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["abc"], LanguageId), (editor, model, viewModel) =>
        {
            var raised = 0;
            viewModel.WillType += (_, _) => raised++;
            MoveTo(viewModel, 1, 4);

            viewModel.Paste("xy", false);

            Assert.Equal(0, raised);
        });
    }
}
