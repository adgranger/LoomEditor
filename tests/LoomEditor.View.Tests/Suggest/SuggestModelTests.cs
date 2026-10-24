// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/test/browser/suggestModel.test.ts (suite 'SuggestModel - TriggerAndCancelOracle')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Suggest.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

/// <summary>
/// Monaco's <c>assertEvent</c> helpers become checks on <see cref="SuggestModelRecorder"/>. Typing never starts a session in this card
/// (quick suggest and trigger characters are P8-17): where Monaco's typing started one, the test calls <c>Trigger</c> and says so.
/// </summary>
public partial class SuggestModelTests
{
    private static CompletionRange DefaultRange(TextModel model, Position position)
    {
        WordAtPosition word = model.GetWordUntilPosition(position);
        return CompletionRange.FromRange(new Range(position.LineNumber, word.StartColumn, position.LineNumber, word.EndColumn));
    }

    private static CompletionList List(bool incomplete, params CompletionItem[] items) => new(items, incomplete);

    private static CompletionItem Item(string label, CompletionRange range, CompletionItemKind kind = CompletionItemKind.Property) =>
        new(label, label, kind) { Range = range };

    // Monaco: alwaysEmptySupport
    private static FakeCompletionProvider AlwaysEmpty() => new() { OnProvide = (_, _, _) => List(false) };

    // Monaco: alwaysSomethingSupport
    private static FakeCompletionProvider AlwaysSomething() => new()
    {
        OnProvide = (model, position, _) => List(false, Item(model.GetWordUntilPosition(position).Word, DefaultRange(model, position))),
    };

    private static Range FromColumnOne(Position position) => new(position.LineNumber, 1, position.LineNumber, position.Column);

    private static SuggestEventArgs LastSuggest(SuggestModelRecorder recorder) => recorder.Suggests[^1];

    private static string[] Labels(SuggestEventArgs e) => [.. e.CompletionModel.Items.Select(i => i.TextLabel)];

    // ---- ported from Monaco ----

    // Monaco: 'events - cancel/trigger'
    [Fact]
    public void Events_CancelTrigger()
    {
        using var editor = new SuggestTestEditor("abc def");
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        model.Trigger(new SuggestTriggerOptions(Auto: true));
        model.Cancel();
        model.Trigger(new SuggestTriggerOptions(Auto: true));
        model.Trigger(new SuggestTriggerOptions(Auto: false));

        Assert.Equal(
            ["trigger auto=true", "cancel retrigger=false", "trigger auto=true", "cancel retrigger=false", "trigger auto=false"],
            recorder.Log);
    }

    // Monaco: 'events - suggest/empty'
    [Fact]
    public void Events_SuggestEmpty()
    {
        using var editor = new SuggestTestEditor("abc def");
        editor.Providers.Register("plaintext", AlwaysEmpty());
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        model.Trigger(new SuggestTriggerOptions(Auto: true));
        editor.Flush();
        Assert.Equal(["trigger auto=true", "cancel retrigger=false"], recorder.Log);

        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();
        Assert.Equal(["trigger auto=true", "cancel retrigger=false", "trigger auto=false", "suggest items=0 frozen=false"], recorder.Log);
        Assert.False(LastSuggest(recorder).TriggerOptions.Auto);
    }

    // Monaco: '#17400: Keep filtering suggestModel.ts after space'
    // Replaced: `editor.trigger('keyboard', Handler.Type, { text: 'My' })` started the session through quick suggest; here `Trigger` does it
    // right before typing (Monaco's `setPosition` calls with source 'api' would cancel it, so they are dropped: the caret is already there).
    [Fact]
    public void KeepFilteringAfterSpace_17400()
    {
        using var editor = new SuggestTestEditor(string.Empty);
        editor.Providers.Register("plaintext", new FakeCompletionProvider
        {
            OnProvide = (model, position, _) => List(false, Item("My Table", DefaultRange(model, position))),
        });
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        // make sure completionModel starts here!
        model.Trigger(new SuggestTriggerOptions(Auto: true));
        editor.Flush();
        Assert.Single(LastSuggest(recorder).CompletionModel.Items);

        editor.Type("My");
        editor.Flush();
        SuggestEventArgs afterMy = LastSuggest(recorder);
        Assert.True(afterMy.TriggerOptions.Auto);
        Assert.Equal(["My Table"], Labels(afterMy));

        editor.Type(" ");
        editor.Flush();
        SuggestEventArgs afterSpace = LastSuggest(recorder);
        Assert.True(afterSpace.TriggerOptions.Auto);
        Assert.Equal(["My Table"], Labels(afterSpace));
    }

    // Monaco: 'Incomplete suggestion results cause re-triggering when typing w/o further context, #28400 (1/2)'
    [Fact]
    public void IncompleteResults_TypingWithoutContext_Cancels_28400_1()
    {
        using var editor = new SuggestTestEditor("foo");
        editor.Providers.Register("plaintext", new FakeCompletionProvider
        {
            OnProvide = (_, position, _) => List(true, Item("foo", CompletionRange.FromRange(FromColumnOne(position)))),
        });
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();
        SuggestEventArgs first = LastSuggest(recorder);
        Assert.False(first.TriggerOptions.Auto);
        Assert.Single(first.CompletionModel.GetIncompleteProvider());
        Assert.Single(first.CompletionModel.Items);

        recorder.Log.Clear();
        editor.Type(";");
        editor.Flush();
        Assert.Equal(["cancel retrigger=false"], recorder.Log);
    }

    // Monaco: 'Incomplete suggestion results cause re-triggering when typing w/o further context, #28400 (2/2)'
    [Fact]
    public void IncompleteResults_TypingWithoutContext_KeepsFiltering_28400_2()
    {
        using var editor = new SuggestTestEditor("foo");
        editor.Providers.Register("plaintext", new FakeCompletionProvider
        {
            OnProvide = (_, position, _) => List(true, Item("foo;", CompletionRange.FromRange(FromColumnOne(position)))),
        });
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();
        Assert.Single(LastSuggest(recorder).CompletionModel.GetIncompleteProvider());

        // while we cancel incrementally enriching the set of completions we still filter against those that we have until now
        editor.Type(";");
        editor.Flush();
        SuggestEventArgs after = LastSuggest(recorder);
        Assert.False(after.TriggerOptions.Auto);
        Assert.Single(after.CompletionModel.GetIncompleteProvider());
        Assert.Single(after.CompletionModel.Items);
    }

    // Monaco: 'Trigger character is provided in suggest context'
    // Replaced: typing 'foo.' started the session through the trigger character; here `Trigger` carries the character.
    [Fact]
    public void TriggerCharacter_IsProvidedInTheSuggestContext()
    {
        using var editor = new SuggestTestEditor(string.Empty);
        FakeCompletionProvider provider = new(["."])
        {
            OnProvide = (_, position, _) => List(false, Item("foo.bar", CompletionRange.FromRange(FromColumnOne(position)))),
        };
        editor.Providers.Register("plaintext", provider);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        model.Trigger(new SuggestTriggerOptions(Auto: true, TriggerKind: CompletionTriggerKind.TriggerCharacter, TriggerCharacter: "."));
        editor.Flush();

        CompletionContext context = Assert.Single(provider.Calls).Context;
        Assert.Equal(CompletionTriggerKind.TriggerCharacter, context.TriggerKind);
        Assert.Equal(".", context.TriggerCharacter);
        Assert.Single(recorder.Suggests);
    }

    // Monaco: 'Backspace should not always cancel code completion, #36491'
    // Replaced: typing 'd' started the session through quick suggest; here `Trigger` does it after the character is typed.
    [Fact]
    public void Backspace_DoesNotAlwaysCancel_36491()
    {
        using var editor = new SuggestTestEditor("abc def");
        FakeCompletionProvider provider = AlwaysSomething();
        editor.Providers.Register("plaintext", provider);
        editor.MoveCaret(1, 4);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.Type("d");
        model.Trigger(new SuggestTriggerOptions(Auto: true));
        editor.Flush();
        SuggestEventArgs first = LastSuggest(recorder);
        Assert.True(first.TriggerOptions.Auto);
        Assert.Single(first.CompletionModel.Items);
        Assert.Same(provider, first.CompletionModel.Items[0].Provider);

        recorder.Suggests.Clear();
        editor.Backspace();
        editor.Flush();
        SuggestEventArgs second = LastSuggest(recorder);
        Assert.True(second.TriggerOptions.Auto);
        Assert.Single(second.CompletionModel.Items);
        Assert.Same(provider, second.CompletionModel.Items[0].Provider);
    }

    // Monaco: 'Trigger (full) completions when (incomplete) completions are already active #99504'
    // Replaced: typing 'Z' started the session through quick suggest; here `Trigger` does it after the character is typed.
    [Fact]
    public void FullCompletions_WhenCompletionsAreAlreadyActive_99504()
    {
        using var editor = new SuggestTestEditor(string.Empty);
        FakeCompletionProvider a = new()
        {
            OnProvide = (_, position, _) => List(false, Item("Z aaa", CompletionRange.FromRange(FromColumnOne(position)), CompletionItemKind.Class)),
        };
        FakeCompletionProvider b = new()
        {
            OnProvide = (model, position, _) => model.GetWordUntilPosition(position).Word.StartsWith('a')
                ? List(false, Item("aaa", DefaultRange(model, position), CompletionItemKind.Folder))
                : null,
        };
        editor.Providers.Register("plaintext", a);
        editor.Providers.Register("plaintext", b);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.Type("Z");
        model.Trigger(new SuggestTriggerOptions(Auto: true));
        editor.Flush();
        SuggestEventArgs first = LastSuggest(recorder);
        Assert.True(first.TriggerOptions.Auto);
        Assert.Equal(["Z aaa"], Labels(first));

        // started another word: Z a| -> items should be: Z aaa, aaa
        editor.Type(" a");
        editor.Flush();
        SuggestEventArgs second = LastSuggest(recorder);
        Assert.True(second.TriggerOptions.Auto);
        Assert.Equal(["Z aaa", "aaa"], Labels(second));
        Assert.Single(a.Calls); // the suggestions of the "active" provider are kept, see Monaco #106573
        Assert.Equal(2, b.Calls.Count);
    }

    // Monaco: 'Completion list closes unexpectedly when typing a digit after a word separator #169390'
    // Typing '2' starts the trigger character session (P8-17), as in Monaco.
    [Fact]
    public void TypingADigitAfterAWordSeparator_KeepsTheList_169390()
    {
        using var editor = new SuggestTestEditor("foo");
        FakeCompletionProvider p1 = new()
        {
            OnProvide = (_, position, _) => List(
                false,
                Item("foo-20", CompletionRange.FromRange(FromColumnOne(position)), CompletionItemKind.Text),
                Item("foo-hello", CompletionRange.FromRange(FromColumnOne(position)), CompletionItemKind.Text)),
        };
        FakeCompletionProvider p2 = new(["2"])
        {
            OnProvide = (_, position, context) => context.TriggerKind != CompletionTriggerKind.TriggerCharacter
                ? null
                : List(false, Item("foo-210", CompletionRange.FromRange(FromColumnOne(position)), CompletionItemKind.Class)),
        };
        editor.Providers.Register("plaintext", p1);
        editor.Providers.Register("plaintext", p2);
        editor.Select(new Selection(1, 4, 1, 4));
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();
        SuggestEventArgs first = LastSuggest(recorder);
        Assert.False(first.TriggerOptions.Auto);
        Assert.Equal(["foo-20", "foo-hello"], Labels(first));

        editor.Type("-");
        editor.Type("2");
        editor.Flush();

        SuggestEventArgs second = LastSuggest(recorder);
        Assert.True(second.TriggerOptions.Auto);
        Assert.Equal(["foo-20", "foo-210"], Labels(second));
        Assert.Equal([1, 2], [p1.Calls.Count, p2.Calls.Count]);
    }

    // Monaco: 'Set refilter-flag, keep triggerKind'
    // Replaced: typing 'o' and '.' started sessions through quick suggest and the trigger character; here `Trigger` does it after each character.
    [Fact]
    public void RefilterFlag_KeepsTheTriggerKind()
    {
        using var editor = new SuggestTestEditor("foo");
        editor.Providers.Register("plaintext", new FakeCompletionProvider(["."])
        {
            OnProvide = (model, position, _) =>
            {
                string word = model.GetWordUntilPosition(position).Word;
                return List(false, Item(word.Length == 0 ? "hello" : word, DefaultRange(model, position)));
            },
        });
        editor.Select(new Selection(1, 4, 1, 4));
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.Type("o");
        model.Trigger(new SuggestTriggerOptions(Auto: true));
        editor.Flush();
        SuggestTriggerOptions first = LastSuggest(recorder).TriggerOptions;
        Assert.True(first.Auto);
        Assert.Null(first.TriggerCharacter);
        Assert.Null(first.TriggerKind);
        Assert.Single(LastSuggest(recorder).CompletionModel.Items);

        editor.Type(".");
        model.Trigger(new SuggestTriggerOptions(Auto: true, Retrigger: true, TriggerKind: CompletionTriggerKind.TriggerCharacter, TriggerCharacter: "."));
        editor.Flush();
        SuggestTriggerOptions second = LastSuggest(recorder).TriggerOptions;
        Assert.True(second.Auto);
        Assert.False(second.Refilter);
        Assert.Equal(".", second.TriggerCharacter);
        Assert.Equal(CompletionTriggerKind.TriggerCharacter, second.TriggerKind);
        Assert.Single(LastSuggest(recorder).CompletionModel.Items);

        editor.Type("h");
        editor.Flush();
        SuggestTriggerOptions third = LastSuggest(recorder).TriggerOptions;
        Assert.True(third.Auto);
        Assert.True(third.Refilter);
        Assert.Equal(".", third.TriggerCharacter);
        Assert.Equal(CompletionTriggerKind.TriggerCharacter, third.TriggerKind);
        Assert.Single(LastSuggest(recorder).CompletionModel.Items);
    }
}
