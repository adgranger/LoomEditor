// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;

namespace LoomEditor.View.Suggest;

/// <summary>
/// How a completion session starts (Monaco: <c>SuggestTriggerOptions</c> plus its <c>completionOptions</c>; <c>clipboardText</c> is not ported).
/// </summary>
/// <param name="Auto">The session was started by typing, not by the user.</param>
/// <param name="Shy">The list is not shown right away.</param>
/// <param name="Refilter">The options are those of a session being refiltered.</param>
/// <param name="Retrigger">The session replaces a running one.</param>
/// <param name="TriggerKind">The kind sent to the providers; <see cref="CompletionTriggerKind.Invoke"/> when null.</param>
/// <param name="TriggerCharacter">The trigger character that started the session, if any.</param>
/// <param name="ProviderFilter">When not empty, only these providers are asked.</param>
/// <param name="ProviderItemsToReuse">Providers whose previous items are reused instead of asking them again.</param>
/// <remarks>Threading: immutable record; the collections must not be changed while a request runs.</remarks>
internal sealed record SuggestTriggerOptions(
    bool Auto = false,
    bool Shy = false,
    bool Refilter = false,
    bool Retrigger = false,
    CompletionTriggerKind? TriggerKind = null,
    string? TriggerCharacter = null,
    IReadOnlySet<ICompletionItemProvider>? ProviderFilter = null,
    IReadOnlyDictionary<ICompletionItemProvider, IReadOnlyList<SuggestItem>>? ProviderItemsToReuse = null);
