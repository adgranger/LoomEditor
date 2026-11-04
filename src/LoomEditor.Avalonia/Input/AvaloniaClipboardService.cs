using Avalonia.Input.Platform;
using LoomEditor.View.Controller;

namespace LoomEditor.Avalonia.Input;

/// <summary>The system clipboard of the window the editor is in (text only).</summary>
/// <remarks>
/// The clipboard can be held by another process or missing (headless): a write is then lost and a read gives
/// <see langword="null"/>. Threading: UI thread only.
/// </remarks>
internal sealed class AvaloniaClipboardService(Func<IClipboard?> clipboard) : IClipboardService
{
    /// <inheritdoc />
    public void WriteText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (clipboard() is not { } target)
        {
            return;
        }

        // Fire and forget: the write finishes on its own, a failure (clipboard held by another process) is dropped.
        _ = WriteAsync(target, text);
    }

    /// <inheritdoc />
    public async Task<string?> ReadTextAsync()
    {
        if (clipboard() is not { } source)
        {
            return null;
        }

        try
        {
            return await source.TryGetTextAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.ExternalException)
        {
            // Another process holds the clipboard.
            return null;
        }
    }

    private static async Task WriteAsync(IClipboard target, string text)
    {
        try
        {
            await target.SetTextAsync(text);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.ExternalException)
        {
            // Another process holds the clipboard.
        }
    }
}
