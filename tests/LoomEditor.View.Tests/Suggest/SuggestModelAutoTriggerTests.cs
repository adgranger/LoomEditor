// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/test/browser/suggestModel.test.ts (suite 'SuggestModel - TriggerAndCancelOracle')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Cursors;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Suggest.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

/// <summary>Typing starts the sessions here: quick suggest after the delay (<c>Scheduler.AdvanceBy</c>) and trigger characters at once.</summary>
public class SuggestModelAutoTriggerTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(10);

    private static CompletionRange DefaultRange(TextModel model, Position position)
    {
        WordAtPosition word = model.GetWordUntilPosition(position);
        return CompletionRange.FromRange(new Range(position.LineNumber, word.StartColumn, position.LineNumber, word.EndColumn));
    }

    private static CompletionItem Item(string label, CompletionRange range, CompletionItemKind kind = CompletionItemKind.Property) =>
        new(label, label, kind) { Range = range };

    private static CompletionList List(bool incomplete, params CompletionItem[] items) => new(items, incomplete);

    // Monaco: alwaysSomethingSupport
    private static FakeCompletionProvider AlwaysSomething() => new()
    {
        OnProvide = (model, position, _) => List(false, Item(model.GetWordUntilPosition(position).Word, DefaultRange(model, position))),
    };

    private static FakeCompletionProvider Single(string label, IReadOnlyList<string>? triggerCharacters = null) => new(triggerCharacters)
    {
        OnProvide = (model, position, _) => List(false, Item(label, DefaultRange(model, position))),
    };

    private static FakeCompletionProvider FromColumnOne(string label, IReadOnlyList<string>? triggerCharacters = null) => new(triggerCharacters)
    {
        OnProvide = (_, position, _) => List(false, Item(label, CompletionRange.FromRange(new Range(position.LineNumber, 1, position.LineNumber, position.Column)))),
    };

    private static string[] Labels(SuggestEventArgs e) => [.. e.CompletionModel.Items.Select(i => i.TextLabel)];

    // ---- ported from Monaco ----

    // Monaco: 'trigger - on type'
    [Fact]
    public void Trigger_OnType()
    {
        using var editor = new SuggestTestEditor("abc def");
        FakeCompletionProvider provider = AlwaysSomething();
        editor.Providers.Register("plaintext", provider);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.MoveCaret(1, 4);
        editor.Type("d");
        editor.Scheduler.AdvanceBy(Delay);
        editor.Flush();

        SuggestEventArgs e = Assert.Single(recorder.Suggests);
        Assert.True(e.TriggerOptions.Auto);
        Assert.Single(e.CompletionModel.Items);
        Assert.Same(provider, e.CompletionModel.Items[0].Provider);
    }

    // Monaco: '#17400: Keep filtering suggestModel.ts after space'
    [Fact]
    public void Issue17400_KeepFilteringAfterSpace()
    {
        using var editor = new SuggestTestEditor("");
        editor.Providers.Register("plaintext", Single("My Table"));
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        // make sure completionModel starts here!
        model.Trigger(new SuggestTriggerOptions(Auto: true));
        editor.Flush();

        editor.MoveCaret(1, 1);
        editor.Type("My");
        editor.Scheduler.AdvanceBy(Delay);
        editor.Flush();
        SuggestEventArgs e = recorder.Suggests[^1];
        Assert.True(e.TriggerOptions.Auto);
        Assert.Equal(["My Table"], Labels(e));

        editor.MoveCaret(1, 3);
        editor.Type(" ");
        editor.Scheduler.AdvanceBy(Delay);
        editor.Flush();
        e = recorder.Suggests[^1];
        Assert.True(e.TriggerOptions.Auto);
        Assert.Equal(["My Table"], Labels(e));
    }

    // Monaco: '#21484: Trigger character always force a new completion session'
    [Fact]
    public void Issue21484_TriggerCharacterAlwaysForcesANewCompletionSession()
    {
        using var editor = new SuggestTestEditor("");
        editor.Providers.Register("plaintext", new FakeCompletionProvider
        {
            OnProvide = (_, position, _) => List(false, Item("foo.bar", CompletionRange.FromRange(new Range(position.LineNumber, 1, position.LineNumber, position.Column)))),
        });
        editor.Providers.Register("plaintext", new FakeCompletionProvider(["."])
        {
            OnProvide = (model, position, _) =>
            {
                string line = model.GetLineContent(position.LineNumber);
                int startColumn = position.Column + (line[position.Column - 2] == '.' ? 0 : -1);
                return List(false, Item("boom", CompletionRange.FromRange(new Range(position.LineNumber, startColumn, position.LineNumber, position.Column))));
            },
        });
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.MoveCaret(1, 1);
        editor.Type("foo");
        editor.Scheduler.AdvanceBy(Delay);
        editor.Flush();
        SuggestEventArgs e = recorder.Suggests[^1];
        Assert.True(e.TriggerOptions.Auto);
        Assert.Equal(["foo.bar"], Labels(e));

        editor.Type(".");
        editor.Flush();
        e = recorder.Suggests[^1];
        Assert.True(e.TriggerOptions.Auto);
        Assert.Equal(["foo.bar", "boom"], Labels(e));
    }

    // Monaco: 'Intellisense Completion doesn't respect space after equal sign (.html file), #29353 [1/2]'
    [Fact]
    public void Issue29353_1of2()
    {
        using var editor = new SuggestTestEditor("fo");
        editor.Providers.Register("plaintext", AlwaysSomething());
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        editor.MoveCaret(1, 3);

        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();
        SuggestEventArgs e = recorder.Suggests[^1];
        Assert.False(e.TriggerOptions.Auto);
        Assert.False(e.IsFrozen);
        Assert.Single(e.CompletionModel.Items);

        recorder.Log.Clear();
        editor.Type("+");
        Assert.Equal(["cancel retrigger=false"], recorder.Log);
    }

    // Monaco: 'Intellisense Completion doesn't respect space after equal sign (.html file), #29353 [2/2]'
    [Fact]
    public void Issue29353_2of2()
    {
        using var editor = new SuggestTestEditor("fo");
        editor.Providers.Register("plaintext", AlwaysSomething());
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        editor.MoveCaret(1, 3);

        model.Trigger(new SuggestTriggerOptions(Auto: false));
        editor.Flush();
        SuggestEventArgs e = recorder.Suggests[^1];
        Assert.False(e.TriggerOptions.Auto);
        Assert.False(e.IsFrozen);
        Assert.Single(e.CompletionModel.Items);

        recorder.Log.Clear();
        editor.Type(" ");
        Assert.Equal(["cancel retrigger=false"], recorder.Log);
    }

    // Monaco: 'Fails to render completion details #47988' (the completion part: the dispose counters of the lists do not exist here)
    [Fact]
    public void Issue47988_FailsToRenderCompletionDetails()
    {
        using var editor = new SuggestTestEditor("");
        editor.Providers.Register("plaintext", new FakeCompletionProvider
        {
            OnProvide = (model, position, _) => List(
                true,
                new CompletionItem("CompleteNot", "Incomplete", CompletionItemKind.Folder) { SortText = "a", Range = DefaultRange(model, position) }),
        });
        editor.Providers.Register("plaintext", new FakeCompletionProvider
        {
            CanResolve = true,
            OnResolve = item => item,
            OnProvide = (model, position, _) => List(
                false,
                new CompletionItem("Complete", "Complete", CompletionItemKind.Folder) { SortText = "z", Range = DefaultRange(model, position) }),
        });
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        editor.MoveCaret(1, 1);

        editor.Type("c");
        editor.Scheduler.AdvanceBy(Delay);
        editor.Flush();
        SuggestEventArgs e = recorder.Suggests[^1];
        Assert.True(e.TriggerOptions.Auto);
        Assert.Equal(2, e.CompletionModel.Items.Count);

        editor.Type("o");
        editor.Scheduler.AdvanceBy(Delay);
        editor.Flush();
        e = recorder.Suggests[^1];
        Assert.True(e.TriggerOptions.Auto);
        Assert.Equal(2, e.CompletionModel.Items.Count);
    }

    // Monaco: 'registerCompletionItemProvider with letters as trigger characters block other completion items to show up #127815'
    [Fact]
    public void Issue127815_LettersAsTriggerCharactersDoNotBlockOtherItems()
    {
        using var editor = new SuggestTestEditor("");
        static FakeCompletionProvider At(string text, IReadOnlyList<string>? chars = null) => new(chars)
        {
            OnProvide = (_, position, _) => List(false, Item(text, CompletionRange.FromRange(new Range(position.LineNumber, position.Column, position.LineNumber, position.Column)), CompletionItemKind.Class)),
        };
        editor.Providers.Register("plaintext", At("AAAA"));
        editor.Providers.Register("plaintext", At("AAAA", ["a", "."]));
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.MoveCaret(1, 1);
        editor.Type(".");
        editor.Flush();
        SuggestEventArgs e = recorder.Suggests[^1];
        Assert.True(e.TriggerOptions.Auto);
        Assert.Single(e.CompletionModel.Items);

        editor.SetText("");
        editor.MoveCaret(1, 1);
        editor.Type("a");
        editor.Scheduler.AdvanceBy(Delay);
        editor.Flush();
        e = recorder.Suggests[^1];
        Assert.True(e.TriggerOptions.Auto);
        Assert.Equal(2, e.CompletionModel.Items.Count);
    }

    // Monaco: 'Unexpected suggest scoring #167242'
    [Fact]
    public void Issue167242_UnexpectedSuggestScoring()
    {
        using var editor = new SuggestTestEditor("gi");
        editor.Providers.Register("*", new FakeCompletionProvider
        {
            // word-based
            OnProvide = (model, position, _) => List(false, Item("pull", DefaultRange(model, position), CompletionItemKind.Text)),
        });
        editor.Providers.Register("plaintext", new FakeCompletionProvider
        {
            // JSON-based
            OnProvide = (_, position, _) => List(false, Item("git.pull", CompletionRange.FromRange(new Range(position.LineNumber, 1, position.LineNumber, position.Column)), CompletionItemKind.Class)),
        });
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        editor.Select(Selection.Collapsed(new Position(1, 3)));

        editor.Type("t");
        editor.Scheduler.AdvanceBy(Delay);
        editor.Flush();
        SuggestEventArgs e = recorder.Suggests[^1];
        Assert.True(e.TriggerOptions.Auto);
        Assert.Equal(["git.pull"], Labels(e));

        editor.Type(".");
        editor.Type("p");
        editor.Scheduler.AdvanceBy(Delay);
        editor.Flush();
        e = recorder.Suggests[^1];
        Assert.True(e.TriggerOptions.Auto);
        Assert.Equal(["git.pull"], Labels(e));
    }

    // ---- new ----

    [Fact]
    public void QuickSuggest_FiresAfterTheDelay()
    {
        using var editor = new SuggestTestEditor("abc def");
        editor.Providers.Register("plaintext", AlwaysSomething());
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        editor.MoveCaret(1, 4);

        editor.Type("d");
        editor.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(9));
        Assert.Empty(recorder.Log);

        editor.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        Assert.Equal(["trigger auto=true"], recorder.Log);
    }

    [Fact]
    public void QuickSuggest_RestartsOnEachKeystroke()
    {
        using var editor = new SuggestTestEditor("");
        editor.Providers.Register("plaintext", AlwaysSomething());
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.Type("d");
        editor.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(5));
        editor.Type("e");
        editor.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(5));
        Assert.Empty(recorder.Log);

        editor.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(5));
        Assert.Equal(["trigger auto=true"], recorder.Log);
    }

    [Fact]
    public void QuickSuggest_NotInsideAComment() => AssertNoQuickSuggestInside(StandardTokenType.Comment);

    [Fact]
    public void QuickSuggest_NotInsideAString() => AssertNoQuickSuggestInside(StandardTokenType.String);

    private static void AssertNoQuickSuggestInside(StandardTokenType tokenType)
    {
        using var editor = new SuggestTestEditor("");
        editor.Providers.Register("plaintext", AlwaysSomething());
        using var model = new SuggestModel(editor.Context, SuggestModelOptions.Default, (_, _) => tokenType);
        var recorder = new SuggestModelRecorder(model);

        editor.Type("d");
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Empty(recorder.Log);
    }

    [Fact]
    public void QuickSuggest_InsideACommentWithCommentsOn_Triggers()
    {
        using var editor = new SuggestTestEditor("");
        editor.Providers.Register("plaintext", AlwaysSomething());
        SuggestModelOptions options = SuggestModelOptions.Default with { QuickSuggestions = new QuickSuggestionsOptions(Comments: QuickSuggestionValue.On) };
        using var model = new SuggestModel(editor.Context, options, (_, _) => StandardTokenType.Comment);
        var recorder = new SuggestModelRecorder(model);

        editor.Type("d");
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Equal(["trigger auto=true"], recorder.Log);
    }

    [Fact]
    public void QuickSuggest_NotOnANumber()
    {
        using var editor = new SuggestTestEditor("");
        editor.Providers.Register("plaintext", AlwaysSomething());
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.Type("1");
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Empty(recorder.Log);
    }

    [Fact]
    public void QuickSuggest_NotWithoutProviderForTheLanguage()
    {
        using var editor = new SuggestTestEditor("");
        editor.Providers.Register("csharp", AlwaysSomething());
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.Type("d");
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Empty(recorder.Log);
    }

    [Fact]
    public void QuickSuggest_NotWithoutFocus()
    {
        using var editor = new SuggestTestEditor("");
        editor.Providers.Register("plaintext", AlwaysSomething());
        editor.Context.ViewModel.SetHasFocus(false);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.Type("d");
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Empty(recorder.Log);
    }

    [Fact]
    public void QuickSuggest_AllOff_Disabled()
    {
        using var editor = new SuggestTestEditor("");
        editor.Providers.Register("plaintext", AlwaysSomething());
        var off = new QuickSuggestionsOptions(QuickSuggestionValue.Off, QuickSuggestionValue.Off, QuickSuggestionValue.Off);
        using SuggestModel model = editor.CreateModel(SuggestModelOptions.Default with { QuickSuggestions = off });
        var recorder = new SuggestModelRecorder(model);

        editor.Type("d");
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Empty(recorder.Log);
    }

    [Fact]
    public void QuickSuggest_Inline_BehavesAsOff()
    {
        using var editor = new SuggestTestEditor("");
        editor.Providers.Register("plaintext", AlwaysSomething());
        using SuggestModel model = editor.CreateModel(SuggestModelOptions.Default with { QuickSuggestions = new QuickSuggestionsOptions(Other: QuickSuggestionValue.Inline) });
        var recorder = new SuggestModelRecorder(model);

        editor.Type("d");
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Empty(recorder.Log);
    }

    [Fact]
    public void QuickSuggest_Cancel_StopsTheTimer()
    {
        using var editor = new SuggestTestEditor("");
        editor.Providers.Register("plaintext", AlwaysSomething());
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.Type("d");
        model.Cancel();
        editor.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100));

        Assert.Empty(recorder.Log);
    }

    [Fact]
    public void Backspace_DoesNotTriggerQuickSuggest()
    {
        using var editor = new SuggestTestEditor("abcd");
        editor.Providers.Register("plaintext", AlwaysSomething());
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        editor.MoveCaret(1, 5);

        editor.Backspace();
        editor.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100));

        Assert.Empty(recorder.Log);
    }

    [Fact]
    public void TriggerCharacter_TriggersTheProviderAtOnce()
    {
        using var editor = new SuggestTestEditor("abc");
        FakeCompletionProvider provider = Single("x", ["."]);
        editor.Providers.Register("plaintext", provider);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        editor.MoveCaret(1, 4);

        editor.Type(".");

        Assert.Equal(["trigger auto=true"], recorder.Log);
        editor.Flush();
        CompletionContext context = Assert.Single(provider.Calls).Context;
        Assert.Equal(CompletionTriggerKind.TriggerCharacter, context.TriggerKind);
        Assert.Equal(".", context.TriggerCharacter);
    }

    [Fact]
    public void TriggerCharacter_OtherProviderItemsReused()
    {
        using var editor = new SuggestTestEditor("abc");
        FakeCompletionProvider other = FromColumnOne("abc.foo");
        FakeCompletionProvider dotProvider = FromColumnOne("abc.bar", ["."]);
        editor.Providers.Register("plaintext", other);
        editor.Providers.Register("plaintext", dotProvider);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        editor.MoveCaret(1, 4);
        model.Trigger(new SuggestTriggerOptions(Auto: true));
        editor.Flush();

        editor.Type(".");
        editor.Flush();

        Assert.Single(other.Calls);
        Assert.Equal(2, dotProvider.Calls.Count);
        Assert.Equal(["abc.bar", "abc.foo"], Labels(recorder.Suggests[^1]));
    }

    [Fact]
    public void TriggerCharacter_InMiddleOfQuickSuggestCase_Ignored()
    {
        using var editor = new SuggestTestEditor("");
        FakeCompletionProvider provider = Single("a1", ["a"]);
        editor.Providers.Register("plaintext", provider);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);

        editor.Type("a");
        Assert.Empty(recorder.Log);

        editor.Scheduler.AdvanceBy(Delay);
        editor.Flush();
        Assert.Equal(CompletionTriggerKind.Invoke, Assert.Single(provider.Calls).Context.TriggerKind);
    }

    [Fact]
    public void TriggerCharacter_SurrogatePairLastChar()
    {
        using var editor = new SuggestTestEditor("abcdef");
        FakeCompletionProvider provider = Single("x", ["\U0001F600"]);
        editor.Providers.Register("plaintext", provider);
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        // in the middle of a word: at its end, quick suggest would handle the emoji (it is a word character)
        editor.MoveCaret(1, 3);

        editor.Context.ViewModel.Type("\U0001F600", "keyboard");

        Assert.Equal(["trigger auto=true"], recorder.Log);
        editor.Flush();
        Assert.Equal("\U0001F600", Assert.Single(provider.Calls).Context.TriggerCharacter);
    }

    [Fact]
    public void TriggerCharacter_ReadOnly_Ignored()
    {
        using var editor = new SuggestTestEditor("abc");
        editor.Providers.Register("plaintext", Single("x", ["."]));
        editor.Context.ViewModel.SetCursorOptions(EditorCursorOptions.Default with { ReadOnly = true });
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        editor.MoveCaret(1, 4);

        editor.Type(".");

        Assert.Empty(recorder.Log);
    }

    [Fact]
    public void TriggerCharacter_Disabled_Ignored()
    {
        using var editor = new SuggestTestEditor("abc");
        editor.Providers.Register("plaintext", Single("x", ["."]));
        using SuggestModel model = editor.CreateModel(SuggestModelOptions.Default with { SuggestOnTriggerCharacters = false });
        var recorder = new SuggestModelRecorder(model);
        editor.MoveCaret(1, 4);

        editor.Type(".");

        Assert.Empty(recorder.Log);
    }

    [Fact]
    public void TriggerCharacters_UpdatedWhenProvidersChange()
    {
        using var editor = new SuggestTestEditor("abc");
        using SuggestModel model = editor.CreateModel();
        var recorder = new SuggestModelRecorder(model);
        editor.Providers.Register("plaintext", Single("x", [":"]));
        editor.MoveCaret(1, 4);

        editor.Type(":");

        Assert.Equal(["trigger auto=true"], recorder.Log);
    }

    [Fact]
    public void QuickSuggestionsOptions_ValueFor_MapsTokenTypes()
    {
        var options = new QuickSuggestionsOptions(QuickSuggestionValue.On, QuickSuggestionValue.Inline, QuickSuggestionValue.Off);

        Assert.Equal(QuickSuggestionValue.On, options.ValueFor(StandardTokenType.Other));
        Assert.Equal(QuickSuggestionValue.On, options.ValueFor(StandardTokenType.RegEx));
        Assert.Equal(QuickSuggestionValue.Inline, options.ValueFor(StandardTokenType.Comment));
        Assert.Equal(QuickSuggestionValue.Off, options.ValueFor(StandardTokenType.String));
    }

    [Fact]
    public void QuickSuggestionsOptions_IsAllOff_CountsInlineAsOff()
    {
        Assert.True(new QuickSuggestionsOptions(QuickSuggestionValue.Inline, QuickSuggestionValue.Off, QuickSuggestionValue.Inline).IsAllOff);
        Assert.False(QuickSuggestionsOptions.Default.IsAllOff);
    }
}
