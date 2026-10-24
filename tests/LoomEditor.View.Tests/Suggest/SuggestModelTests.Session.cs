using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Suggest.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

public partial class SuggestModelTests
{
    private static FakeCompletionProvider Fixed(params string[] labels) => new()
    {
        OnProvide = (_, position, _) => List(false, [.. labels.Select(l => Item(l, CompletionRange.FromRange(FromColumnOne(position))))]),
    };

    [Fact]
    public void Trigger_RaisesTriggerThenSuggest()
    {
        using var editor = new SuggestTestEditor();
        editor.Providers.Register("plaintext", Fixed("a", "b", "c"));
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        Assert.Equal(["trigger auto=false", "suggest items=3 frozen=false"], recorder.Log);
    }

    [Fact]
    public void Trigger_NoProviders_SuggestWithNoItems_ThenCancelForAuto()
    {
        using var editor = new SuggestTestEditor("abc def");
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        model.Trigger(new SuggestTriggerOptions(Auto: true));
        editor.Flush();
        Assert.Equal(["trigger auto=true", "cancel retrigger=false"], recorder.Log);

        recorder.Log.Clear();
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();
        Assert.Equal(["trigger auto=false", "suggest items=0 frozen=false"], recorder.Log);
    }

    [Fact]
    public void Trigger_StaleResult_AfterCancel_Dropped()
    {
        using var editor = new SuggestTestEditor();
        FakeCompletionProvider provider = FakeCompletionProvider.Pending();
        editor.Providers.Register("plaintext", provider);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        model.Trigger(new SuggestTriggerOptions(Auto: false));
        model.Cancel();
        provider.Complete(List(false, Item("a", CompletionRange.FromRange(new Range(1, 1, 1, 1)))));
        editor.Flush();

        Assert.Equal(["trigger auto=false", "cancel retrigger=false"], recorder.Log);
    }

    [Fact]
    public void Trigger_SecondTriggerCancelsFirstRequest()
    {
        using var editor = new SuggestTestEditor();
        FakeCompletionProvider provider = FakeCompletionProvider.Pending();
        editor.Providers.Register("plaintext", provider);
        using SuggestModel model = editor.CreateModel();

        model.Trigger(new SuggestTriggerOptions(Auto: false));
        model.Trigger(new SuggestTriggerOptions(Auto: false));

        Assert.True(provider.Tokens[0].IsCancellationRequested);
        Assert.False(provider.Tokens[1].IsCancellationRequested);
    }

    [Fact]
    public void Trigger_ProviderThrows_Cancels()
    {
        using var editor = new SuggestTestEditor();
        FakeCompletionProvider failing = FakeCompletionProvider.Pending();
        editor.Providers.Register("plaintext", failing);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        // a provider that fails gives no items (Monaco ignores it): the auto session has nothing left
        model.Trigger(new SuggestTriggerOptions(Auto: true));
        failing.Fail();
        editor.Flush();
        Assert.Equal(["trigger auto=true", "cancel retrigger=false"], recorder.Log);

        // an error while the items are built is not left as a session without a request
        recorder.Log.Clear();
        using var editor2 = new SuggestTestEditor();
        editor2.Providers.Register("plaintext", new FakeCompletionProvider { OnProvide = (_, _, _) => List(false, [null!]) });
        using SuggestModel model2 = editor2.CreateModel();
        var recorder2 = new SuggestModelRecorder(model2);
        model2.Trigger(new SuggestTriggerOptions(Auto: false));
        editor2.Flush();
        Assert.Equal(["trigger auto=false", "cancel retrigger=false"], recorder2.Log);
        Assert.Equal(SuggestState.Idle, model2.State);
    }

    [Fact]
    public void State_FollowsTheSession()
    {
        using var editor = new SuggestTestEditor();
        using SuggestModel model = editor.CreateModel();

        Assert.Equal(SuggestState.Idle, model.State);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        Assert.Equal(SuggestState.Manual, model.State);
        model.Trigger(new SuggestTriggerOptions(Auto: true));
        Assert.Equal(SuggestState.Auto, model.State);
        model.Cancel();
        Assert.Equal(SuggestState.Idle, model.State);
    }

    [Fact]
    public void TypeMoreCharacters_Refilters_WithoutNewRequest()
    {
        using var editor = new SuggestTestEditor("f");
        FakeCompletionProvider provider = Fixed("foobar", "foo", "other");
        editor.Providers.Register("plaintext", provider);
        editor.MoveCaret(1, 2);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        model.Trigger(new SuggestTriggerOptions(Auto: true));
        editor.Flush();
        editor.Type("oob");
        editor.Flush();

        Assert.Single(provider.Calls);
        Assert.Equal(["foobar"], Labels(LastSuggest(recorder)));
        Assert.DoesNotContain(recorder.Log, l => l.StartsWith("cancel", StringComparison.Ordinal));
    }

    [Fact]
    public void CursorLeft_OnWord_Retriggers()
    {
        using var editor = new SuggestTestEditor("foo");
        editor.Providers.Register("plaintext", AlwaysSomething());
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        recorder.Log.Clear();
        editor.MoveCaret(1, 3);
        editor.Flush();

        Assert.Equal(["cancel retrigger=true", "trigger auto=false", "suggest items=1 frozen=false"], recorder.Log);
    }

    [Fact]
    public void CursorLeft_OffWord_Cancels()
    {
        using var editor = new SuggestTestEditor("foo  bar");
        editor.Providers.Register("plaintext", AlwaysSomething());
        editor.MoveCaret(1, 9);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        recorder.Log.Clear();
        editor.MoveCaret(1, 5);

        Assert.Equal(["cancel retrigger=false"], recorder.Log);
    }

    [Fact]
    public void NewLine_Cancels()
    {
        using var editor = new SuggestTestEditor("foo\nbar");
        editor.Providers.Register("plaintext", AlwaysSomething());
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        recorder.Log.Clear();
        editor.MoveCaret(2, 2);

        Assert.Equal(["cancel retrigger=false"], recorder.Log);
    }

    [Fact]
    public void SelectionNonEmpty_Cancels()
    {
        using var editor = new SuggestTestEditor("foo");
        editor.Providers.Register("plaintext", AlwaysSomething());
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        recorder.Log.Clear();
        editor.Select(new Selection(1, 1, 1, 4));

        Assert.Equal(["cancel retrigger=false"], recorder.Log);
    }

    [Fact]
    public void MouseMove_SourceMouse_Cancels()
    {
        using var editor = new SuggestTestEditor("foo");
        editor.Providers.Register("plaintext", AlwaysSomething());
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        recorder.Log.Clear();
        editor.MoveCaret(1, 3, "mouse");

        Assert.Equal(["cancel retrigger=false"], recorder.Log);
    }

    [Fact]
    public void ContentChanged_WhileIdle_DoesNothing()
    {
        using var editor = new SuggestTestEditor("foo");
        editor.Providers.Register("plaintext", AlwaysSomething());
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.Type("x");
        editor.Flush();

        Assert.Empty(recorder.Log);
        Assert.Equal(SuggestState.Idle, model.State);
    }

    [Fact]
    public void LanguageChanged_Cancels()
    {
        using var editor = new SuggestTestEditor("foo");
        editor.Providers.Register("plaintext", AlwaysSomething());
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        recorder.Log.Clear();
        editor.Context.RegisterLanguage("other");
        editor.Model.Tokenization.SetLanguageId("other");

        Assert.Equal(["cancel retrigger=false"], recorder.Log);
    }

    [Fact]
    public void ProviderUnregistered_Cancels()
    {
        using var editor = new SuggestTestEditor("foo");
        IDisposable registration = editor.Providers.Register("plaintext", AlwaysSomething());
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        recorder.Log.Clear();
        registration.Dispose();

        Assert.Equal(["cancel retrigger=false"], recorder.Log);
    }

    [Fact]
    public void ProviderRegistered_WhileActive_Retriggers()
    {
        using var editor = new SuggestTestEditor("foo");
        editor.Providers.Register("plaintext", AlwaysSomething());
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        recorder.Log.Clear();
        editor.Providers.Register("plaintext", Fixed("foobar"));
        editor.Flush();

        Assert.Equal(["cancel retrigger=true", "trigger auto=false", "suggest items=2 frozen=false"], recorder.Log);
    }

    [Fact]
    public void Dispose_CancelsAndUnsubscribes()
    {
        using var editor = new SuggestTestEditor("foo");
        editor.Providers.Register("plaintext", AlwaysSomething());
        editor.MoveCaret(1, 4);
        SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        recorder.Log.Clear();
        model.Dispose();
        Assert.Equal(["cancel retrigger=false"], recorder.Log);

        recorder.Log.Clear();
        editor.Type("x");
        editor.MoveCaret(1, 2);
        editor.Providers.Register("plaintext", Fixed("a"));
        editor.Flush();
        Assert.Empty(recorder.Log);
    }

    [Fact]
    public void LeadingWhitespaceChange_Cancels()
    {
        using var editor = new SuggestTestEditor("    foo");
        editor.Providers.Register("plaintext", AlwaysSomething());
        editor.MoveCaret(1, 8);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        recorder.Log.Clear();
        editor.Context.Commands.Execute("outdent");
        editor.Flush();

        Assert.Equal(["cancel retrigger=false"], recorder.Log);
    }

    [Fact]
    public void ManualSession_FreezesWhenNothingMatches()
    {
        using var editor = new SuggestTestEditor("foo");
        editor.Providers.Register("plaintext", Fixed("foo"));
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        recorder.Log.Clear();
        editor.Type("x");
        editor.Flush();

        Assert.Equal(["suggest items=1 frozen=true"], recorder.Log);
        Assert.True(LastSuggest(recorder).IsFrozen);
    }

    [Fact]
    public void ManualSession_FrozenThenNotOnWord_Cancels()
    {
        using var editor = new SuggestTestEditor("foo");
        editor.Providers.Register("plaintext", Fixed("foo"));
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();
        editor.Type("x");
        editor.Flush();

        recorder.Log.Clear();
        editor.Type(" ");
        editor.Flush();

        Assert.Equal(["cancel retrigger=false"], recorder.Log);
    }

    [Fact]
    public void IncompleteProvider_TypingRetriggers_OnlyThatProvider()
    {
        using var editor = new SuggestTestEditor("f");
        var incomplete = new FakeCompletionProvider
        {
            OnProvide = (_, position, _) => List(true, Item("foo", CompletionRange.FromRange(FromColumnOne(position)))),
        };
        FakeCompletionProvider complete = Fixed("foobar");
        editor.Providers.Register("plaintext", incomplete);
        editor.Providers.Register("plaintext", complete);
        editor.MoveCaret(1, 2);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();

        editor.Type("o");
        editor.Flush();

        Assert.Equal(2, incomplete.Calls.Count);
        Assert.Equal(CompletionTriggerKind.TriggerForIncompleteCompletions, incomplete.Calls[1].Context.TriggerKind);
        Assert.Single(complete.Calls);
        Assert.Equal(["foo", "foobar"], Labels(LastSuggest(recorder)));
    }

    [Fact]
    public void NewWord_RetriggersReusingItems()
    {
        using var editor = new SuggestTestEditor(string.Empty);
        FakeCompletionProvider provider = Fixed("Z aaa");
        editor.Providers.Register("plaintext", provider);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        editor.Type("Z");
        model.Trigger(new SuggestTriggerOptions(Auto: true));
        editor.Flush();

        recorder.Log.Clear();
        editor.Type(" a");
        editor.Flush();

        Assert.Single(provider.Calls);
        Assert.Contains("cancel retrigger=true", recorder.Log);
        Assert.Equal(["Z aaa"], Labels(LastSuggest(recorder)));
    }
}
