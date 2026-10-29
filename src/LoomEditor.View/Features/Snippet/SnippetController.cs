// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetController2.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;

namespace LoomEditor.View.Features.Snippet;

/// <summary>
/// Snippet mode of one editor: inserts snippets, moves between their tab stops and leaves snippet mode (Monaco: <c>SnippetController2</c>).
/// </summary>
/// <remarks>
/// <para>
/// Behavior differences: choices show their first option (no choice list, no <c>inSnippetChoice</c>); no <c>apply(edits)</c>; no
/// model-changed listener (an editor has one model: a replaced text arrives as a flush content change); the insert errors are not
/// logged, they cancel snippet mode and are rethrown.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class SnippetController : IDisposable
{
    private readonly EditorContext _editor;
    private SnippetSession? _session;
    private bool _listening;
    private int _modelVersionId = -1;

    /// <summary>Creates the controller of <paramref name="editor"/>.</summary>
    public SnippetController(EditorContext editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        _editor = editor;
    }

    /// <summary>True while a snippet with tab stops is active (Monaco: <c>inSnippetMode</c>).</summary>
    public bool IsInSnippet => _editor.InSnippetMode;

    /// <summary>The range that contains every placeholder of the active snippet, or <see langword="null"/>.</summary>
    public Range? GetSessionEnclosingRange() => _session?.GetEnclosingRange();

    /// <summary>Inserts <paramref name="template"/> at every cursor, or merges it into the active placeholder when in snippet mode.</summary>
    public void Insert(string template, SnippetInsertOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (_editor.IsReadOnly)
        {
            return;
        }

        try
        {
            DoInsert(template, options ?? new SnippetInsertOptions());
        }
        catch
        {
            Cancel();
            throw;
        }
    }

    /// <summary>Walks to the end of the snippet.</summary>
    public void Finish()
    {
        while (IsInSnippet)
        {
            Next();
        }
    }

    /// <summary>Leaves snippet mode; with <paramref name="resetSelection"/> only the primary cursor stays.</summary>
    public void Cancel(bool resetSelection = false)
    {
        SetKeys(false, false, false);
        StopListening();

        _session?.Dispose();
        _session = null;
        _modelVersionId = -1;
        if (resetSelection)
        {
            // Reset the selection to the primary cursor when explicitly cancelling snippet mode, e.g. when pressing Escape.
            _editor.SetSelections([_editor.Selection]);
        }
    }

    /// <summary>Moves to the previous tab stop.</summary>
    public void Prev()
    {
        _session?.Prev();
        UpdateState();
    }

    /// <summary>Moves to the next tab stop.</summary>
    public void Next()
    {
        _session?.Next();
        UpdateState();
    }

    /// <summary>Leaves snippet mode.</summary>
    public void Dispose() => Cancel();

    private void DoInsert(string template, SnippetInsertOptions options)
    {
        // Do not listen while inserting the snippet: that is the in-flight state causing cancelation.
        StopListening();

        TextModel model = _editor.Model;
        if (options.UndoStopBefore)
        {
            model.PushStackElement();
        }

        var sessionOptions = new SnippetSessionInsertOptions
        {
            OverwriteBefore = options.OverwriteBefore,
            OverwriteAfter = options.OverwriteAfter,
            AdjustWhitespace = options.AdjustWhitespace,
            ClipboardText = options.ClipboardText,
        };

        if (_session is null)
        {
            _modelVersionId = model.AlternativeVersionId;
            _session = new SnippetSession(_editor, template, sessionOptions, _editor.SnippetVariableHost, TimeProvider.System, Random.Shared);
            _session.Insert();
        }
        else
        {
            _session.Merge(template, sessionOptions);
        }

        if (options.UndoStopAfter)
        {
            model.PushStackElement();
        }

        UpdateState();

        if (_session is not null)
        {
            model.ContentChanged += OnContentChanged;
            _editor.SelectionChanged += OnSelectionChanged;
            _listening = true;
        }
    }

    private void OnContentChanged(object? sender, ModelContentChangedEventArgs e)
    {
        if (e.IsFlush)
        {
            Cancel();
        }
    }

    private void OnSelectionChanged(object? sender, EventArgs e) => UpdateState();

    private void StopListening()
    {
        if (_listening)
        {
            _editor.Model.ContentChanged -= OnContentChanged;
            _editor.SelectionChanged -= OnSelectionChanged;
            _listening = false;
        }
    }

    private void UpdateState()
    {
        if (_session is null)
        {
            // Canceled in the meanwhile.
            return;
        }

        if (_modelVersionId == _editor.Model.AlternativeVersionId)
        {
            // Undo until the 'before' state happened: cancel snippet mode.
            Cancel();
            return;
        }

        if (!_session.HasPlaceholder)
        {
            // Do not listen for selection changes and do not update the context keys when the snippet is plain text.
            Cancel();
            return;
        }

        if (_session.IsAtLastPlaceholder || !_session.IsSelectionWithinPlaceholders())
        {
            _editor.Model.PushStackElement();
            Cancel();
            return;
        }

        SetKeys(true, !_session.IsAtFirstPlaceholder, !_session.IsAtLastPlaceholder);
    }

    private void SetKeys(bool inSnippet, bool hasPrev, bool hasNext)
    {
        _editor.InSnippetMode = inSnippet;
        _editor.HasPrevTabstop = hasPrev;
        _editor.HasNextTabstop = hasNext;
    }
}
