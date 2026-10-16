// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Events;

namespace LoomEditor.View.Cursors;

// The typing entry point of the controller.
internal sealed partial class CursorsController
{
    /// <summary>Types <paramref name="text"/> at every cursor (Monaco: <c>type</c>).</summary>
    /// <remarks>
    /// A text that comes straight from the keyboard (<paramref name="source"/> is <c>"keyboard"</c>) is typed grapheme
    /// by grapheme, so that Enter is recognized. Any other text is inserted as it is.
    /// </remarks>
    public void Type(ViewModelEventsCollector eventsCollector, string text, string? source = null)
    {
        ExecuteEdit(
            () =>
            {
                if (source == "keyboard")
                {
                    // If this event is coming straight from the keyboard, look for electric characters and enter
                    int len = text.Length;
                    int offset = 0;
                    while (offset < len)
                    {
                        int charLength = Strings.NextCharLength(text, offset);
                        string chr = text.Substring(offset, charLength);

                        // Here we must interpret each typed character individually
                        ExecuteEditOperation(TypeOperations.TypeWithInterceptors(_compositionState is not null,_prevEditOperationType, Context.CursorConfig, _model, GetSelections(), chr));

                        offset += charLength;
                    }
                }
                else
                {
                    ExecuteEditOperation(TypeOperations.TypeWithoutInterceptors(_prevEditOperationType, GetSelections(), text));
                }
            },
            eventsCollector,
            source);
    }

    /// <summary>Pastes <paramref name="text"/> at every cursor (Monaco: <c>paste</c>).</summary>
    public void Paste(
        ViewModelEventsCollector eventsCollector,
        string text,
        bool pasteOnNewLine,
        IReadOnlyList<string>? multicursorText,
        string? source = null,
        bool isBlock = false)
    {
        ExecuteEdit(
            () => ExecuteEditOperation(TypeOperations.Paste(Context.CursorConfig, GetSelections(), text, pasteOnNewLine, multicursorText ?? [], isBlock)),
            eventsCollector,
            source,
            CursorChangeReason.Paste);
    }

    /// <summary>Cuts the selections, or the whole lines of empty selections (Monaco: <c>cut</c>).</summary>
    public void Cut(ViewModelEventsCollector eventsCollector, string? source = null)
    {
        ExecuteEdit(
            () => ExecuteEditOperation(DeleteOperations.Cut(Context.CursorConfig, _viewModel, GetSelections())),
            eventsCollector,
            source);
    }
}
