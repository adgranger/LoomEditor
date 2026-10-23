// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findWidget.ts (_getAriaLabel), findController.ts (MatchFindAction), wordHighlighter.ts (moveNext), toggleTabFocusMode.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Accessibility;

/// <summary>The texts Monaco announces to screen readers (English only).</summary>
/// <remarks>Threading: pure functions; thread-safe.</remarks>
internal static class AccessibilityMessages
{
    /// <summary>The find label when there is no match (Monaco: <c>NLS_NO_RESULTS</c>).</summary>
    public const string NoResults = "No results";

    /// <summary>Announced when Tab starts moving focus (Monaco: <c>toggleTabFocusMode</c>).</summary>
    public const string TabMovesFocusOn = "Pressing Tab will now move focus to the next focusable element";

    /// <summary>Announced when Tab inserts the tab character again.</summary>
    public const string TabMovesFocusOff = "Pressing Tab will now insert the tab character";

    /// <summary>The message of the find widget for a label such as "3 of 5" (Monaco: <c>_getAriaLabel</c>).</summary>
    public static string FindResult(string label, Range? currentMatch, string searchString, TextModel? model)
    {
        if (label == NoResults)
        {
            return searchString == "" ? $"{label} found" : $"{label} found for '{searchString}'";
        }

        if (currentMatch is { } match)
        {
            int line = match.StartLineNumber;
            string ariaLabel = string.Create(
                CultureInfo.InvariantCulture,
                $"{label} found for '{searchString}', at {line}:{match.StartColumn}");
            if (model is not null && line >= 1 && line <= model.LineCount)
            {
                return $"{model.GetLineContent(line)}, {ariaLabel}";
            }

            return ariaLabel;
        }

        return $"{label} found for '{searchString}'";
    }

    /// <summary>The message of a repeated find with no result (Monaco: <c>MatchFindAction</c>).</summary>
    public static string FindNoResultsAgain(string searchString) => $"{NoResults} found for '{searchString}'";

    /// <summary>The message of a word highlight move; <paramref name="index"/> is 1-based (Monaco: <c>moveNext</c>).</summary>
    public static string WordHighlightPosition(string lineContent, int index, int count, string word) =>
        string.Create(CultureInfo.InvariantCulture, $"{lineContent}, {index} of {count} for '{word}'");
}
