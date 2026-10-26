using LoomEditor.View.Features;
using LoomEditor.View.Features.Suggest;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Suggest.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Suggest;

// New tests: the default features, typing and commands together.
public class SuggestEndToEndTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(10);

    private sealed class Harness : IDisposable
    {
        public Harness(FakeCompletionProvider provider)
        {
            Editor = new SuggestTestEditor();
            Editor.Context.SuggestPresenter = Presenter;
            Editor.Providers.Register("plaintext", provider);
            Host = new EditorFeatureHost(Editor.Context, EditorFeatures.Default);
        }

        public SuggestTestEditor Editor { get; }

        public FakeSuggestPresenter Presenter { get; } = new();

        public EditorFeatureHost Host { get; }

        public SuggestController Controller => ((SuggestFeature)Host.Features.Single(f => f is SuggestFeature)).Controller!;

        public string[] Labels => [.. Controller.List.Items.Select(i => i.TextLabel)];

        public void Type(string text, bool expectList = false)
        {
            Editor.Type(text);
            Editor.Scheduler.AdvanceBy(Delay);
            Settle(expectList);
        }

        public void Run(string id, bool expectList = false)
        {
            Editor.Context.Commands.Execute(id);
            Settle(expectList);
        }

        // The default features keep the scheduler busy, so SuggestTestEditor.Flush never sleeps and the provider task (thread pool) cannot finish:
        // run the dispatcher and the scheduler with short sleeps until the list state stops changing.
        private void Settle(bool expectList)
        {
            int unchanged = 0;
            (int, int, bool) last = default;
            for (int i = 0; i < 5000 && (expectList ? !Presenter.IsVisible : unchanged < 15); i++)
            {
                Editor.Dispatcher.RunAll();
                Editor.Scheduler.AdvanceBy(TimeSpan.Zero);
                Thread.Sleep(2);
                (int, int, bool) now = (Controller.List.Items.Count, Controller.List.SelectedIndex, Presenter.IsVisible);
                unchanged = now == last ? unchanged + 1 : 0;
                last = now;
            }
        }

        public void Dispose()
        {
            Host.Dispose();
            Editor.Dispose();
        }
    }

    private static Harness Create(IReadOnlyList<string>? triggerCharacters = null)
    {
        var provider = new FakeCompletionProvider(triggerCharacters)
        {
            OnProvide = SuggestFeatureTests.Items("foo", "foobar", "bar").OnProvide,
        };
        return new Harness(provider);
    }

    [Fact]
    public void TypeAWord_ShowsFilteredList_AcceptWithEnter_InsertsAndCloses()
    {
        using Harness h = Create();

        h.Type("fo", expectList: true);

        Assert.True(h.Presenter.IsVisible);
        Assert.Equal(["foo", "foobar"], h.Labels);

        h.Run(SuggestCommands.AcceptSelectedSuggestion);

        Assert.Equal("foo", h.Editor.Model.GetValue());
        Assert.False(h.Presenter.IsVisible);
    }

    [Fact]
    public void TriggerCharacter_Dot_ShowsList()
    {
        using Harness h = Create(["."]);

        h.Type(".", expectList: true);

        Assert.True(h.Presenter.IsVisible);
        Assert.Equal(3, h.Controller.List.Items.Count);
    }

    [Fact]
    public void EscapeClosesAndTypingAgainReopens()
    {
        using Harness h = Create();
        h.Type("fo", expectList: true);

        h.Run(SuggestCommands.HideSuggestWidget);
        Assert.False(h.Presenter.IsVisible);

        h.Type("o", expectList: true);

        Assert.True(h.Presenter.IsVisible);
        Assert.Equal(["foo", "foobar"], h.Labels);
    }

    [Fact]
    public void TypingNonMatching_ClosesAutoList()
    {
        using Harness h = Create();
        h.Type("fo", expectList: true);

        h.Type("z");

        Assert.False(h.Presenter.IsVisible);
    }

    [Fact]
    public void AcceptThenUndo_RestoresTypedText()
    {
        using Harness h = Create();
        h.Type("fo", expectList: true);
        h.Run(SuggestCommands.AcceptSelectedSuggestion);

        h.Editor.Model.Undo();

        Assert.Equal("fo", h.Editor.Model.GetValue());
    }

    [Fact]
    public void ManualTrigger_EmptyWord_ShowsAllItems()
    {
        using Harness h = Create();

        h.Run(SuggestCommands.TriggerSuggest, expectList: true);
        Assert.True(h.Presenter.IsVisible);
        Assert.Equal(["bar", "foo", "foobar"], h.Labels);
    }
}
