// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetVariables.ts (SelectionBasedVariableResolver)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using System.Text.RegularExpressions;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Snippets;

/// <summary>Resolves the variables that depend on the selection and the text around it (Monaco: <c>SelectionBasedVariableResolver</c>).</summary>
/// <remarks>
/// Not ported: <c>OvertypingCapturer</c> (the suggest widget's overtype tracking), so <c>TM_SELECTED_TEXT</c> is unknown when
/// nothing is selected. Threading: UI thread only; resolvers are created per insertion.
/// </remarks>
internal sealed partial class SelectionBasedVariableResolver(TextModel model, Selection selection, int selectionIdx) : IVariableResolver
{
    /// <inheritdoc />
    public string? Resolve(Variable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        CultureInfo culture = CultureInfo.InvariantCulture;
        switch (variable.Name)
        {
            case "SELECTION" or "TM_SELECTED_TEXT":
                return ResolveSelectedText(variable);
            case "TM_CURRENT_LINE":
                return model.GetLineContent(selection.Position.LineNumber);
            case "TM_CURRENT_WORD":
                return model.GetWordAtPosition(selection.Position) is { Word.Length: > 0 } word ? word.Word : null;
            case "TM_LINE_INDEX":
                return (selection.Position.LineNumber - 1).ToString(culture);
            case "TM_LINE_NUMBER":
                return selection.Position.LineNumber.ToString(culture);
            case "CURSOR_INDEX":
                return selectionIdx.ToString(culture);
            case "CURSOR_NUMBER":
                return (selectionIdx + 1).ToString(culture);
            default:
                return null;
        }
    }

    private string? ResolveSelectedText(Variable variable)
    {
        Range range = selection.Range;
        string value = model.GetValueInRange(range);
        if (value.Length == 0)
        {
            // not ported: OvertypingCapturer
            return null;
        }

        if (range.StartLineNumber == range.EndLineNumber || variable.Snippet is not { } snippet)
        {
            return value;
        }

        // The selection is a multiline string whose indentation we now need to adjust: compare the indentation of this variable
        // with the indentation at the editor position and add the potential extra indentation to the value.
        string line = model.GetLineContent(range.StartLineNumber);
        string lineLeadingWhitespace = Strings.GetLeadingWhitespace(line, 0, range.StartColumn - 1);

        string varLeadingWhitespace = lineLeadingWhitespace;
        snippet.Walk(marker =>
        {
            if (marker == variable)
            {
                return false;
            }

            if (marker is TextMarker text)
            {
                varLeadingWhitespace = Strings.GetLeadingWhitespace(LineBreakRegex().Split(text.Value)[^1]);
            }

            return true;
        });
        string extra = varLeadingWhitespace[CommonPrefixLength(varLeadingWhitespace, lineLeadingWhitespace)..];
        return LineBreakRegex().Replace(value, match => match.Value + extra);
    }

    private static int CommonPrefixLength(string a, string b)
    {
        int length = Math.Min(a.Length, b.Length);
        for (int i = 0; i < length; i++)
        {
            if (a[i] != b[i])
            {
                return i;
            }
        }

        return length;
    }

    [GeneratedRegex(@"\r\n|\r|\n")]
    private static partial Regex LineBreakRegex();
}
