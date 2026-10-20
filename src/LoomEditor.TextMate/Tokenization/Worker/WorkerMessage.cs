// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/backgroundTokenization/worker/textMateTokenizationWorker.worker.ts (the $accept* / $retokenize methods and IRawModelData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.TextMate.Theme;

namespace LoomEditor.TextMate.Tokenization.Worker;

/// <summary>
/// A message for the <see cref="TokenizationWorker"/>: each record is one RPC method of Monaco's worker
/// (<c>$acceptNewModel</c>, <c>$acceptModelChanged</c>, …). Messages are handled in the order they were posted.
/// </summary>
/// <remarks>Threading: immutable; the lists must not be changed after posting.</remarks>
internal abstract record WorkerMessage
{
    private protected WorkerMessage()
    {
    }
}

/// <summary><c>$acceptNewModel</c> (<c>IRawModelData</c>, without the URI).</summary>
/// <param name="ControllerId">The id of the model's controller on the UI side.</param>
/// <param name="Lines">A copy of the lines of the model.</param>
/// <param name="Eol">The end-of-line sequence.</param>
/// <param name="VersionId">The version of the model the lines belong to.</param>
/// <param name="LanguageId">The language id.</param>
/// <param name="EncodedLanguageId">The language id as stored in token metadata.</param>
/// <param name="MaxTokenizationLineLength">Lines of this length or more are not tokenized.</param>
internal sealed record NewModelMessage(
    int ControllerId,
    IReadOnlyList<string> Lines,
    string Eol,
    int VersionId,
    string LanguageId,
    int EncodedLanguageId,
    int MaxTokenizationLineLength) : WorkerMessage;

/// <summary><c>$acceptModelChanged</c>.</summary>
internal sealed record ModelChangedMessage(int ControllerId, MirrorModelChangedEvent Event) : WorkerMessage;

/// <summary><c>$retokenize</c>.</summary>
internal sealed record RetokenizeMessage(int ControllerId, int StartLineNumber, int EndLineNumberExclusive) : WorkerMessage;

/// <summary><c>$acceptModelLanguageChanged</c>.</summary>
internal sealed record LanguageChangedMessage(int ControllerId, string LanguageId, int EncodedLanguageId) : WorkerMessage;

/// <summary><c>$acceptRemovedModel</c>.</summary>
internal sealed record RemovedModelMessage(int ControllerId) : WorkerMessage;

/// <summary><c>$acceptTheme</c>.</summary>
internal sealed record ThemeMessage(RawTheme Theme, IReadOnlyList<string> ColorMap) : WorkerMessage;

/// <summary><c>$acceptMaxTokenizationLineLength</c>.</summary>
internal sealed record MaxTokenizationLineLengthMessage(int ControllerId, int Value) : WorkerMessage;

/// <summary>Stops the worker (it disposes its models and grammars on its own thread).</summary>
internal sealed record DisposeMessage : WorkerMessage;
