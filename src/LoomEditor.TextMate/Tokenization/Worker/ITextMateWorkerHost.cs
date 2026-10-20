// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/backgroundTokenization/worker/textMateWorkerHost.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.TextMate.Tokenization.Worker;

/// <summary>
/// Receives the results of the tokenization worker (Monaco: <c>TextMateWorkerHost</c>, <c>$setTokensAndStates</c>).
/// Not ported: <c>$readFile</c> (the worker reads the embedded grammars itself) and <c>$reportTokenizationTime</c>.
/// </summary>
/// <remarks>Threading: called on the worker thread; an implementation must hand the data over to the thread that owns the model.</remarks>
internal interface ITextMateWorkerHost
{
    /// <summary><c>$setTokensAndStates</c>: a batch of tokens and end states, computed for version <paramref name="versionId"/> of the model of <paramref name="controllerId"/>.</summary>
    /// <remarks>The worker never touches the arguments again.</remarks>
    void SetTokensAndStates(int controllerId, int versionId, IReadOnlyList<ContiguousMultilineTokens> tokens, IReadOnlyList<StateDeltas> stateDeltas);
}
