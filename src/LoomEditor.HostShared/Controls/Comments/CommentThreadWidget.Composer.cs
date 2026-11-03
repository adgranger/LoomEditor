using LoomEditor.View.Comments;

#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls.Comments;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls.Comments;
#endif

// What the composer's buttons do and how its parts are put together; the host part holds the controls, the focus, the key handling and the styles.
internal sealed partial class CommentThreadWidget
{
    private bool _focusOnLoaded;

    /// <summary>Raised when the composer closed from the keyboard or the Cancel button: the editor takes the focus back.</summary>
    public event EventHandler? FocusReturnRequested;

    private void CancelComposer()
    {
        // Escape only cancels an empty draft: only Cancel can discard text.
        bool discarded = !string.IsNullOrWhiteSpace(_viewModel.Draft);
        _viewModel.Cancel();
        if (discarded)
        {
            Announce?.Invoke(CommentStrings.CommentDiscarded);
        }

        FocusReturnRequested?.Invoke(this, EventArgs.Empty);
    }

    private void StartReply()
    {
        _viewModel.StartComposing();
        FocusComposer();
    }

    private void BuildComposerParts()
    {
        _toolbar = new CommentComposerToolbar(ApplyToolbarAction);
        _toolbarSlot.Children.Add(_toolbar);
        _composerButtons.Children.Add(_previewButton);
        _composerButtons.Children.Add(_cancelButton);
        _composerButtons.Children.Add(_submitButton);
        _composer.Children.Add(_toolbarSlot);
        _composer.Children.Add(_textBox);
        _composer.Children.Add(_preview);
        _composer.Children.Add(_composerButtons);
        _composerArea.Children.Add(_replyButton);
        _composerArea.Children.Add(_composer);
    }
}
