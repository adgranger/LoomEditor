using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Features.Suggest;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Suggest.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Suggest;

// New tests: Monaco has no test of the suggest actions.
public class SuggestFeatureTests
{
    private static readonly string[] AllIds =
    [
        SuggestCommands.TriggerSuggest, SuggestCommands.AcceptSelectedSuggestion, SuggestCommands.HideSuggestWidget,
        SuggestCommands.SelectNextSuggestion, SuggestCommands.SelectPrevSuggestion, SuggestCommands.SelectNextPageSuggestion,
        SuggestCommands.SelectPrevPageSuggestion, SuggestCommands.SelectFirstSuggestion, SuggestCommands.SelectLastSuggestion,
    ];

    internal static FakeCompletionProvider Items(params string[] labels) => new()
    {
        OnProvide = (model, position, _) =>
        {
            WordAtPosition word = model.GetWordUntilPosition(position);
            var range = CompletionRange.FromRange(new Range(position.LineNumber, word.StartColumn, position.LineNumber, word.EndColumn));
            return new CompletionList([.. labels.Select(l => new CompletionItem(l, l, CompletionItemKind.Property) { Range = range })], false);
        },
    };

    private sealed class Harness : IDisposable
    {
        public Harness(FakeCompletionProvider? provider, string text = "ab", bool attach = true)
        {
            Editor = new SuggestTestEditor(text);
            Presenter = new FakeSuggestPresenter();
            Editor.Context.SuggestPresenter = Presenter;
            if (provider is not null)
            {
                Editor.Providers.Register("plaintext", provider);
            }

            Editor.MoveCaret(1, text.Length + 1);
            if (attach)
            {
                Feature.Attach(Editor.Context);
            }
        }

        public SuggestTestEditor Editor { get; }

        public FakeSuggestPresenter Presenter { get; }

        public SuggestFeature Feature { get; } = new();

        public bool Run(string id)
        {
            bool ran = Editor.Context.Commands.Execute(id);
            Editor.Flush();
            return ran;
        }

        public void Open()
        {
            Run(SuggestCommands.TriggerSuggest);
            for (int i = 0; i < 500 && !Feature.Controller!.IsVisible; i++)
            {
                Editor.Flush();
            }

            Assert.True(Feature.Controller!.IsVisible);
        }

        public void Dispose() => Editor.Dispose();
    }

    [Fact]
    public void Attach_RegistersTheNineCommands()
    {
        using var h = new Harness(Items("abc"));

        foreach (string id in AllIds)
        {
            Assert.True(h.Editor.Context.Commands.Contains(id), id);
        }
    }

    [Fact]
    public void Dispose_UnregistersTheCommands_AndTheController()
    {
        using var h = new Harness(Items("abc"));

        h.Feature.Dispose();

        foreach (string id in AllIds)
        {
            Assert.False(h.Editor.Context.Commands.Contains(id), id);
        }

        Assert.Null(h.Feature.Controller);
    }

    [Fact]
    public void NonEditorContext_AttachDoesNothing()
    {
        var feature = new SuggestFeature();

        feature.Attach(new FakeContext(new CommandRegistry()));

        Assert.Null(feature.Controller);
    }

    [Fact]
    public void Flags_FollowTheList()
    {
        using var h = new Harness(Items("abc", "abd"));
        EditorContext context = h.Editor.Context;
        Assert.False(context.SuggestWidgetVisible);

        h.Open();

        Assert.True(context.SuggestWidgetVisible);
        Assert.True(context.SuggestWidgetMultipleSuggestions);

        h.Run(SuggestCommands.HideSuggestWidget);

        Assert.False(context.SuggestWidgetVisible);
        Assert.False(context.SuggestWidgetMultipleSuggestions);
    }

    [Fact]
    public void Flags_SingleItem_NotMultiple()
    {
        using var h = new Harness(Items("abc"));

        h.Open();

        Assert.True(h.Editor.Context.SuggestWidgetVisible);
        Assert.False(h.Editor.Context.SuggestWidgetMultipleSuggestions);
    }

    [Fact]
    public void Trigger_Command_OpensTheList()
    {
        using var h = new Harness(Items("abc", "abd"));

        h.Run(SuggestCommands.TriggerSuggest);

        Assert.True(h.Presenter.IsVisible);
        Assert.Equal(2, h.Feature.Controller!.List.Items.Count);
    }

    [Fact]
    public void Trigger_Command_WhenVisible_NoOp()
    {
        FakeCompletionProvider provider = Items("abc", "abd");
        using var h = new Harness(provider);
        h.Open();
        int calls = provider.Calls.Count;

        h.Run(SuggestCommands.TriggerSuggest);

        Assert.Equal(calls, provider.Calls.Count);
    }

    [Fact]
    public void Trigger_Command_ReadOnly_NoOp()
    {
        FakeCompletionProvider provider = Items("abc");
        using var h = new Harness(provider);
        h.Editor.Context.ViewModel.SetCursorOptions(h.Editor.Context.ViewModel.CursorOptions with { ReadOnly = true });

        h.Run(SuggestCommands.TriggerSuggest);

        Assert.Empty(provider.Calls);
        Assert.False(h.Presenter.IsVisible);
    }

    [Fact]
    public void Trigger_Command_NoProvider_NoOp()
    {
        using var h = new Harness(null);

        h.Run(SuggestCommands.TriggerSuggest);

        Assert.Empty(h.Presenter.Shows);
    }

    [Fact]
    public void Accept_Command_InsertsTheSelectedItem()
    {
        using var h = new Harness(Items("abc", "abd"));
        h.Open();

        h.Run(SuggestCommands.AcceptSelectedSuggestion);

        Assert.Equal("abc", h.Editor.Model.GetValue());
        Assert.False(h.Presenter.IsVisible);
    }

    [Fact]
    public void Hide_Command_ClosesTheList()
    {
        using var h = new Harness(Items("abc", "abd"));
        h.Open();

        h.Run(SuggestCommands.HideSuggestWidget);

        Assert.False(h.Presenter.IsVisible);
        Assert.Equal("ab", h.Editor.Model.GetValue());
    }

    [Fact]
    public void Select_Commands_MoveTheSelection()
    {
        using var h = new Harness(Items("aa", "ab", "ac"), text: "a");
        h.Open();
        SuggestController controller = h.Feature.Controller!;

        h.Run(SuggestCommands.SelectNextSuggestion);
        Assert.Equal(1, controller.List.SelectedIndex);
        h.Run(SuggestCommands.SelectNextSuggestion);
        Assert.Equal(2, controller.List.SelectedIndex);
        h.Run(SuggestCommands.SelectPrevSuggestion);
        Assert.Equal(1, controller.List.SelectedIndex);
        h.Run(SuggestCommands.SelectNextPageSuggestion);
        Assert.Equal(2, controller.List.SelectedIndex);
        h.Run(SuggestCommands.SelectFirstSuggestion);
        Assert.Equal(0, controller.List.SelectedIndex);
        h.Run(SuggestCommands.SelectLastSuggestion);
        Assert.Equal(2, controller.List.SelectedIndex);
        h.Run(SuggestCommands.SelectPrevPageSuggestion);
        Assert.Equal(0, controller.List.SelectedIndex);
    }

    [Fact]
    public void Commands_WhenListHidden_NoOp()
    {
        using var h = new Harness(Items("abc", "abd"));

        foreach (string id in AllIds.Skip(1))
        {
            h.Run(id);
        }

        Assert.Equal("ab", h.Editor.Model.GetValue());
        Assert.Empty(h.Presenter.Shows);
    }

    private sealed class FakeContext(CommandRegistry commands) : IEditorContext
    {
        public IEditorScheduler Scheduler => throw new NotSupportedException();

        public CommandRegistry Commands { get; } = commands;

        public TextModel Model => throw new NotSupportedException();

        public Selection Selection
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public IReadOnlyList<Selection> Selections => throw new NotSupportedException();

        public bool IsReadOnly { get => false; set => throw new NotSupportedException(); }

        public event EventHandler? ReadOnlyChanged { add { } remove { } }

        public event EventHandler? SelectionChanged
        {
            add { }
            remove { }
        }

        public void SetSelections(IReadOnlyList<Selection> selections) => throw new NotSupportedException();
    }
}
