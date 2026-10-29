// Ported from microsoft/vscode: src/vs/editor/contrib/smartSelect/browser/smartSelect.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.SmartSelect;

/// <summary>
/// Expand and shrink selection (Monaco: <c>SmartSelectController</c>, <c>GrowSelectionAction</c> and <c>ShrinkSelectionAction</c>):
/// grows each selection to the next enclosing range and goes back.
/// </summary>
/// <remarks>
/// Behavior differences: synchronous, no provider registry (only the bracket and word providers), no menus.
/// Threading: UI thread only.
/// </remarks>
public sealed class SmartSelectFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];
    private IEditorContext? _context;
    private ViewModel? _viewModel;
    private List<SelectionRanges>? _state;
    private bool _ignoreSelection;

    /// <inheritdoc />
    public string Id => "loom.smartSelect";

    internal SmartSelectOptions Options { get; set; } = new(true, true);

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.GetViewModel() is not { } viewModel)
        {
            return;
        }

        _context = context;
        _viewModel = viewModel;
        // listen to caret move and forget about state
        viewModel.EventDispatcher.OnEvent += OnViewModelEvent;
        _registrations.Add(context.Commands.Register(new EditorCommand(SmartSelectCommands.Expand, () => Run(true))));
        _registrations.Add(context.Commands.Register(new EditorCommand(SmartSelectCommands.Shrink, () => Run(false))));
    }

    private void OnViewModelEvent(object? sender, OutgoingViewModelEvent e)
    {
        if (e is CursorStateChangedEvent && !_ignoreSelection)
        {
            _state = null;
        }
    }

    private void Run(bool forward)
    {
        List<Selection> selections = _viewModel!.GetSelections();

        if (_state is null)
        {
            List<List<Range>> ranges = SmartSelectRanges.Provide(_context!.Model, [.. selections.Select(s => s.Position)], Options);
            if (ranges.Count == 0 || ranges.Count != selections.Count)
            {
                // invalid result
                return;
            }

            _state = [];
            for (int i = 0; i < ranges.Count; i++)
            {
                Selection selection = selections[i];
                // filter ranges inside the selection, then prepend the current selection
                List<Range> own = [selection.Range, .. ranges[i].Where(r => r.Contains(selection.Range.Start) && r.Contains(selection.Range.End))];
                _state.Add(new SelectionRanges(0, own));
            }
        }

        _state = [.. _state.Select(state => state.Mov(forward))];
        List<Selection> newSelections = [.. _state.Select(state => new Selection(state.Ranges[state.Index].Start, state.Ranges[state.Index].End))];
        _ignoreSelection = true;
        try
        {
            _viewModel.SetSelections(null, newSelections);
        }
        finally
        {
            _ignoreSelection = false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_viewModel is not null)
        {
            _viewModel.EventDispatcher.OnEvent -= OnViewModelEvent;
        }

        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        _state = null;
        _context = null;
        _viewModel = null;
    }
}
