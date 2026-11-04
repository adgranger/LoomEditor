using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LoomEditor.Avalonia.Controls.Suggest;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class SuggestWidgetTests
{
    private static readonly RawInputModifiers CommandKey = OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control;

    private sealed class FixedCompletionProvider(IReadOnlyList<string> labels) : ICompletionItemProvider
    {
        public IReadOnlyList<string>? TriggerCharacters => null;

        public Task<CompletionList?> ProvideCompletionItemsAsync(TextModel model, Position position, CompletionContext context, CancellationToken token)
        {
            // The word before the cursor is replaced by the chosen item.
            var range = new Range(position.LineNumber, 1, position.LineNumber, position.Column);
            var items = labels.Select(label => new CompletionItem(label, label, CompletionItemKind.Property) { Range = new CompletionRange(range, range) }).ToList();
            return Task.FromResult<CompletionList?>(new CompletionList(items, false));
        }
    }

    private static async Task<bool> WaitUntil(Func<bool> condition)
    {
        for (int i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(20);
        }

        return condition();
    }

    private static (Editor Editor, Window Window, SuggestWidget Widget) Open(IReadOnlyList<string> labels, string text = "")
    {
        var editor = new Editor { Text = text, MinimapEnabled = false };
        editor.LanguageFeatures.CompletionProviders.Register("*", new FixedCompletionProvider(labels));
        Window window = EditorWindow.Open(editor);
        editor.Focus();
        EditorWindow.Capture(window);
        editor.SetSelections([Selection.Collapsed(new Position(1, text.Length + 1))]);
        SuggestWidget widget = editor.GetVisualDescendants().OfType<SuggestWidget>().Single();
        return (editor, window, widget);
    }

    private static async Task TriggerSuggest(Window window, SuggestWidget widget)
    {
        window.KeyPress(Key.Space, CommandKey, PhysicalKey.Space, " ");
        Assert.True(await WaitUntil(() => widget.IsVisible), "the list did not show");
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public async Task CtrlSpace_ShowsSuggestions()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (Editor _, Window window, SuggestWidget widget) = Open(["alpha", "beta", "gamma"]);

            await TriggerSuggest(window, widget);

            Assert.Equal(3, widget.VisibleRowControlCount);
            Assert.Equal("alpha", widget.RowLabel(0));
            window.Close();
        });
    }

    [Fact]
    public async Task Down_Enter_AcceptsSecond()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (Editor editor, Window window, SuggestWidget widget) = Open(["alpha", "beta", "gamma"]);
            await TriggerSuggest(window, widget);

            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("beta", editor.Model.GetValue());
            Assert.False(widget.IsVisible);
            window.Close();
        });
    }

    [Fact]
    public async Task Typing_FiltersList()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (Editor _, Window window, SuggestWidget widget) = Open(["alpha", "beta", "gamma"]);
            await TriggerSuggest(window, widget);

            window.KeyTextInput("be");
            Assert.True(await WaitUntil(() => widget.VisibleRowControlCount == 1), $"{widget.VisibleRowControlCount} rows");

            Assert.Equal("beta", widget.RowLabel(0));
            window.Close();
        });
    }

    [Fact]
    public async Task Escape_Hides()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (Editor _, Window window, SuggestWidget widget) = Open(["alpha", "beta", "gamma"]);
            await TriggerSuggest(window, widget);

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Dispatcher.UIThread.RunJobs();

            Assert.False(widget.IsVisible);
            window.Close();
        });
    }

    [Fact]
    public async Task List_Of_10000_Items_IsVirtualized()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            string[] labels = [.. Enumerable.Range(0, 10000).Select(i => "item" + i)];
            (Editor _, Window window, SuggestWidget widget) = Open(labels);

            await TriggerSuggest(window, widget);

            Assert.True(widget.RowControlCount < 50, $"{widget.RowControlCount} row controls");
            Assert.Equal(12, widget.VisibleRowControlCount);
            window.Close();
        });
    }
}
