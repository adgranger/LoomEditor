// Ported from microsoft/vscode: src/vs/editor/contrib/clipboard/browser/clipboard.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.View.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Controller;

/// <summary>The cut, copy and paste commands, over an <see cref="IClipboardService"/>.</summary>
/// <remarks>
/// <para>
/// Monaco asks the browser (<c>execCommand</c>) on the desktop and uses the clipboard service on the web; the web path is what
/// is ported (ADR 0006, decision 10). The clipboard metadata live in memory only (no custom clipboard format, no JSON).
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal static class ClipboardCommands
{
    /// <summary>Registers <c>editor.action.clipboardCutAction</c>, <c>…CopyAction</c> and <c>…PasteAction</c>.</summary>
    /// <returns>Dispose it to unregister the three commands.</returns>
    public static IDisposable Register(CommandRegistry registry, ViewModel viewModel, IClipboardService clipboard)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(clipboard);

        IDisposable[] registrations =
        [
            registry.Register(new EditorCommand(DefaultKeybindings.ClipboardCopy, () => Copy(viewModel, clipboard))),
            registry.Register(new EditorCommand(DefaultKeybindings.ClipboardCut, () => Cut(viewModel, clipboard))),
            registry.Register(new EditorCommand(DefaultKeybindings.ClipboardPaste, () => StartPaste(viewModel, clipboard))),
        ];
        return new Registrations(registrations);
    }

    /// <summary>Copies the selections to the clipboard (Monaco: <c>registerExecCommandImpl</c> for <c>copy</c>).</summary>
    public static void Copy(ViewModel viewModel, IClipboardService clipboard)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(clipboard);

        // Do not execute if there is no selection and empty selection clipboard is off.
        if (viewModel.GetSelection().IsEmpty && !viewModel.CursorOptions.EmptySelectionClipboard)
        {
            return;
        }

        (ClipboardDataToCopy dataToCopy, _) = ClipboardUtils.GenerateDataToCopyAndStoreInMemory(viewModel);
        clipboard.WriteText(dataToCopy.Text);
    }

    /// <summary>
    /// Copies the selections, then cuts them. A read-only editor copies but does not delete (the cursor refuses the edit).
    /// </summary>
    public static void Cut(ViewModel viewModel, IClipboardService clipboard)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(clipboard);

        if (viewModel.GetSelection().IsEmpty && !viewModel.CursorOptions.EmptySelectionClipboard)
        {
            return;
        }

        Copy(viewModel, clipboard);
        viewModel.Cut("keyboard");
    }

    /// <summary>Pastes the clipboard text, with the metadata of the last copy of this process when the text is still that copy.</summary>
    public static async Task PasteAsync(ViewModel viewModel, IClipboardService clipboard)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(clipboard);

        string? clipboardText = await clipboard.ReadTextAsync();
        if (string.IsNullOrEmpty(clipboardText))
        {
            return;
        }

        ClipboardStoredMetadata? metadata = InMemoryClipboardMetadataManager.Instance.Get(clipboardText);
        bool pasteOnNewLine = false;
        if (metadata is not null)
        {
            pasteOnNewLine = viewModel.CursorOptions.EmptySelectionClipboard && metadata.IsFromEmptySelection;
        }

        viewModel.Paste(clipboardText, pasteOnNewLine, metadata?.MulticursorText, "keyboard", metadata?.IsBlock ?? false);
    }

    // The command is synchronous: it starts the paste on the UI thread and returns. A failed clipboard read must not crash
    // the app, so the exception is written to the debug output and swallowed.
    private static void StartPaste(ViewModel viewModel, IClipboardService clipboard) => _ = RunPasteAsync(viewModel, clipboard);

    private static async Task RunPasteAsync(ViewModel viewModel, IClipboardService clipboard)
    {
        try
        {
            await PasteAsync(viewModel, clipboard);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Paste failed: {exception}");
        }
    }

    private sealed class Registrations(IDisposable[] registrations) : IDisposable
    {
        public void Dispose()
        {
            foreach (IDisposable registration in registrations)
            {
                registration.Dispose();
            }
        }
    }
}
