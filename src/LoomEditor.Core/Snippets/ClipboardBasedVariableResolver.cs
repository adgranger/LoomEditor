// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetVariables.ts (ClipboardBasedVariableResolver)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.Core.Snippets;

/// <summary>Resolves <c>CLIPBOARD</c> (Monaco: <c>ClipboardBasedVariableResolver</c>).</summary>
/// <remarks>
/// With <c>spread</c> each cursor gets one line of the clipboard when the number of non-blank lines equals the cursor count.
/// Threading: UI thread only; resolvers are created per insertion.
/// </remarks>
/// <param name="readClipboardText">Reads the clipboard text (the host's <see cref="ISnippetVariableHost.ReadClipboardText"/>).</param>
/// <param name="selectionIdx">The index of the cursor being inserted.</param>
/// <param name="selectionCount">The number of cursors.</param>
/// <param name="spread">Whether to give each cursor a line.</param>
internal sealed partial class ClipboardBasedVariableResolver(Func<string?> readClipboardText, int selectionIdx, int selectionCount, bool spread)
    : IVariableResolver
{
    /// <inheritdoc />
    public string? Resolve(Variable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        if (variable.Name != "CLIPBOARD")
        {
            return null;
        }

        string? clipboardText = readClipboardText();
        if (string.IsNullOrEmpty(clipboardText))
        {
            return null;
        }

        // Spread assigns each cursor a line of the clipboard text whenever the line count equals the cursor count.
        if (spread)
        {
            string[] lines = LineBreakRegex().Split(clipboardText).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            if (lines.Length == selectionCount)
            {
                return selectionIdx >= 0 && selectionIdx < lines.Length ? lines[selectionIdx] : null;
            }
        }

        return clipboardText;
    }

    [GeneratedRegex(@"\r\n|\n|\r")]
    private static partial Regex LineBreakRegex();
}
