// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestController.ts (the command ids)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Suggest;

/// <summary>Ids of the commands exposed by <see cref="SuggestFeature"/>. Same ids as Monaco.</summary>
public static class SuggestCommands
{
    /// <summary>Opens the completion list (Ctrl+Space, Ctrl+I).</summary>
    public const string TriggerSuggest = "editor.action.triggerSuggest";

    /// <summary>Inserts the selected item (Tab, Enter).</summary>
    public const string AcceptSelectedSuggestion = "acceptSelectedSuggestion";

    /// <summary>Closes the completion list (Escape).</summary>
    public const string HideSuggestWidget = "hideSuggestWidget";

    /// <summary>Selects the next item (Down).</summary>
    public const string SelectNextSuggestion = "selectNextSuggestion";

    /// <summary>Selects the previous item (Up).</summary>
    public const string SelectPrevSuggestion = "selectPrevSuggestion";

    /// <summary>Selects the next page of items (PageDown).</summary>
    public const string SelectNextPageSuggestion = "selectNextPageSuggestion";

    /// <summary>Selects the previous page of items (PageUp).</summary>
    public const string SelectPrevPageSuggestion = "selectPrevPageSuggestion";

    /// <summary>Selects the first item (no default key).</summary>
    public const string SelectFirstSuggestion = "selectFirstSuggestion";

    /// <summary>Selects the last item (no default key).</summary>
    public const string SelectLastSuggestion = "selectLastSuggestion";
}
