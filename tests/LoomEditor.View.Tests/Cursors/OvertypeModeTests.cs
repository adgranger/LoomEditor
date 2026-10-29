// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Overtype Mode')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Overtype;
using LoomEditor.View.Tests.Accessibility.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public class OvertypeModeTests
{
    private static TestCodeEditorOptions Overtype(bool overtypeOnPaste = true) =>
        new(CursorOptions: new EditorCursorOptions { InputMode = EditorInputMode.Overtype, OvertypeOnPaste = overtypeOnPaste });

    // Monaco: 'simple type'
    [Fact]
    public void SimpleType()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["123456789", "123456789"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 3, 1, 3)]);
                editor.ViewModel.Type("a", "keyboard");
                Assert.Equal("12a456789\n123456789", editor.Model.GetValue());

                editor.ViewModel.SetSelections("test", [new Selection(1, 9, 1, 9)]);
                editor.ViewModel.Type("bbb", "keyboard");
                Assert.Equal("12a45678bbb\n123456789", editor.Model.GetValue());
            },
            Overtype());
    }

    // Monaco: 'multi-line selection type'
    [Fact]
    public void MultiLineSelectionType()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["123456789", "123456789"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 5, 2, 3)]);
                editor.ViewModel.Type("cc", "keyboard");
                Assert.Equal("1234cc456789", editor.Model.GetValue());
            },
            Overtype());
    }

    // Monaco: 'simple paste'
    [Fact]
    public void SimplePaste()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["123456789", "123456789"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 5, 1, 5)]);
                editor.ViewModel.Paste("cc", false);
                Assert.Equal("1234cc789\n123456789", editor.Model.GetValue());

                editor.ViewModel.SetSelections("test", [new Selection(1, 5, 1, 5)]);
                editor.ViewModel.Paste("dddddddd", false);
                Assert.Equal("1234dddddddd\n123456789", editor.Model.GetValue());
            },
            Overtype());
    }

    // Monaco: 'multi-line selection paste'
    [Fact]
    public void MultiLineSelectionPaste()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["123456789", "123456789"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 5, 2, 3)]);
                editor.ViewModel.Paste("cc", false);
                Assert.Equal("1234cc456789", editor.Model.GetValue());
            },
            Overtype());
    }

    // Monaco: 'paste multi-line text'
    [Fact]
    public void PasteMultiLineText()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["123456789", "123456789"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 5, 1, 5)]);
                editor.ViewModel.Paste("aaaaaaa\nbbbbbbb", false);
                Assert.Equal("1234aaaaaaa\nbbbbbbb\n123456789", editor.Model.GetValue());
            },
            Overtype());
    }

    // Monaco: 'composition type'
    [Fact]
    public void CompositionType()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["123456789", "123456789"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 5, 1, 5)]);
                editor.ViewModel.StartComposition();
                editor.ViewModel.CompositionType("セ", 0, 0, 0, "keyboard");
                Assert.Equal("1234セ56789\n123456789", editor.Model.GetValue());

                editor.ViewModel.EndComposition("keyboard");
                Assert.Equal("1234セ6789\n123456789", editor.Model.GetValue());
            },
            Overtype());
    }

    [Fact]
    public void Type_OvertypeAtEndOfLine_Appends()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["abc", "def"],
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 4);
                editor.ViewModel.Type("x", "keyboard");
                Assert.Equal("abcx\ndef", editor.Model.GetValue());
            },
            Overtype());
    }

    [Fact]
    public void Type_OvertypeWithSelection_ReplacesTheSelection()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["abcdef"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 2, 1, 4)]);
                editor.ViewModel.Type("x", "keyboard");
                Assert.Equal("axdef", editor.Model.GetValue());
            },
            Overtype());
    }

    [Fact]
    public void Type_InsertMode_Inserts()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["abcdef"],
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 3);
                editor.ViewModel.Type("x", "keyboard");
                Assert.Equal("abxcdef", editor.Model.GetValue());
            });
    }

    [Fact]
    public void Composition_Overtype_ReplacesFollowingCharacters()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["abcdef"],
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 2);
                editor.ViewModel.StartComposition();
                editor.ViewModel.CompositionType("にほ", 0, 0, 0, "keyboard");
                editor.ViewModel.EndComposition("keyboard");
                Assert.Equal("aにほdef", editor.Model.GetValue());
            },
            Overtype());
    }

    [Fact]
    public void Paste_OvertypeOnPasteTrue_Overtypes()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["abcdef"],
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 2);
                editor.ViewModel.Paste("XY", false);
                Assert.Equal("aXYdef", editor.Model.GetValue());
            },
            Overtype(overtypeOnPaste: true));
    }

    [Fact]
    public void Paste_OvertypeOnPasteFalse_Inserts()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["abcdef"],
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 2);
                editor.ViewModel.Paste("XY", false);
                Assert.Equal("aXYbcdef", editor.Model.GetValue());
            },
            Overtype(overtypeOnPaste: false));
    }

    [Fact]
    public void ToggleCommand_FlipsModeAndFiresEvent()
    {
        var recorder = new RecordingAnnouncer();
        using var context = new EditorContext { Announcer = recorder };
        new OvertypeFeature().Attach(context);
        int raised = 0;
        context.InputModeChanged += (_, _) => raised++;

        context.Commands.Execute(OvertypeCommands.ToggleOvertype);
        Assert.Equal(EditorInputMode.Overtype, context.InputMode);
        context.Commands.Execute(OvertypeCommands.ToggleOvertype);
        Assert.Equal(EditorInputMode.Insert, context.InputMode);

        Assert.Equal(2, raised);
        Assert.Equal([AccessibilityMessages.OvertypeModeOn, AccessibilityMessages.InsertModeOn], recorder.Messages);
    }

    [Fact]
    public void ToggleCommand_ReadOnly_DoesNothing()
    {
        using var context = new EditorContext { IsReadOnly = true };
        new OvertypeFeature().Attach(context);

        Assert.False(context.Commands.Execute(OvertypeCommands.ToggleOvertype));
        Assert.Equal(EditorInputMode.Insert, context.InputMode);
    }

    [Fact]
    public void ToggleCommand_TypingFollowsTheMode()
    {
        using var context = new EditorContext();
        new OvertypeFeature().Attach(context);
        context.SetText("abc");
        context.Selection = Selection.Collapsed(new Position(1, 1));

        context.Commands.Execute(OvertypeCommands.ToggleOvertype);
        context.ViewModel.Type("x", "keyboard");

        Assert.Equal("xbc", context.Model.GetValue());
    }

    [Fact]
    public void Type_OvertypeMultipleCursors()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["abcdef", "abcdef"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 2, 1, 2), new Selection(2, 4, 2, 4)]);
                editor.ViewModel.Type("x", "keyboard");
                Assert.Equal("axcdef\nabcxef", editor.Model.GetValue());
            },
            Overtype());
    }

    [Fact]
    public void Undo_AfterOvertypeRun_RestoresInOneStep()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["abcdef"],
            editor =>
            {
                MoveTo(editor.ViewModel, 1, 2);
                editor.ViewModel.Type("x", "keyboard");
                editor.ViewModel.Type("y", "keyboard");
                editor.ViewModel.Type("z", "keyboard");
                Assert.Equal("axyzef", editor.Model.GetValue());

                Undo(editor);
                Assert.Equal("abcdef", editor.Model.GetValue());
            },
            Overtype());
    }
}
