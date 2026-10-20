// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/backgroundTokenization/worker/textMateTokenizationWorker.worker.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using System.Threading.Channels;

namespace LoomEditor.TextMate.Tokenization.Worker;

/// <summary>
/// The tokenization worker (Monaco: <c>TextMateTokenizationWorker</c>, a web worker): one background thread with its
/// own <see cref="TMGrammarFactory"/> (native scanners belong to the thread that created them, ADR 0008, decision 5)
/// that handles <see cref="WorkerMessage"/>s in order and tokenizes the documents it mirrors. Replaces the web worker
/// RPC by a <see cref="Channel{T}"/>, <c>RunOnceScheduler(10)</c> by a 10 ms wait for more messages and
/// <c>setTimeout0</c> by reading the pending messages between two slices. Not ported: <c>$init</c> (WASM loading).
/// </summary>
/// <remarks>
/// Threading: <see cref="Post"/> and <see cref="Dispose"/> are thread-safe; everything else runs on the worker thread.
/// The <see cref="ITextMateWorkerHost"/> is called on the worker thread. An exception while handling a message is
/// written with <see cref="Debug"/> and the loop goes on.
/// </remarks>
internal sealed class TokenizationWorker : ITokenizationWorker
{
    /// <summary>Lines per batch (Monaco: 200).</summary>
    public const int MaxLinesPerSlice = 200;

    /// <summary>Milliseconds per batch (Monaco: 20).</summary>
    public const int MaxMillisecondsPerSlice = 20;

    /// <summary>Milliseconds to wait for more messages before tokenizing (Monaco: the <c>RunOnceScheduler</c> delay).</summary>
    public const int DebounceMilliseconds = 10;

    private const int JoinTimeoutMilliseconds = 1000;

    private readonly Channel<WorkerMessage> _channel = Channel.CreateUnbounded<WorkerMessage>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    private readonly Func<TMGrammarFactory> _createGrammarFactory;
    private readonly ITextMateWorkerHost _host;
    private readonly Thread _thread;
    private readonly Dictionary<int, TextMateWorkerTokenizer> _models = [];
    private readonly Dictionary<int, CreateGrammarResult> _grammarCache = [];
    private TMGrammarFactory? _grammarFactory;
    private int _disposed;

    /// <summary>Starts the thread. <paramref name="createGrammarFactory"/> runs on it, so the factory and its scanners belong to it.</summary>
    public TokenizationWorker(Func<TMGrammarFactory> createGrammarFactory, ITextMateWorkerHost host)
    {
        ArgumentNullException.ThrowIfNull(createGrammarFactory);
        ArgumentNullException.ThrowIfNull(host);
        _createGrammarFactory = createGrammarFactory;
        _host = host;
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal,
            Name = "LoomEditor tokenizer",
        };
        _thread.Start();
    }

    /// <summary>Queues a message; it is ignored once the worker is disposed.</summary>
    public void Post(WorkerMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _channel.Writer.TryWrite(message);
    }

    /// <summary>Stops the worker and waits for its thread for one second.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _channel.Writer.TryWrite(new DisposeMessage());
        if (!_thread.Join(JoinTimeoutMilliseconds))
        {
            Debug.WriteLine("The tokenization worker did not stop in time.");
        }
    }

    private void Run()
    {
        try
        {
            try
            {
                _grammarFactory = _createGrammarFactory();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Unable to create the grammar factory of the tokenization worker: {e}");
            }

            ChannelReader<WorkerMessage> reader = _channel.Reader;
            bool moreSlices = false;
            while (true)
            {
                if (!moreSlices)
                {
                    // Nothing to tokenize: sleep until a message comes, then give it 10 ms to be followed by others.
                    if (Handle(ReadBlocking(reader, Timeout.Infinite)!))
                    {
                        return;
                    }

                    WorkerMessage? next;
                    while ((next = ReadBlocking(reader, DebounceMilliseconds)) is not null)
                    {
                        if (Handle(next))
                        {
                            return;
                        }
                    }
                }
                else
                {
                    // Yield between two slices to take the changes that arrived.
                    while (reader.TryRead(out WorkerMessage? pending))
                    {
                        if (Handle(pending))
                        {
                            return;
                        }
                    }
                }

                moreSlices = TokenizeOneSliceOfEachModel();
            }
        }
        finally
        {
            foreach (TextMateWorkerTokenizer model in _models.Values)
            {
                model.Dispose();
            }

            _models.Clear();
            _grammarFactory?.Dispose();
            _grammarFactory = null;
        }
    }

    // Reads the next message, waiting at most `timeoutMilliseconds`; null on timeout.
    private static WorkerMessage? ReadBlocking(ChannelReader<WorkerMessage> reader, int timeoutMilliseconds)
    {
        if (reader.TryRead(out WorkerMessage? message))
        {
            return message;
        }

        using var cts = new CancellationTokenSource(timeoutMilliseconds);
        try
        {
            return reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult() && reader.TryRead(out message) ? message : null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private bool TokenizeOneSliceOfEachModel()
    {
        bool moreSlices = false;
        foreach (TextMateWorkerTokenizer model in _models.Values.ToArray())
        {
            try
            {
                moreSlices |= model.TokenizeSlice(MaxLinesPerSlice, MaxMillisecondsPerSlice);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Tokenization failed: {e}");
            }
        }

        return moreSlices;
    }

    // Returns true when the worker must stop.
    private bool Handle(WorkerMessage message)
    {
        try
        {
            switch (message)
            {
                case DisposeMessage:
                    return true;
                case NewModelMessage m:
                    _models[m.ControllerId] = new TextMateWorkerTokenizer(
                        [.. m.Lines], m.Eol, m.VersionId, _host, m.ControllerId, GetOrCreateGrammar, m.LanguageId, m.EncodedLanguageId, m.MaxTokenizationLineLength);
                    break;
                case ModelChangedMessage m:
                    _models[m.ControllerId].AcceptEvents(m.Event);
                    break;
                case RetokenizeMessage m:
                    _models[m.ControllerId].Retokenize(m.StartLineNumber, m.EndLineNumberExclusive);
                    break;
                case LanguageChangedMessage m:
                    _models[m.ControllerId].OnLanguageId(m.LanguageId, m.EncodedLanguageId);
                    break;
                case RemovedModelMessage m:
                    if (_models.Remove(m.ControllerId, out TextMateWorkerTokenizer? removed))
                    {
                        removed.Dispose();
                    }

                    break;
                case ThemeMessage m:
                    _grammarFactory?.SetTheme(m.Theme, m.ColorMap);
                    break;
                case MaxTokenizationLineLengthMessage m:
                    _models[m.ControllerId].AcceptMaxTokenizationLineLength(m.Value);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"The tokenization worker failed to handle {message.GetType().Name}: {e}");
        }

        return false;
    }

    // getOrCreateGrammar: one grammar per language, shared by the models of that language.
    private CreateGrammarResult? GetOrCreateGrammar(string languageId, int encodedLanguageId)
    {
        if (_grammarFactory is null)
        {
            return null;
        }

        if (_grammarCache.TryGetValue(encodedLanguageId, out CreateGrammarResult? cached))
        {
            return cached;
        }

        try
        {
            CreateGrammarResult created = _grammarFactory.CreateGrammar(languageId, encodedLanguageId);
            _grammarCache[encodedLanguageId] = created;
            return created;
        }
        catch (InvalidOperationException e) when (e.Message == TMGrammarFactory.MissingTMGrammarErrorMessage)
        {
            return null; // a language without grammar is not an error
        }
    }
}
