// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findWidget.ts (_getAriaLabel), findController.ts (MatchFindAction), wordHighlighter.ts (moveNext), toggleTabFocusMode.ts, diffEditor/components/accessibleDiffViewer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using LoomEditor.Core.Model;
using LoomEditor.View.DiffEditor;

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

    /// <summary>The message when go to definition finds nothing (Monaco: <c>noResultWord</c> / <c>generic.noResult</c>).</summary>
    public static string NoDefinitionFound(string? word) =>
        string.IsNullOrEmpty(word) ? "No definition found" : $"No definition found for '{word}'";

    /// <summary>The message when the definition is in a document that cannot be opened.</summary>
    public static string DefinitionInOtherDocument(string uri) => $"Definition in {uri}";

    /// <summary>The message after accepting an item with additional edits (Monaco: <c>aria.alert.snippet</c>).</summary>
    public static string SuggestAdditionalEdits(string label, int count) =>
        string.Create(CultureInfo.InvariantCulture, $"Accepting '{label}' made {count} additional edits");

    /// <summary>The label of the list of the accessible diff viewer (Monaco: <c>accessibleDiffViewer.ts</c>, <c>ariaLabel</c>).</summary>
    public const string DiffViewerListLabel = "Accessible Diff Viewer. Use arrow up and down to navigate.";

    /// <summary>"no lines changed", "1 line changed" or "{n} lines changed".</summary>
    public static string DiffChangedLines(int lineCount) => lineCount switch
    {
        0 => "no lines changed",
        1 => "1 line changed",
        _ => string.Create(CultureInfo.InvariantCulture, $"{lineCount} lines changed"),
    };

    /// <summary>The label of the header of a group; <paramref name="index"/> is 1-based.</summary>
    public static string DiffHeaderLabel(int index, int count, int originalStart, int originalLength, int modifiedStart, int modifiedLength) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"Difference {index} of {count}: original line {originalStart}, {DiffChangedLines(originalLength)}, modified line {modifiedStart}, {DiffChangedLines(modifiedLength)}");

    /// <summary>The text of the header of a group, like <c>1/2: @@ -3,2 +3,4 @@</c>; <paramref name="index"/> is 1-based.</summary>
    public static string DiffHeaderText(int index, int count, int originalStart, int originalLength, int modifiedStart, int modifiedLength) =>
        string.Create(CultureInfo.InvariantCulture, $"{index}/{count}: @@ -{originalStart},{originalLength} +{modifiedStart},{modifiedLength} @@");

    /// <summary>The label of an unchanged, deleted or added row; an empty line is announced as "blank".</summary>
    public static string DiffRowLabel(LineType type, string content, int? originalLine, int? modifiedLine)
    {
        string text = content.Length == 0 ? "blank" : content;
        return type switch
        {
            LineType.Unchanged when originalLine == modifiedLine => string.Create(CultureInfo.InvariantCulture, $"{text} unchanged line {originalLine}"),
            LineType.Unchanged => string.Create(CultureInfo.InvariantCulture, $"{text} original line {originalLine} modified line {modifiedLine}"),
            LineType.Added => string.Create(CultureInfo.InvariantCulture, $"+ {text} modified line {modifiedLine}"),
            LineType.Deleted => string.Create(CultureInfo.InvariantCulture, $"- {text} original line {originalLine}"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "A header has no row label."),
        };
    }
}
