using LoomEditor.Core.Languages;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Comment;

namespace LoomEditor.View.Tests.Features.Comment;

public class CommentFeatureTests
{
    private const string LanguageId = "typescriptMode";

    private sealed class CommentEditor : IDisposable
    {
        private readonly EditorFeatureHost _host;

        public CommentEditor(string text, bool withComments = true)
        {
            Context = new EditorContext();
            if (withComments)
            {
                Context.RegisterLanguage(LanguageId);
                Context.Model.LanguageConfigurationRegistry.Register(LanguageId, new LanguageConfiguration
                {
                    Comments = new CommentRule(new LineCommentConfig("//"), new CharacterPair("/*", "*/")),
                });
                Context.Model.Tokenization.SetLanguageId(LanguageId);
            }

            Context.SetText(text);
            Feature = new CommentFeature();
            _host = new EditorFeatureHost(Context, [() => Feature]);
        }

        public EditorContext Context { get; }

        public CommentFeature Feature { get; }

        public string Text => Context.Model.GetValue();

        public void Dispose()
        {
            _host.Dispose();
            Context.Dispose();
        }
    }

    [Fact]
    public void CommentLine_TypeScriptDocument_TogglesSlashSlashOnEverySelectedLine()
    {
        using var editor = new CommentEditor("one\n  two\nthree");
        editor.Context.Selection = new Selection(1, 1, 3, 3);

        editor.Context.Commands.Execute(CommentCommands.CommentLine);
        Assert.Equal("// one\n//   two\n// three", editor.Text);

        editor.Context.Commands.Execute(CommentCommands.CommentLine);
        Assert.Equal("one\n  two\nthree", editor.Text);
    }

    [Fact]
    public void CommentLine_TwoCursorsOnTheSameLine_CommentsTheLineOnce()
    {
        using var editor = new CommentEditor("one\ntwo");
        editor.Context.SetSelections([new Selection(1, 1, 1, 1), new Selection(1, 3, 1, 3)]);

        editor.Context.Commands.Execute(CommentCommands.CommentLine);

        Assert.Equal("// one\ntwo", editor.Text);
    }

    [Fact]
    public void AddAndRemoveCommentLine_RunOnPlainText_DoNothing()
    {
        using var editor = new CommentEditor("one\ntwo", withComments: false);
        editor.Context.Selection = new Selection(1, 1, 2, 4);

        editor.Context.Commands.Execute(CommentCommands.AddCommentLine);
        editor.Context.Commands.Execute(CommentCommands.RemoveCommentLine);

        Assert.Equal("one\ntwo", editor.Text);
    }

    [Fact]
    public void AddCommentLine_ThenRemoveCommentLine_RestoresTheText()
    {
        using var editor = new CommentEditor("one\ntwo");
        editor.Context.Selection = new Selection(1, 1, 2, 4);

        editor.Context.Commands.Execute(CommentCommands.AddCommentLine);
        Assert.Equal("// one\n// two", editor.Text);

        editor.Context.Commands.Execute(CommentCommands.RemoveCommentLine);
        Assert.Equal("one\ntwo", editor.Text);
    }

    [Fact]
    public void BlockComment_WithSelection_WrapsItAndUndoRestoresTheText()
    {
        using var editor = new CommentEditor("some text");
        editor.Context.Selection = new Selection(1, 1, 1, 5);

        editor.Context.Commands.Execute(CommentCommands.BlockComment);
        Assert.Equal("/* some */ text", editor.Text);

        editor.Context.Model.Undo();
        Assert.Equal("some text", editor.Text);
    }

    [Fact]
    public void CommentLine_ReadOnly_DoesNotRun()
    {
        using var editor = new CommentEditor("one");
        editor.Context.ViewModel.SetCursorOptions(new EditorCursorOptions { ReadOnly = true });

        bool ran = editor.Context.Commands.Execute(CommentCommands.CommentLine);

        Assert.False(ran);
        Assert.Equal("one", editor.Text);
    }

    [Fact]
    public void Options_InsertSpaceFalse_AddsTheTokenWithoutSpace()
    {
        using var editor = new CommentEditor("one");
        editor.Feature.Options = new EditorCommentsOptions(InsertSpace: false);

        editor.Context.Commands.Execute(CommentCommands.CommentLine);

        Assert.Equal("//one", editor.Text);
    }

    [Fact]
    public void Keybindings_CommentKeys_ResolveToTheCommentCommands()
    {
        var focused = new KeybindingContext(TextInputFocus: true, EditorTextFocus: true, Writable: true);
        var dispatcher = new KeybindingDispatcher(KeybindingResolver.Default);

        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Ctrl, KeyCode.Slash), focused, out string? commentLine));
        Assert.Equal("editor.action.commentLine", commentLine);

        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK), focused, out string? first));
        Assert.Null(first);
        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyC), focused, out string? addCommentLine));
        Assert.Equal("editor.action.addCommentLine", addCommentLine);

        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Shift | KeyModifiers.Alt, KeyCode.KeyA), focused, out string? blockComment));
        Assert.Equal("editor.action.blockComment", blockComment);
    }

    [Fact]
    public void Keybindings_NotWritable_ResolveToNothing()
    {
        var readOnly = new KeybindingContext(TextInputFocus: true, EditorTextFocus: true, Writable: false);

        Assert.Null(KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.Ctrl, KeyCode.Slash), readOnly));
        // Ctrl+K starts the folding chords, which do not need a writable editor; the comment chord itself resolves to nothing.
        var ctrlK = new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK);
        Assert.Equal(KeybindingResolutionKind.NoMatchingKb, KeybindingResolver.Default.Resolve(readOnly, ctrlK, new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyC)).Kind);
    }
}
