using LoomEditor.View.Features.Snippet;
using LoomEditor.WinUI.Input;

namespace LoomEditor.WinUI.Controls;

public sealed partial class LoomEditor
{
    /// <summary>
    /// Inserts a snippet (TextMate syntax: <c>${1:default}</c>, <c>$0</c>, <c>${TM_SELECTED_TEXT}</c>, ...) at every cursor and enters
    /// snippet mode: <c>Tab</c> and <c>Shift+Tab</c> move between the tab stops, <c>Escape</c> leaves.
    /// </summary>
    /// <param name="template">The snippet template.</param>
    /// <remarks>Does nothing when the editor is read-only. A nested call merges into the active tab stop.</remarks>
    public void InsertSnippet(string template) => InsertSnippet(template, 0, 0);

    /// <summary>
    /// Inserts a snippet at every cursor after replacing <paramref name="overwriteBefore"/> characters before and
    /// <paramref name="overwriteAfter"/> characters after each cursor.
    /// </summary>
    /// <param name="template">The snippet template.</param>
    /// <param name="overwriteBefore">The number of characters before each cursor that the snippet replaces.</param>
    /// <param name="overwriteAfter">The number of characters after each cursor that the snippet replaces.</param>
    public void InsertSnippet(string template, int overwriteBefore, int overwriteAfter)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentOutOfRangeException.ThrowIfNegative(overwriteBefore);
        ArgumentOutOfRangeException.ThrowIfNegative(overwriteAfter);

        var options = new SnippetInsertOptions { OverwriteBefore = overwriteBefore, OverwriteAfter = overwriteAfter };
        SnippetController? controller = _context.SnippetController;
        if (controller is null)
        {
            return;
        }

        if (!template.Contains("CLIPBOARD", StringComparison.Ordinal))
        {
            controller.Insert(template, options);
            return;
        }

        // The clipboard can only be read asynchronously: read it first, then insert on the UI thread.
        _ = InsertWithClipboardAsync(controller, template, options);
    }

    private static async Task InsertWithClipboardAsync(SnippetController controller, string template, SnippetInsertOptions options)
    {
        var clipboard = new WindowsClipboardService();
        string? text = await clipboard.ReadTextAsync();
        controller.Insert(template, options with { ClipboardText = text });
    }
}
