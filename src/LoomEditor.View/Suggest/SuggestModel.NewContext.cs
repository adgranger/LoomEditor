// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestModel.ts (_onNewContext)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;

namespace LoomEditor.View.Suggest;

internal sealed partial class SuggestModel
{
    // The order of the early returns is the behavior. Trigger and Cancel raise events whose handlers can call back:
    // _lineContext is read again after every call that can cancel (Monaco notes the same two places).
    private void OnNewContext(LineContext ctx)
    {
        if (_lineContext is not { } context)
        {
            // happens when 24x7 IntelliSense is enabled and still in its delay
            return;
        }

        if (ctx.LineNumber != context.LineNumber)
        {
            // e.g. happens when pressing Enter while IntelliSense is computed
            Cancel();
            return;
        }

        if (GetLeadingWhitespace(ctx.LeadingLineContent) != GetLeadingWhitespace(context.LeadingLineContent))
        {
            // cancel IntelliSense when line start changes: happens when the current word gets outdented
            Cancel();
            return;
        }

        if (ctx.Column < context.Column)
        {
            // typed -> moved cursor LEFT -> retrigger if still on a word
            if (ctx.LeadingWord.Word.Length != 0)
            {
                Trigger(new SuggestTriggerOptions(Auto: context.TriggerOptions.Auto, Retrigger: true));
            }
            else
            {
                Cancel();
            }

            return;
        }

        if (_completionModel is not { } completionModel)
        {
            // happens when IntelliSense is not yet computed
            return;
        }

        if (ctx.LeadingWord.Word.Length != 0 && ctx.LeadingWord.StartColumn > context.LeadingWord.StartColumn)
        {
            // started a new word while IntelliSense shows -> retrigger but reuse all items that we currently have
            if (LineContext.ShouldAutoTrigger(_context.Model, _context.Selection.Position) && _lineContext is { } current)
            {
                Trigger(new SuggestTriggerOptions(
                    Auto: current.TriggerOptions.Auto,
                    Retrigger: true,
                    ProviderItemsToReuse: completionModel.GetItemsByProvider()));
            }

            return;
        }

        if (ctx.Column > context.Column && completionModel.GetIncompleteProvider().Count > 0 && ctx.LeadingWord.Word.Length != 0)
        {
            // typed -> moved cursor RIGHT & incomplete model & still on a word -> retrigger
            var providerItemsToReuse = new Dictionary<ICompletionItemProvider, IReadOnlyList<SuggestItem>>();
            var providerFilter = new HashSet<ICompletionItemProvider>();
            foreach ((ICompletionItemProvider provider, IReadOnlyList<SuggestItem> items) in completionModel.GetItemsByProvider())
            {
                if (items.Count > 0 && items[0].Container.Incomplete)
                {
                    providerFilter.Add(provider);
                }
                else
                {
                    providerItemsToReuse[provider] = items;
                }
            }

            Trigger(new SuggestTriggerOptions(
                Auto: context.TriggerOptions.Auto,
                Retrigger: true,
                TriggerKind: CompletionTriggerKind.TriggerForIncompleteCompletions,
                ProviderFilter: providerFilter,
                ProviderItemsToReuse: providerItemsToReuse));
            return;
        }

        // typed -> moved cursor RIGHT -> update UI
        CompletionLineContext oldLineContext = completionModel.LineContext;
        var isFrozen = false;

        completionModel.LineContext = new CompletionLineContext(ctx.LeadingLineContent, ctx.Column - context.Column);

        if (completionModel.Items.Count == 0)
        {
            bool shouldAutoTrigger = LineContext.ShouldAutoTrigger(_context.Model, _context.Selection.Position);
            if (_lineContext is not { } afterCheck)
            {
                Cancel();
                return;
            }

            if (shouldAutoTrigger && afterCheck.LeadingWord.EndColumn < ctx.LeadingWord.StartColumn)
            {
                // retrigger when heading into a new word
                Trigger(new SuggestTriggerOptions(Auto: afterCheck.TriggerOptions.Auto, Retrigger: true));
                return;
            }

            if (!afterCheck.TriggerOptions.Auto)
            {
                // freeze when IntelliSense was manually requested
                completionModel.LineContext = oldLineContext;
                isFrozen = completionModel.Items.Count > 0;

                if (isFrozen && ctx.LeadingWord.Word.Length == 0)
                {
                    // there were results before but now there aren't and also we are not on a word anymore -> cancel
                    Cancel();
                    return;
                }
            }
            else
            {
                // nothing left
                Cancel();
                return;
            }
        }

        DidSuggest?.Invoke(this, new SuggestEventArgs(completionModel, ctx.TriggerOptions, isFrozen));
    }

    // Monaco: strings.getLeadingWhitespace
    private static string GetLeadingWhitespace(string text)
    {
        var length = 0;
        while (length < text.Length && text[length] is ' ' or '\t')
        {
            length++;
        }

        return text[..length];
    }
}
