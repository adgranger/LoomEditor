// Ported from microsoft/vscode: src/vs/editor/contrib/wordHighlighter/browser/wordHighlighter.ts (WordHighlighterContribution, the actions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.WordHighlighter;

/// <summary>Highlights the occurrences of the word at the cursor, and adds the commands to go through them.</summary>
/// <remarks>
/// Not ported: the screen reader <c>alert</c> of the navigation, <c>onDidChangeModel</c> (the model of an editor never changes),
/// the <c>hasWordHighlights</c> context key (the commands check it themselves) and the semantic providers (phase 8).
/// Threading: UI thread only.
/// </remarks>
internal sealed class WordHighlighterFeature : IEditorFeature
{
    private static readonly TimeSpan RestoreDelay = TimeSpan.FromMilliseconds(250);

    private readonly List<IDisposable> _registrations = [];
    private WordHighlighter? _wordHighlighter;
    private EditorWordHighlightOptions _options = EditorWordHighlightOptions.Default;

    /// <inheritdoc />
    public string Id => "loom.wordHighlighter";

    /// <summary>The highlighter, or <see langword="null"/> when the model is too large (Monaco: <c>wordHighlighter</c>).</summary>
    internal WordHighlighter? WordHighlighter => _wordHighlighter;

    /// <summary>The options of the highlighter.</summary>
    internal EditorWordHighlightOptions Options
    {
        get => _options;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _options = value;
            if (_wordHighlighter is not null)
            {
                _wordHighlighter.Options = value;
            }
        }
    }

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ViewModel? viewModel = context.GetViewModel();
        if (viewModel is null)
        {
            return;
        }

        // Monaco: createWordHighlighterIfPossible
        if (!viewModel.Model.IsTooLargeForTokenization)
        {
            _wordHighlighter = new WordHighlighter(viewModel, context.Scheduler, _options);
        }

        void Register(string id, Action run) => _registrations.Add(context.Commands.Register(new EditorCommand(id, run)));

        // Monaco: precondition `hasWordHighlights`
        Register(WordHighlighterCommands.Next, () =>
        {
            if (_wordHighlighter is { HasDecorations: true } highlighter)
            {
                highlighter.MoveNext();
            }
        });
        Register(WordHighlighterCommands.Prev, () =>
        {
            if (_wordHighlighter is { HasDecorations: true } highlighter)
            {
                highlighter.MoveBack();
            }
        });

        // Monaco: restoreViewState(true), with a 250 ms delay
        Register(WordHighlighterCommands.Trigger, () => _wordHighlighter?.Restore(RestoreDelay));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        _wordHighlighter?.Dispose();
        _wordHighlighter = null;
    }
}
