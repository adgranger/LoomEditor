namespace LoomEditor.View.Controller;

/// <summary>Reads and writes the system clipboard (Monaco: <c>IClipboardService</c>, the text part).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IClipboardService
{
    /// <summary>Puts <paramref name="text"/> on the clipboard. Does nothing if the clipboard cannot be written.</summary>
    void WriteText(string text);

    /// <summary>The text on the clipboard, or <see langword="null"/> if there is none or it cannot be read.</summary>
    Task<string?> ReadTextAsync();
}
