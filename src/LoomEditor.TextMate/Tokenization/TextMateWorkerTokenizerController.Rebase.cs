// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/backgroundTokenization/textMateWorkerTokenizerController.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Tokens;

namespace LoomEditor.TextMate.Tokenization;

// The part of setTokensAndStates that moves a batch to the current version of the model.
internal sealed partial class TextMateWorkerTokenizerController
{
    // A fresh transformer for the edits made since the batch (transformers only move forward).
    private IIndexTransformer CreateCurToFutureTransformer() =>
        MonotonousIndexTransformer.FromMany([.. _pendingChanges.Select(c => ModelContentChangeEdits.LinesLengthEditFromModelContentChange(c.Changes))]);

    private List<ContiguousMultilineTokens> RebaseTokens(IReadOnlyList<ContiguousMultilineTokens> tokens)
    {
        IIndexTransformer curToFutureTransformerTokens = CreateCurToFutureTransformer();

        // Filter tokens in lines that got changed in the future to prevent flickering
        // These tokens are recomputed anyway.
        var builder = new ContiguousMultilineTokensBuilder();
        foreach (ContiguousMultilineTokens t in tokens)
        {
            for (int i = t.StartLineNumber; i <= t.EndLineNumber; i++)
            {
                // If the result is null, the current line got touched by an edit.
                // The worker will send us new tokens for all the new/touched lines after it received the edits.
                if (curToFutureTransformerTokens.Transform(i - 1) is not null)
                {
                    builder.Add(i, t.GetLineTokens(i)!);
                }
            }
        }

        List<ContiguousMultilineTokens> rebased = builder.Finalize();

        // Apply future changes to tokens
        foreach (PendingChange change in _pendingChanges)
        {
            foreach (ModelContentChange innerChange in change.Changes)
            {
                foreach (ContiguousMultilineTokens t in rebased)
                {
                    t.ApplyEdit(innerChange.Range, innerChange.Text);
                }
            }
        }

        return rebased;
    }
}
