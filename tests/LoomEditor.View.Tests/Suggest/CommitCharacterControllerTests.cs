// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestCommitCharacters.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Suggest.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

public class CommitCharacterControllerTests
{
    private sealed class Harness : IDisposable
    {
        public Harness(SuggestModelOptions? options = null, params (string Label, char[]? Commit)[] items)
        {
            Editor = new SuggestTestEditor("fo");
            (string, char[]?)[] defs = items.Length == 0 ? [("foo", ['(', '.'])] : items;
            Editor.Providers.Register("plaintext", new FakeCompletionProvider
            {
                OnProvide = (_, position, _) => new CompletionList(
                    [.. defs.Select(d => new CompletionItem(d.Item1, d.Item1, CompletionItemKind.Property)
                    {
                        Range = CompletionRange.FromRange(new Range(1, 1, 1, position.Column)),
                        CommitCharacters = d.Item2,
                    })],
                    false),
            });
            Controller = new SuggestController(Editor.Context, options ?? SuggestModelOptions.Default);
            Editor.MoveCaret(1, 3);
            Controller.Trigger();
            Editor.Flush();
        }

        public SuggestTestEditor Editor { get; }

        public SuggestController Controller { get; }

        public string Text => Editor.Model.GetValue();

        public void Dispose()
        {
            Controller.Dispose();
            Editor.Dispose();
        }
    }

    [Fact]
    public void TypeCommitCharacter_AcceptsThenTypesIt()
    {
        using var h = new Harness();

        h.Editor.Type(".");

        Assert.Equal("foo.", h.Text);

        // typing pushes an undo stop of its own before the first typed character, so the accept and the character are two steps
        h.Editor.Model.Undo();
        Assert.Equal("foo", h.Text);
        h.Editor.Model.Undo();
        Assert.Equal("fo", h.Text);
    }

    [Fact]
    public void TypeOtherCharacter_DoesNotAccept()
    {
        using var h = new Harness();

        h.Editor.Type("x");

        Assert.Equal("fox", h.Text);
    }

    [Fact]
    public void NoCommitCharacters_NoAccept()
    {
        using var h = new Harness(null, ("foo", null));

        h.Editor.Type(".");

        Assert.Equal("fo.", h.Text);
    }

    [Fact]
    public void Frozen_DoesNotAccept()
    {
        using var h = new Harness();
        h.Controller.List.Reset(h.Controller.List.Items, 0, true);

        h.Editor.Type(".");

        Assert.Equal("fo.", h.Text);
    }

    [Fact]
    public void SessionIdle_DoesNotAccept()
    {
        using var h = new Harness();
        h.Controller.Cancel();

        h.Editor.Type(".");

        Assert.Equal("fo.", h.Text);
    }

    [Fact]
    public void Disabled_NoAccept()
    {
        using var h = new Harness(SuggestModelOptions.Default with { AcceptSuggestionOnCommitCharacter = false });

        h.Editor.Type(".");

        Assert.Equal("fo.", h.Text);
    }

    [Fact]
    public void SelectionChange_UsesTheNewItemsCharacters()
    {
        using var h = new Harness(null, ("foo1", ['(']), ("foo2", ['.']));
        h.Controller.SelectNext();

        h.Editor.Type(".");

        Assert.Equal("foo2.", h.Text);
    }

    [Fact]
    public void DidCancel_Resets()
    {
        using var h = new Harness();
        h.Controller.Model.Cancel();
        h.Controller.Model.Trigger(new SuggestTriggerOptions(Shy: true));

        h.Editor.Type(".");

        Assert.Equal("fo.", h.Text);
    }

    [Fact]
    public void SurrogateLastUnit_Ignored()
    {
        using var h = new Harness(null, ("foo", ['.']));

        // only the last UTF-16 unit counts: here a low surrogate
        h.Editor.Context.ViewModel.Type(".\U0001F600", "keyboard");

        Assert.Equal("fo.\U0001F600", h.Text);
    }

    [Fact]
    public void Dispose_Unsubscribes()
    {
        using var h = new Harness();
        h.Controller.Dispose();

        h.Editor.Type(".");

        Assert.Equal("fo.", h.Text);
    }
}
