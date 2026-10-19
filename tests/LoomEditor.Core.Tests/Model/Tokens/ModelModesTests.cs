// Ported from microsoft/vscode: src/vs/editor/test/common/model/model.modes.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Tests.Model.Tokens.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.Tokens;

public class ModelModesTests
{
    private sealed class ModelState2(string prevLineContent) : IState
    {
        public string PrevLineContent { get; set; } = prevLineContent;

        public IState Clone() => new ModelState2(PrevLineContent);

        public bool Equals(IState? other) => other is ModelState2 s && s.PrevLineContent == PrevLineContent;
    }

    /// <summary>Records what it is called for (the first character of each line, or the whole line).</summary>
    private sealed class RecordingSupport(Func<IState> initialState, Func<string, string> record, bool mutateState) : ITokenizationSupport
    {
        public List<string> CalledFor { get; private set; } = [];

        public IState GetInitialState() => initialState();

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
        {
            CalledFor.Add(record(line));
            if (mutateState)
            {
                ((ModelState2)state).PrevLineContent = line;
            }

            return new EncodedTokenizationResult([], state);
        }

        public List<string> GetAndClear()
        {
            List<string> result = CalledFor;
            CalledFor = [];
            return result;
        }
    }

    private static TextModel CreateModel(string text, string languageId, ITokenizationSupport support)
    {
        var registry = new TokenizationRegistry();
        registry.Register(languageId, support);
        var model = new TextModel(text, DefaultEndOfLine.LF, null, registry, new LanguageIdCodec(), null);
        model.Tokenization.SetLanguageId(languageId);
        return model;
    }

    private static void Insert(TextModel model, int line, int column, string text) =>
        model.ApplyEdits([new TextEdit(Range.Collapsed(new Position(line, column)), text)]);

    private static void Delete(TextModel model, Range range) => model.ApplyEdits([new TextEdit(range, string.Empty)]);

    // Model Modes 1

    private static (TextModel Model, RecordingSupport Support) CreateModes1()
    {
        var support = new RecordingSupport(() => NullState.Instance, line => line[..1], mutateState: false);
        return (CreateModel("1\r\n2\n3\n4\r\n5", "modelModeTest1", support), support);
    }

    // Monaco: 'Editor Model - Model Modes 1' > 'model calls syntax highlighter 1'
    [Fact]
    public void ModelCallsSyntaxHighlighter_1()
    {
        (TextModel model, RecordingSupport support) = CreateModes1();

        model.Tokenization.ForceTokenization(1);
        Assert.Equal(["1"], support.GetAndClear());
    }

    // Monaco: 'Editor Model - Model Modes 1' > 'model calls syntax highlighter 2'
    [Fact]
    public void ModelCallsSyntaxHighlighter_2()
    {
        (TextModel model, RecordingSupport support) = CreateModes1();

        model.Tokenization.ForceTokenization(2);
        Assert.Equal(["1", "2"], support.GetAndClear());

        model.Tokenization.ForceTokenization(2);
        Assert.Empty(support.GetAndClear());
    }

    // Monaco: 'Editor Model - Model Modes 1' > 'model caches states'
    [Fact]
    public void ModelCachesStates()
    {
        (TextModel model, RecordingSupport support) = CreateModes1();

        for (int line = 1; line <= 5; line++)
        {
            model.Tokenization.ForceTokenization(line);
            Assert.Equal([line.ToString(System.Globalization.CultureInfo.InvariantCulture)], support.GetAndClear());
        }

        model.Tokenization.ForceTokenization(5);
        Assert.Empty(support.GetAndClear());
    }

    // Monaco: 'Editor Model - Model Modes 1' > 'model invalidates states for one line insert'
    [Fact]
    public void ModelInvalidatesStatesForOneLineInsert()
    {
        (TextModel model, RecordingSupport support) = CreateModes1();
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["1", "2", "3", "4", "5"], support.GetAndClear());

        Insert(model, 1, 1, "-");
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["-"], support.GetAndClear());

        model.Tokenization.ForceTokenization(5);
        Assert.Empty(support.GetAndClear());
    }

    // Monaco: 'Editor Model - Model Modes 1' > 'model invalidates states for many lines insert'
    [Fact]
    public void ModelInvalidatesStatesForManyLinesInsert()
    {
        (TextModel model, RecordingSupport support) = CreateModes1();
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["1", "2", "3", "4", "5"], support.GetAndClear());

        Insert(model, 1, 1, "0\n-\n+");
        Assert.Equal(7, model.LineCount);
        model.Tokenization.ForceTokenization(7);
        Assert.Equal(["0", "-", "+"], support.GetAndClear());

        model.Tokenization.ForceTokenization(7);
        Assert.Empty(support.GetAndClear());
    }

    // Monaco: 'Editor Model - Model Modes 1' > 'model invalidates states for one new line'
    [Fact]
    public void ModelInvalidatesStatesForOneNewLine()
    {
        (TextModel model, RecordingSupport support) = CreateModes1();
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["1", "2", "3", "4", "5"], support.GetAndClear());

        Insert(model, 1, 2, "\n");
        Insert(model, 2, 1, "a");
        model.Tokenization.ForceTokenization(6);
        Assert.Equal(["1", "a"], support.GetAndClear());
    }

    // Monaco: 'Editor Model - Model Modes 1' > 'model invalidates states for one line delete'
    [Fact]
    public void ModelInvalidatesStatesForOneLineDelete()
    {
        (TextModel model, RecordingSupport support) = CreateModes1();
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["1", "2", "3", "4", "5"], support.GetAndClear());

        Insert(model, 1, 2, "-");
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["1"], support.GetAndClear());

        Delete(model, new Range(1, 1, 1, 2));
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["-"], support.GetAndClear());

        model.Tokenization.ForceTokenization(5);
        Assert.Empty(support.GetAndClear());
    }

    // Monaco: 'Editor Model - Model Modes 1' > 'model invalidates states for many lines delete'
    [Fact]
    public void ModelInvalidatesStatesForManyLinesDelete()
    {
        (TextModel model, RecordingSupport support) = CreateModes1();
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["1", "2", "3", "4", "5"], support.GetAndClear());

        Delete(model, new Range(1, 1, 3, 1));
        model.Tokenization.ForceTokenization(3);
        Assert.Equal(["3"], support.GetAndClear());

        model.Tokenization.ForceTokenization(3);
        Assert.Empty(support.GetAndClear());
    }

    // Model Modes 2

    private static (TextModel Model, RecordingSupport Support) CreateModes2()
    {
        var support = new RecordingSupport(() => new ModelState2(string.Empty), line => line, mutateState: true);
        return (CreateModel("Line1\r\nLine2\nLine3\nLine4\r\nLine5", "modelModeTest2", support), support);
    }

    // Monaco: 'Editor Model - Model Modes 2' > 'getTokensForInvalidLines one text insert'
    [Fact]
    public void GetTokensForInvalidLines_OneTextInsert()
    {
        (TextModel model, RecordingSupport support) = CreateModes2();
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["Line1", "Line2", "Line3", "Line4", "Line5"], support.GetAndClear());
        Insert(model, 1, 6, "-");
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["Line1-", "Line2"], support.GetAndClear());
    }

    // Monaco: 'Editor Model - Model Modes 2' > 'getTokensForInvalidLines two text insert'
    [Fact]
    public void GetTokensForInvalidLines_TwoTextInsert()
    {
        (TextModel model, RecordingSupport support) = CreateModes2();
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["Line1", "Line2", "Line3", "Line4", "Line5"], support.GetAndClear());
        model.ApplyEdits(
        [
            new TextEdit(Range.Collapsed(new Position(1, 6)), "-"),
            new TextEdit(Range.Collapsed(new Position(3, 6)), "-"),
        ]);

        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["Line1-", "Line2", "Line3-", "Line4"], support.GetAndClear());
    }

    // Monaco: 'Editor Model - Model Modes 2' > 'getTokensForInvalidLines one multi-line text insert, one small text insert'
    [Fact]
    public void GetTokensForInvalidLines_OneMultiLineTextInsert_OneSmallTextInsert()
    {
        (TextModel model, RecordingSupport support) = CreateModes2();
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["Line1", "Line2", "Line3", "Line4", "Line5"], support.GetAndClear());
        Insert(model, 1, 6, "\nNew line\nAnother new line");
        Insert(model, 5, 6, "-");
        model.Tokenization.ForceTokenization(7);
        Assert.Equal(["Line1", "New line", "Another new line", "Line2", "Line3-", "Line4"], support.GetAndClear());
    }

    // Monaco: 'Editor Model - Model Modes 2' > 'getTokensForInvalidLines one delete text'
    [Fact]
    public void GetTokensForInvalidLines_OneDeleteText()
    {
        (TextModel model, RecordingSupport support) = CreateModes2();
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["Line1", "Line2", "Line3", "Line4", "Line5"], support.GetAndClear());
        Delete(model, new Range(1, 1, 1, 5));
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["1", "Line2"], support.GetAndClear());
    }

    // Monaco: 'Editor Model - Model Modes 2' > 'getTokensForInvalidLines one line delete text'
    [Fact]
    public void GetTokensForInvalidLines_OneLineDeleteText()
    {
        (TextModel model, RecordingSupport support) = CreateModes2();
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["Line1", "Line2", "Line3", "Line4", "Line5"], support.GetAndClear());
        Delete(model, new Range(1, 1, 2, 1));
        model.Tokenization.ForceTokenization(4);
        Assert.Equal(["Line2"], support.GetAndClear());
    }

    // Monaco: 'Editor Model - Model Modes 2' > 'getTokensForInvalidLines multiple lines delete text'
    [Fact]
    public void GetTokensForInvalidLines_MultipleLinesDeleteText()
    {
        (TextModel model, RecordingSupport support) = CreateModes2();
        model.Tokenization.ForceTokenization(5);
        Assert.Equal(["Line1", "Line2", "Line3", "Line4", "Line5"], support.GetAndClear());
        Delete(model, new Range(1, 1, 3, 3));
        model.Tokenization.ForceTokenization(3);
        Assert.Equal(["ne3", "Line4"], support.GetAndClear());
    }

    // New tests

    private const int SupportForeground = 7;

    private static (TextModel Model, ManualTokenizationScheduler Scheduler, RecordingSupport Support) CreateWithScheduler(string text)
    {
        var support = new RecordingSupport(() => NullState.Instance, line => line, mutateState: false);
        var registry = new TokenizationRegistry();
        registry.Register("foo", new ForegroundSupport(support));
        var scheduler = new ManualTokenizationScheduler();
        var model = new TextModel(text, DefaultEndOfLine.LF, null, registry, new LanguageIdCodec(), scheduler);
        model.Tokenization.SetLanguageId("foo");
        return (model, scheduler, support);
    }

    /// <summary>Gives every line one token with the foreground <see cref="SupportForeground"/>, and records the calls in <paramref name="inner"/>.</summary>
    private sealed class ForegroundSupport(RecordingSupport inner) : ITokenizationSupport
    {
        public IState GetInitialState() => inner.GetInitialState();

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
        {
            inner.TokenizeEncoded(line, hasEol, state);
            return new EncodedTokenizationResult([0, (uint)SupportForeground << MetadataConsts.ForegroundOffset], state);
        }
    }

    [Fact]
    public void SetVisibleLines_TokenizesVisibleLinesFirst()
    {
        (TextModel model, ManualTokenizationScheduler scheduler, _) =
            CreateWithScheduler(string.Join('\n', Enumerable.Range(1, 100).Select(i => $"line {i}")));
        using IAttachedView view = model.AttachView();
        var events = new List<ModelTokensChangedEventArgs>();
        model.TokensChanged += (_, e) => events.Add(e);

        // scrolling: nothing happens before the scheduler posts the refresh
        view.SetVisibleLines([(50, 52)], stabilized: false);
        Assert.Equal(ColorId.DefaultForeground, model.Tokenization.GetLineTokens(51).GetForeground(0));
        scheduler.RunPosted();

        Assert.Equal(SupportForeground, model.Tokenization.GetLineTokens(51).GetForeground(0));
        Assert.Equal(ColorId.DefaultForeground, model.Tokenization.GetLineTokens(10).GetForeground(0));
        Assert.False(model.Tokenization.HasAccurateTokensForLine(51)); // heuristic: the background has not reached it
        Assert.Contains(events, e => e.Ranges.Contains(new ChangedLineRange(50, 52)));

        // a stabilized view (reveal): refreshed at once
        view.SetVisibleLines([(80, 81)], stabilized: true);
        Assert.Equal(SupportForeground, model.Tokenization.GetLineTokens(80).GetForeground(0));
    }

    [Fact]
    public void TooLargeModel_IsNotTokenized()
    {
        var support = new RecordingSupport(() => NullState.Instance, line => line, mutateState: false);
        var registry = new TokenizationRegistry();
        registry.Register("foo", new ForegroundSupport(support));
        var model = new TextModel(new string('\n', 300_000), DefaultEndOfLine.LF, null, registry, new LanguageIdCodec(), null);
        model.Tokenization.SetLanguageId("foo");

        model.Tokenization.ForceTokenization(10);

        Assert.True(model.IsTooLargeForTokenization);
        Assert.Empty(support.CalledFor);
        Assert.Equal(ColorId.DefaultForeground, model.Tokenization.GetLineTokens(5).GetForeground(0));
    }

    [Fact]
    public void RateLimiter_RunsTheFirstCallOnly_UntilTheDelayPassed()
    {
        var limiter = new RateLimiter(timesPerSecond: 1);
        int runs = 0;

        limiter.RunIfNotLimited(() => runs++);
        limiter.RunIfNotLimited(() => runs++);

        Assert.Equal(1, runs);
    }
}
