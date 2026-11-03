using LoomEditor.Core.Comments;

namespace LoomEditor.Avalonia.Controls.Comments;

// P15-23 deletes this stub: the comment zone host (the boxes over the threads' zones) does not exist yet, so the editor has none.
internal interface ICommentZoneHost
{
    void FocusComposer(CommentThread thread);
}
