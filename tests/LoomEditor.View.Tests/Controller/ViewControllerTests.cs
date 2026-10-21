using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Commands;
using LoomEditor.View.Controller;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Controller;

// Monaco has no test for dispatchMouse (it is covered by the editor's integration tests): all tests are new.
public class ViewControllerTests
{
    private const string Text = "just some text\nand another line\nand another one";

    private static MouseDispatchData Data(
        int lineNumber,
        int column,
        int mouseDownCount = 1,
        bool inSelectionMode = false,
        bool startedOnLineNumbers = false,
        bool alt = false,
        bool shift = false,
        bool ctrl = false,
        bool middle = false) =>
        new(
            new Position(lineNumber, column), column, NavigationCommandRevealType.Regular, startedOnLineNumbers, inSelectionMode,
            mouseDownCount, alt, ctrl, false, shift, !middle, middle, false);

    private static void WithController(Action<TestCodeEditor, ViewController> callback, TestCodeEditorOptions? options = null) =>
        TestCodeEditor.WithTestCodeEditor(Text, editor => callback(editor, new ViewController(editor.ViewModel)), options);

    [Fact]
    public void DispatchMouse_PlainClick_MovesTheCursor()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(2, 5));

            Assert.Equal(new Selection(2, 5, 2, 5), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_ShiftClick_ExtendsTheSelection()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3));
            controller.DispatchMouse(Data(2, 5, inSelectionMode: true, shift: true));

            Assert.Equal(new Selection(1, 3, 2, 5), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_DoubleClick_SelectsTheWord()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 8, mouseDownCount: 2));

            Assert.Equal(new Selection(1, 6, 1, 10), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_TripleClick_SelectsTheLine()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(2, 4, mouseDownCount: 3));

            Assert.Equal(new Selection(2, 1, 3, 1), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_FourClicks_SelectsEverything()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(2, 4, mouseDownCount: 4));

            Assert.Equal(new Selection(1, 1, 3, 16), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_AltClick_AddsASecondCursor()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3));
            controller.DispatchMouse(Data(3, 5, alt: true));

            Assert.Equal([new Selection(1, 3, 1, 3), new Selection(3, 5, 3, 5)], editor.ViewModel.GetSelections());
        });
    }

    [Fact]
    public void DispatchMouse_AltClickOnASecondaryCursor_RemovesIt()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3));
            controller.DispatchMouse(Data(3, 5, alt: true));

            controller.DispatchMouse(Data(3, 5, alt: true));

            Assert.Single(editor.ViewModel.GetSelections());
        });
    }

    [Fact]
    public void DispatchMouse_AltAndCtrlClick_DoesNothing()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3));

            controller.DispatchMouse(Data(3, 5, alt: true, ctrl: true));

            Assert.Equal(new Selection(1, 3, 1, 3), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_CtrlIsTheModifier_CtrlClickAddsACursor()
    {
        WithController(
            (editor, controller) =>
            {
                controller.DispatchMouse(Data(1, 3));
                controller.DispatchMouse(Data(3, 5, ctrl: true));

                Assert.Equal(2, editor.ViewModel.GetSelections().Count);
            },
            new TestCodeEditorOptions(CursorOptions: new EditorCursorOptions { MultiCursorModifier = MultiCursorModifier.CtrlCmd }));
    }

    [Fact]
    public void DispatchMouse_AltShiftDrag_MakesAColumnSelection()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3));

            controller.DispatchMouse(Data(3, 5, inSelectionMode: true, alt: true, shift: true));

            Assert.Equal(3, editor.ViewModel.GetSelections().Count);
        });
    }

    [Fact]
    public void DispatchMouse_MiddleButton_MakesAColumnSelection()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3, middle: true));

            controller.DispatchMouse(Data(3, 5, inSelectionMode: true, middle: true));

            Assert.Equal(3, editor.ViewModel.GetSelections().Count);
        });
    }

    [Fact]
    public void DispatchMouse_StartedOnLineNumbers_SelectsTheLine()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(2, 1, startedOnLineNumbers: true));

            Assert.Equal(new Selection(2, 1, 3, 1), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_DragStartedOnLineNumbers_ExtendsTheLineSelection()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 1, startedOnLineNumbers: true));

            controller.DispatchMouse(Data(2, 1, startedOnLineNumbers: true, inSelectionMode: true));

            Assert.Equal(new Selection(1, 1, 3, 1), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_AltClickOnLineNumbers_AddsAWholeLineCursor()
    {
        WithController((editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3));

            controller.DispatchMouse(Data(3, 1, startedOnLineNumbers: true, alt: true));

            Assert.Equal(2, editor.ViewModel.GetSelections().Count);
            Assert.Equal(new Selection(3, 1, 3, 16), editor.ViewModel.GetSelections()[1]);
        });
    }

    [Fact]
    public void DispatchMouse_ColumnSelectionOption_ShiftDragMakesAColumnSelection()
    {
        WithController(
            (editor, controller) =>
            {
                controller.DispatchMouse(Data(1, 3));

                controller.DispatchMouse(Data(3, 5, inSelectionMode: true, shift: true));

                Assert.Equal(3, editor.ViewModel.GetSelections().Count);
            },
            new TestCodeEditorOptions(CursorOptions: new EditorCursorOptions { ColumnSelection = true }));
    }

    // Every string literal of the line is one string token; the rest is `Other`.
    private sealed class StringSupport : ITokenizationSupport
    {
        private const uint OtherMeta = (uint)StandardTokenType.Other << MetadataConsts.TokenTypeOffset;
        private const uint StringMeta = (uint)StandardTokenType.String << MetadataConsts.TokenTypeOffset;

        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
        {
            int start = line.IndexOf('"');
            int end = line.LastIndexOf('"');
            if (start < 0 || end == start)
            {
                return new([0, OtherMeta], NullState.Instance);
            }

            var tokens = new List<uint>();
            if (start > 0)
            {
                tokens.AddRange([0, OtherMeta]);
            }

            tokens.AddRange([(uint)start, StringMeta]);
            if (end + 1 < line.Length)
            {
                tokens.AddRange([(uint)(end + 1), OtherMeta]);
            }

            return new([.. tokens], NullState.Instance);
        }
    }

    private static void WithText(string text, Action<TestCodeEditor, ViewController> callback, TestCodeEditorOptions? options = null)
    {
        var registry = new TokenizationRegistry();
        registry.Register("strings", new StringSupport());
        var codec = new LanguageIdCodec();
        codec.Register("strings");
        var configurations = new LanguageConfigurationRegistry();
        configurations.Register("strings", new LanguageConfiguration { Brackets = [new("(", ")"), new("[", "]"), new("{", "}")] });
        var model = new TextModel(text, DefaultEndOfLine.LF, null, registry, codec, null, configurations);
        model.Tokenization.SetLanguageId("strings");
        TestCodeEditor.WithTestCodeEditor(model, editor => callback(editor, new ViewController(editor.ViewModel)), options);
    }

    [Fact]
    public void DispatchMouse_DoubleClickAfterOpeningBracket_SelectsPairContent()
    {
        WithText("f(abc def)", (editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 3, mouseDownCount: 2));

            Assert.Equal(new Selection(1, 3, 1, 10), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_DoubleClickBeforeClosingBracket_SelectsPairContent()
    {
        WithText("f(abc def)", (editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 10, mouseDownCount: 2));

            Assert.Equal(new Selection(1, 3, 1, 10), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_DoubleClickInsideWordBetweenBrackets_SelectsTheWord()
    {
        WithText("f(abc def)", (editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 5, mouseDownCount: 2));

            Assert.Equal(new Selection(1, 3, 1, 6), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_DoubleClickAfterOpeningQuote_SelectsStringContent()
    {
        WithText("x = \"hello world\"", (editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 6, mouseDownCount: 2));

            Assert.Equal(new Selection(1, 6, 1, 17), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_DoubleClickBeforeClosingQuote_SelectsStringContent()
    {
        WithText("x = \"hello world\"", (editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 17, mouseDownCount: 2));

            Assert.Equal(new Selection(1, 6, 1, 17), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_DoubleClickInStringWithRtlText_SelectsTheWord()
    {
        WithText("\"של ום\"", (editor, controller) =>
        {
            controller.DispatchMouse(Data(1, 2, mouseDownCount: 2));

            Assert.Equal(new Selection(1, 2, 1, 4), editor.ViewModel.GetSelection());
        });
    }

    [Fact]
    public void DispatchMouse_DoubleClickSelectsBlockOff_SelectsTheWord()
    {
        WithText(
            "f(abc def)",
            (editor, controller) =>
            {
                controller.DispatchMouse(Data(1, 3, mouseDownCount: 2));

                Assert.Equal(new Selection(1, 3, 1, 6), editor.ViewModel.GetSelection());
            },
            new TestCodeEditorOptions(CursorOptions: new EditorCursorOptions { DoubleClickSelectsBlock = false }));
    }
}
