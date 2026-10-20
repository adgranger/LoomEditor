using LoomEditor.Core.Tokens;
using LoomEditor.TextMate.Tests.TestHelpers;
using LoomEditor.TextMate.Tokenization;
using LoomEditor.TextMate.Tokenization.Worker;

namespace LoomEditor.TextMate.Tests.Tokenization.Worker.TestHelpers;

/// <summary>A batch received by <see cref="RecordingHost"/>.</summary>
internal sealed record Batch(int ControllerId, int VersionId, IReadOnlyList<ContiguousMultilineTokens> Tokens, IReadOnlyList<StateDeltas> StateDeltas);

/// <summary>Records what the worker sends; thread-safe, because the real worker calls it from its own thread.</summary>
internal sealed class RecordingHost : ITextMateWorkerHost
{
    private readonly List<Batch> _batches = [];
    private readonly Lock _lock = new();

    public ManualResetEventSlim Received { get; } = new();

    public IReadOnlyList<Batch> Batches
    {
        get
        {
            lock (_lock)
            {
                return [.. _batches];
            }
        }
    }

    public void SetTokensAndStates(int controllerId, int versionId, IReadOnlyList<ContiguousMultilineTokens> tokens, IReadOnlyList<StateDeltas> stateDeltas)
    {
        lock (_lock)
        {
            _batches.Add(new Batch(controllerId, versionId, tokens, stateDeltas));
        }

        Received.Set();
    }
}

/// <summary>A one-language setup: language <c>a</c>, scope <c>source.a</c>, the word <c>foo</c> is a keyword.</summary>
internal static class WorkerTestSupport
{
    public const string LanguageId = "a";

    private const string Grammar = """{ "scopeName": "source.a", "patterns": [ { "match": "foo", "name": "kw.a" } ] }""";

    public static LanguageRegistry CreateLanguages()
    {
        var languages = new LanguageRegistry();
        languages.Register(LanguageDefinitionReader.Read("""{ "id": "a", "scopeName": "source.a", "path": "a.json" }""", "a/"));
        return languages;
    }

    public static TMGrammarFactory CreateFactory(LanguageRegistry languages) => new(
        languages.CreateGrammarDefinitions(),
        location => location == "a/a.json" ? Grammar : null,
        new RawGrammarCache(),
        OnigTestLib.GetOniguruma());

    public static List<string> Lines(int count, string text = "foo bar") => [.. Enumerable.Repeat(text, count)];
}
