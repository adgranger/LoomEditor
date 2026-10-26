// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/test/browser/suggestModel.test.ts (createMockEditor, withOracle, assertEvent)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.Highlighting.TestHelpers;

namespace LoomEditor.View.Tests.Suggest.TestHelpers;

/// <summary>An editor with a manual scheduler and dispatcher, driven like Monaco's <c>createMockEditor</c> + <c>withOracle</c>.</summary>
/// <remarks>Threading: use from one test thread; provider tasks complete on the thread pool and are brought back by <see cref="Flush"/>.</remarks>
internal sealed class SuggestTestEditor : IDisposable
{
    public SuggestTestEditor(string text = "", string languageId = "plaintext")
    {
        Scheduler = new ManualEditorScheduler();
        Dispatcher = new ManualUiDispatcher();
        Context = new EditorContext(_ => new NoopDisposable(), TimeProvider.System, Dispatcher, Scheduler);
        Context.ViewModel.SetHasFocus(true);
        SetText(text, languageId);
    }

    public EditorContext Context { get; }

    public ManualEditorScheduler Scheduler { get; }

    public ManualUiDispatcher Dispatcher { get; }

    public TextModel Model => Context.Model;

    public LanguageFeatureRegistry<ICompletionItemProvider> Providers => Context.LanguageFeatures.CompletionProviders;

    public SuggestModel CreateModel(SuggestModelOptions? options = null) => new(Context, options ?? SuggestModelOptions.Default);

    public void SetText(string text, string languageId = "plaintext")
    {
        if (languageId != "plaintext")
        {
            Context.RegisterLanguage(languageId);
        }

        Model.Tokenization.SetLanguageId(languageId);
        Context.SetText(text);
    }

    /// <summary>Monaco: <c>editor.setPosition</c> (the reason is <see cref="CursorChangeReason.Explicit"/>; the source is a cursor key by default).</summary>
    public void MoveCaret(int line, int column, string source = "keyboard") =>
        Context.ViewModel.SetSelections(source, [Selection.Collapsed(new Position(line, column))], CursorChangeReason.Explicit);

    public void Select(Selection selection, string source = "keyboard") =>
        Context.ViewModel.SetSelections(source, [selection], CursorChangeReason.Explicit);

    /// <summary>Monaco: <c>editor.trigger('keyboard', Handler.Type, { text })</c>, one call per character.</summary>
    public void Type(string text)
    {
        foreach (char c in text)
        {
            Context.ViewModel.Type(c.ToString(), "keyboard");
        }
    }

    /// <summary>Monaco: <c>editor.runCommand(CoreEditingCommands.DeleteLeft)</c>.</summary>
    public void Backspace() => Context.Commands.Execute("deleteLeft");

    /// <summary>
    /// Runs what the providers posted to the dispatcher and what the scheduler has due, until the editor stays quiet: the provider tasks
    /// finish on the thread pool, so a few quiet rounds are needed to be sure none is on its way. A queued thread pool item counts as
    /// work: on a busy machine (several test assemblies at once) it can wait far longer than the quiet rounds, so the loop is bounded by a deadline.
    /// </summary>
    public void Flush()
    {
        var quiet = 0;
        long deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 10;
        while (quiet < 20 && Stopwatch.GetTimestamp() < deadline)
        {
            bool didWork = Dispatcher.PendingCount > 0 || Scheduler.PendingCount > 0 || ThreadPool.PendingWorkItemCount > 0;
            Dispatcher.RunAll();
            Scheduler.AdvanceBy(TimeSpan.Zero);
            quiet = didWork ? 0 : quiet + 1;
            if (!didWork)
            {
                Thread.Sleep(1);
            }
        }
    }

    public void Dispose() => Context.Dispose();

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

/// <summary>Records the events of a <see cref="SuggestModel"/> as lines such as <c>trigger auto=false</c>, <c>suggest items=3 frozen=false</c>, <c>cancel retrigger=false</c>.</summary>
/// <remarks>Threading: use from one test thread.</remarks>
internal sealed class SuggestModelRecorder
{
    public SuggestModelRecorder(SuggestModel model)
    {
        model.DidTrigger += (_, e) => Log.Add($"trigger auto={Lower(e.Auto)}");
        model.DidSuggest += (_, e) =>
        {
            Log.Add($"suggest items={e.CompletionModel.Items.Count} frozen={Lower(e.IsFrozen)}");
            Suggests.Add(e);
        };
        model.DidCancel += (_, e) => Log.Add($"cancel retrigger={Lower(e.Retrigger)}");
    }

    public List<string> Log { get; } = [];

    public List<SuggestEventArgs> Suggests { get; } = [];

    private static string Lower(bool value) => value ? "true" : "false";
}
