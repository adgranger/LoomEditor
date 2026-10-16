using System.Runtime.InteropServices;
using LoomEditor.View.Controller;
using Windows.ApplicationModel.DataTransfer;

namespace LoomEditor.WinUI.Input;

/// <summary>The Windows clipboard (text only).</summary>
/// <remarks>
/// The clipboard can be held by another process: <see cref="COMException"/>s are caught, a write is then lost and a read gives
/// <see langword="null"/>. Threading: UI thread only.
/// </remarks>
internal sealed class WindowsClipboardService : IClipboardService
{
    /// <inheritdoc />
    public void WriteText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
        }
        catch (COMException)
        {
            // Another process holds the clipboard.
        }
    }

    /// <inheritdoc />
    public async Task<string?> ReadTextAsync()
    {
        try
        {
            DataPackageView content = Clipboard.GetContent();
            return content.Contains(StandardDataFormats.Text) ? await content.GetTextAsync() : null;
        }
        catch (COMException)
        {
            // Another process holds the clipboard.
            return null;
        }
    }
}
