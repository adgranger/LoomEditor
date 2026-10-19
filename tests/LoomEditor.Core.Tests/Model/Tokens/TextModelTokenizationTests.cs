using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Tests.Model.Tokens.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.Tokens;

public class TextModelTokenizationTests
{
    private const int SupportForeground = 7;

    /// <summary>One token per line, with a foreground of <see cref="SupportForeground"/> for a one-character line, one more per extra character.</summary>
    private sealed class FooSupport : ITokenizationSupport
    {
        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) =>
            new([0, (uint)(SupportForeground + line.Length - 1) << MetadataConsts.ForegroundOffset], NullState.Instance);
    }

    private static (TextModel Model, TokenizationRegistry Registry, ManualTokenizationScheduler Scheduler) Create(string text)
    {
        var registry = new TokenizationRegistry();
        registry.Register("foo", new FooSupport());
        var scheduler = new ManualTokenizationScheduler();
        var model = new TextModel(text, DefaultEndOfLine.LF, null, registry, new LanguageIdCodec(), scheduler);
        return (model, registry, scheduler);
    }

    [Fact]
    public void SetLanguageId_UnknownLanguage_GivesNullTokens()
    {
        (TextModel model, _, _) = Create("hello");
        var changes = new List<ModelLanguageChangedEventArgs>();
        model.LanguageChanged += (_, e) => changes.Add(e);

        model.Tokenization.SetLanguageId("bar");

        LineTokens tokens = model.Tokenization.GetLineTokens(1);
        Assert.Equal(1, tokens.GetCount());
        Assert.Equal(ColorId.DefaultForeground, tokens.GetForeground(0));
        Assert.Equal(LanguageIdCodec.NullLanguageId, tokens.GetLanguageId(0));
        ModelLanguageChangedEventArgs change = Assert.Single(changes);
        Assert.Equal(("plaintext", "bar", "api"), (change.OldLanguage, change.NewLanguage, change.Source));
    }

    [Fact]
    public void GetLineTokens_AfterForceTokenization_UsesTheSupport()
    {
        (TextModel model, _, _) = Create("a\nb\nc");
        model.Tokenization.SetLanguageId("foo");
        Assert.Equal(ColorId.DefaultForeground, model.Tokenization.GetLineTokens(1).GetForeground(0));

        model.Tokenization.ForceTokenization(2);

        Assert.Equal(SupportForeground, model.Tokenization.GetLineTokens(1).GetForeground(0));
        Assert.Equal(SupportForeground, model.Tokenization.GetLineTokens(2).GetForeground(0));
        Assert.Equal(ColorId.DefaultForeground, model.Tokenization.GetLineTokens(3).GetForeground(0));
        Assert.True(model.Tokenization.HasAccurateTokensForLine(2));
        Assert.False(model.Tokenization.HasAccurateTokensForLine(3));
    }

    [Fact]
    public void Edit_InvalidatesFollowingStates_AndRaisesTokensChanged()
    {
        (TextModel model, _, _) = Create("a\nb\nc");
        model.Tokenization.SetLanguageId("foo");
        model.Tokenization.ForceTokenization(3);
        Assert.True(model.Tokenization.HasAccurateTokensForLine(3));
        var events = new List<ModelTokensChangedEventArgs>();
        model.TokensChanged += (_, e) => events.Add(e);

        model.ApplyEdits([new TextEdit(new Range(1, 2, 1, 2), "xyz")]);

        Assert.False(model.Tokenization.HasAccurateTokensForLine(1));
        model.Tokenization.ForceTokenization(3);
        Assert.True(model.Tokenization.HasAccurateTokensForLine(3));
        // line 1 is longer: its token now ends at 4 and its foreground changed
        Assert.Equal(4, model.Tokenization.GetLineTokens(1).GetEndOffset(0));
        Assert.Equal(SupportForeground + 3, model.Tokenization.GetLineTokens(1).GetForeground(0));
        Assert.Contains(events, e => e.Ranges.Any(r => r.FromLineNumber <= 1 && 1 <= r.ToLineNumber));
    }

    [Fact]
    public void RegistryChanged_ForTheModelLanguage_ResetsTokenization()
    {
        (TextModel model, TokenizationRegistry registry, _) = Create("a\nb");
        model.Tokenization.SetLanguageId("foo");
        model.Tokenization.ForceTokenization(2);
        Assert.True(model.Tokenization.HasTokens);
        var events = new List<ModelTokensChangedEventArgs>();
        model.TokensChanged += (_, e) => events.Add(e);

        registry.HandleChange(["other"]);
        Assert.Empty(events);
        Assert.True(model.Tokenization.HasTokens);

        registry.HandleChange(["foo"]);

        ModelTokensChangedEventArgs e = Assert.Single(events);
        Assert.Equal(new ChangedLineRange(1, 2), Assert.Single(e.Ranges));
        Assert.False(model.Tokenization.HasTokens);
    }

    [Fact]
    public void BackgroundTokenizationFinished_SetsCompleted()
    {
        (TextModel model, _, ManualTokenizationScheduler scheduler) = Create("a\nb\nc");
        model.Tokenization.SetLanguageId("foo");
        Assert.Equal(BackgroundTokenizationState.InProgress, model.Tokenization.BackgroundTokenizationState);

        using IAttachedView view = model.AttachView(); // the default background tokenizer only runs for an attached model
        int slices = 0;
        while (scheduler.PendingIdleCount > 0 && slices++ < 1000)
        {
            scheduler.RunIdle(0);
        }

        Assert.Equal(BackgroundTokenizationState.Completed, model.Tokenization.BackgroundTokenizationState);
        Assert.Equal(SupportForeground, model.Tokenization.GetLineTokens(3).GetForeground(0));
    }
}
