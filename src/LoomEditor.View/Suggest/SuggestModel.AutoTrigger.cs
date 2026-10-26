// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;
using LoomEditor.View.Features;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Suggest;

// The automatic triggers: quick suggest while typing a word, and the trigger characters of the providers.
internal sealed partial class SuggestModel
{
    private EventHandler<string>? _didTypeHandler;

    // Monaco: _updateTriggerCharacters
    private void UpdateTriggerCharacters()
    {
        if (_didTypeHandler is not null && _viewModel is not null)
        {
            _viewModel.DidType -= _didTypeHandler;
        }

        _didTypeHandler = null;
        if (_viewModel is null
            || _providers is null
            || !_modelOptions.SuggestOnTriggerCharacters
            || _viewModel.IsReadOnly
            || !_providers.Has(_context.GetLanguageId()))
        {
            return;
        }

        var supportsByTriggerCharacter = new Dictionary<string, HashSet<ICompletionItemProvider>>();
        foreach (ICompletionItemProvider provider in _providers.All(_context.GetLanguageId()))
        {
            foreach (string ch in provider.TriggerCharacters ?? [])
            {
                if (!supportsByTriggerCharacter.TryGetValue(ch, out HashSet<ICompletionItemProvider>? supports))
                {
                    supportsByTriggerCharacter[ch] = supports = [];
                }

                supports.Add(provider);
            }
        }

        _didTypeHandler = (_, text) => CheckTriggerCharacter(text, supportsByTriggerCharacter);
        _viewModel.DidType += _didTypeHandler;
    }

    // Monaco: checkTriggerCharacter
    private void CheckTriggerCharacter(string text, Dictionary<string, HashSet<ICompletionItemProvider>> supportsByTriggerCharacter)
    {
        if (text.Length == 0 || LineContext.ShouldAutoTrigger(_context.Model, _context.Selection.Position))
        {
            // don't trigger by trigger characters when this is a case for quick suggest
            return;
        }

        int count = text.Length >= 2 && char.IsLowSurrogate(text[^1]) && char.IsHighSurrogate(text[^2]) ? 2 : 1;
        string lastChar = text[^count..];
        if (!supportsByTriggerCharacter.TryGetValue(lastChar, out HashSet<ICompletionItemProvider>? supports))
        {
            return;
        }

        // keep existing items that where not computed by the supports/providers that want to trigger now
        var providerItemsToReuse = new Dictionary<ICompletionItemProvider, IReadOnlyList<SuggestItem>>();
        if (_completionModel is not null)
        {
            foreach ((ICompletionItemProvider provider, IReadOnlyList<SuggestItem> items) in _completionModel.GetItemsByProvider())
            {
                if (!supports.Contains(provider))
                {
                    providerItemsToReuse[provider] = items;
                }
            }
        }

        Trigger(new SuggestTriggerOptions(
            Auto: true,
            TriggerKind: CompletionTriggerKind.TriggerCharacter,
            TriggerCharacter: lastChar,
            Retrigger: _completionModel is not null,
            ProviderFilter: supports,
            ProviderItemsToReuse: providerItemsToReuse));
    }

    // Monaco: _doTriggerQuickSuggest (without inline completions, canShowQuickSuggest and tokenizeIfCheap)
    private void TriggerQuickSuggestIfTyping()
    {
        Cancel();
        _triggerQuickSuggest.CancelAndSet(
            () =>
            {
                if (_triggerState is not null
                    || _viewModel is null
                    || _providers is null
                    || !LineContext.ShouldAutoTrigger(_context.Model, _context.Selection.Position)
                    || !_viewModel.HasFocus)
                {
                    return;
                }

                QuickSuggestionsOptions quickSuggestions = _modelOptions.QuickSuggestions;
                if (quickSuggestions.IsAllOff)
                {
                    return;
                }

                Position position = _context.Selection.Position;
                if (!quickSuggestions.IsAllOn)
                {
                                        StandardTokenType tokenType = _tokenTypeAt(_context.Model, position);
                    if (quickSuggestions.ValueFor(tokenType) is QuickSuggestionValue.Off or QuickSuggestionValue.Inline)
                    {
                        return;
                    }
                }

                if (!_providers.Has(_context.GetLanguageId()))
                {
                    return;
                }

                Trigger(new SuggestTriggerOptions(Auto: true));
            },
            _modelOptions.QuickSuggestionsDelay);
    }

    // Monaco: lineTokens.getStandardTokenType(lineTokens.findTokenIndexAtOffset(Math.max(position.column - 1 - 1, 0)))
    private static StandardTokenType ReadTokenTypeFromModel(Core.Model.TextModel model, Position position)
    {
        LineTokens lineTokens = model.Tokenization.GetLineTokens(position.LineNumber);
        return lineTokens.GetStandardTokenType(lineTokens.FindTokenIndexAtOffset(Math.Max(position.Column - 1 - 1, 0)));
    }
}
