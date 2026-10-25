using LoomEditor.View.Commands;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.Suggest.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

public class SuggestControllerTests
{
    private static FakeCompletionProvider Items(params string[] labels) => new()
    {
        OnProvide = (model, position, _) =>
        {
            WordAtPosition word = model.GetWordUntilPosition(position);
            var range = CompletionRange.FromRange(new Range(position.LineNumber, word.StartColumn, position.LineNumber, word.EndColumn));
            return new CompletionList([.. labels.Select(l => new CompletionItem(l, l, CompletionItemKind.Property) { Range = range })], false);
        },
    };

    private static Harness Create(FakeCompletionProvider provider, string text = "abc def")
    {
        var editor = new SuggestTestEditor(text);
        var presenter = new FakeSuggestPresenter();
        editor.Context.SuggestPresenter = presenter;
        editor.Providers.Register("plaintext", provider);
        editor.MoveCaret(1, 4);
        return new Harness(editor, presenter, new SuggestController(editor.Context, SuggestModelOptions.Default));
    }

    [Fact]
    public void Suggest_ShowsTheList_AndSelectsTheFirst()
    {
        using Harness h = Create(Items("abc1", "abc2"));

        h.Controller.Trigger();
        h.Editor.Flush();

        Assert.Equal([new Position(1, 1)], h.Presenter.Shows);
        Assert.Equal(0, h.Controller.List.SelectedIndex);
        Assert.Equal(2, h.Controller.List.Items.Count);
        Assert.True(h.Controller.IsVisible);
    }

    [Fact]
    public void Suggest_Preselect_SelectsThatItem()
    {
        var provider = new FakeCompletionProvider
        {
            OnProvide = (_, position, _) =>
            {
                var range = CompletionRange.FromRange(new Range(1, 1, 1, position.Column));
                return new CompletionList(
                    [
                        new CompletionItem("abc1", "abc1", CompletionItemKind.Property) { Range = range },
                        new CompletionItem("abc2", "abc2", CompletionItemKind.Property) { Range = range, Preselect = true },
                    ],
                    false);
            },
        };
        using Harness h = Create(provider);

        h.Controller.Trigger();
        h.Editor.Flush();

        Assert.Equal(1, h.Controller.List.SelectedIndex);
    }

    [Fact]
    public void Suggest_NoItems_HidesPresenter()
    {
        using Harness h = Create(Items());

        h.Controller.Trigger();
        h.Editor.Flush();

        Assert.Empty(h.Presenter.Shows);
        Assert.False(h.Presenter.IsVisible);
        Assert.False(h.Controller.IsVisible);
    }

    [Fact]
    public void Suggest_Shy_DoesNotShow()
    {
        using Harness h = Create(Items("abc1"));

        h.Controller.Model.Trigger(new SuggestTriggerOptions(Shy: true));
        h.Editor.Flush();

        Assert.Empty(h.Presenter.Shows);
        Assert.True(h.Controller.List.IsEmpty);
    }

    [Fact]
    public void Cancel_HidesPresenter_ClearsList()
    {
        using Harness h = Create(Items("abc1"));
        h.Controller.Trigger();
        h.Editor.Flush();

        h.Controller.Cancel();

        Assert.False(h.Presenter.IsVisible);
        Assert.True(h.Controller.List.IsEmpty);
    }

    [Fact]
    public void Retrigger_KeepsThePresenterVisible()
    {
        using Harness h = Create(Items("abc1"));
        h.Controller.Trigger();
        h.Editor.Flush();

        h.Controller.Model.Trigger(new SuggestTriggerOptions(Retrigger: true));

        Assert.Equal(0, h.Presenter.Hides);
        Assert.True(h.Presenter.IsVisible);
        h.Editor.Flush();
        Assert.Equal(2, h.Presenter.Shows.Count);
    }

    [Fact]
    public void Blur_Cancels()
    {
        using Harness h = Create(Items("abc1"));
        h.Controller.Trigger();
        h.Editor.Flush();

        h.Editor.Context.ViewModel.SetHasFocus(false);

        Assert.Equal(SuggestState.Idle, h.Controller.Model.State);
        Assert.False(h.Presenter.IsVisible);
    }

    [Fact]
    public void Trigger_Manual_StartsAManualSession()
    {
        using Harness h = Create(Items("abc1"));

        h.Controller.Trigger();

        Assert.Equal(SuggestState.Manual, h.Controller.Model.State);
    }

    [Fact]
    public void Select_MovesReturnTheListResult()
    {
        using Harness h = Create(Items("abc1", "abc2", "abc3"));
        h.Controller.Trigger();
        h.Editor.Flush();

        Assert.True(h.Controller.SelectNext());
        Assert.Equal(1, h.Controller.List.SelectedIndex);
        Assert.True(h.Controller.SelectLast());
        Assert.False(h.Controller.SelectLast());
        Assert.True(h.Controller.SelectFirst());
        Assert.True(h.Controller.SelectPrevious());
        Assert.Equal(2, h.Controller.List.SelectedIndex);
        Assert.True(h.Controller.SelectPreviousPage());
        Assert.True(h.Controller.SelectNextPage());
        Assert.Equal(2, h.Controller.List.SelectedIndex);
    }

    [Fact]
    public void SelectionChange_ResolvesTheItem()
    {
        FakeCompletionProvider provider = Items("abc1");
        provider.CanResolve = true;
        provider.OnResolve = item => new CompletionItem(item.Label, item.InsertText, item.Kind) { Range = item.Range, Detail = "resolved" };
        using Harness h = Create(provider);
        var changed = 0;
        h.Controller.List.Changed += (_, _) => changed++;

        h.Controller.Trigger();
        h.Editor.Flush();

        Assert.True(h.Controller.List.SelectedItem!.IsResolved);
        Assert.Equal("resolved", h.Controller.List.SelectedItem.Completion.Detail);
        Assert.True(changed >= 2); // the reset, then the resolved item
    }

    [Fact]
    public void SelectionChange_CancelsThePreviousResolve()
    {
        FakeCompletionProvider provider = Items("abc1", "abc2");
        provider.CanResolve = true;
        provider.OnResolve = item => item;
        using Harness h = Create(provider);
        h.Controller.Trigger();
        h.Editor.Flush();
        Assert.False(provider.ResolveTokens[0].IsCancellationRequested);

        h.Controller.SelectNext();

        Assert.True(provider.ResolveTokens[0].IsCancellationRequested);
    }

    [Fact]
    public void ResolveFaulted_Ignored()
    {
        FakeCompletionProvider provider = Items("abc1");
        provider.CanResolve = true;
        provider.OnResolve = _ => throw new InvalidOperationException("resolve failed");
        using Harness h = Create(provider);

        h.Controller.Trigger();
        h.Editor.Flush();

        Assert.True(h.Presenter.IsVisible);
        Assert.False(h.Controller.List.SelectedItem!.IsResolved);
    }

    [Fact]
    public void Dispose_UnsubscribesAndCancels()
    {
        using Harness h = Create(Items("abc1"));
        h.Controller.Trigger();
        h.Editor.Flush();
        int hides = h.Presenter.Hides;

        h.Controller.Dispose();

        Assert.Equal(SuggestState.Idle, h.Controller.Model.State);
        Assert.Equal(hides, h.Presenter.Hides); // unsubscribed first: nothing reacts to the cancel
        h.Editor.Context.ViewModel.SetHasFocus(false);
        Assert.Equal(hides, h.Presenter.Hides);
    }

    [Fact]
    public void NonEditorContext_DoesNothing()
    {
        using var controller = new SuggestController(new FakeContext(), SuggestModelOptions.Default);

        controller.Trigger();

        Assert.Equal(SuggestState.Idle, controller.Model.State);
        Assert.False(controller.IsVisible);
        Assert.False(controller.AcceptSelected());
    }

    [Fact]
    public void IsVisible_FollowsPresenterAndList()
    {
        using Harness h = Create(Items("abc1"));
        Assert.False(h.Controller.IsVisible);

        h.Controller.Trigger();
        h.Editor.Flush();
        Assert.True(h.Controller.IsVisible);

        h.Presenter.Hide();
        Assert.False(h.Controller.IsVisible);
    }

    private sealed class Harness(SuggestTestEditor editor, FakeSuggestPresenter presenter, SuggestController controller) : IDisposable
    {
        public SuggestTestEditor Editor { get; } = editor;

        public FakeSuggestPresenter Presenter { get; } = presenter;

        public SuggestController Controller { get; } = controller;

        public void Dispose()
        {
            Controller.Dispose();
            Editor.Dispose();
        }
    }

    private sealed class FakeContext : IEditorContext
    {
        public IEditorScheduler Scheduler { get; } = new ManualEditorScheduler();

        public CommandRegistry Commands { get; } = new();

        public TextModel Model { get; } = new("abc");

        public Selection Selection { get; set; } = Selection.Collapsed(new Position(1, 1));

        public IReadOnlyList<Selection> Selections => [Selection];

        public event EventHandler? SelectionChanged
        {
            add { }
            remove { }
        }

        public void SetSelections(IReadOnlyList<Selection> selections) => Selection = selections[0];
    }
}
