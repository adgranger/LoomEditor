using System.Diagnostics;
using LoomEditor.Core.Model;
using LoomEditor.View.Controller;
using LoomEditor.View.Layout;
using Microsoft.UI.Dispatching;
using Windows.UI.Text.Core;

namespace LoomEditor.WinUI.Input;

/// <summary>
/// Types with an IME, the emoji panel and other Windows text services through a <see cref="CoreTextEditContext"/>
/// (Monaco: the DOM events of <c>NativeEditContext</c>; the logic is in <see cref="EditContextModel"/>).
/// </summary>
/// <remarks>
/// Windows 11 only: <see cref="TryCreate"/> returns <see langword="null"/> where the text services are not available.
/// Plain typing stays on <c>CharacterReceived</c>; while <see cref="IsComposing"/> the control ignores keys and characters.
/// Threading: UI thread only. <c>Notify*</c> calls are never made from inside a handler: they are queued on the dispatcher.
/// </remarks>
internal sealed class ImeInput : IDisposable
{
    private static bool _unavailableTraced;

    private readonly CoreTextEditContext _editContext;
    private readonly EditContextModel _editContextModel = new();
    private readonly ViewModel _viewModel;
    private readonly TextModel _model;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Func<bool> _isReadOnly;
    private bool _applying;
    private bool _disposed;

    private ImeInput(CoreTextEditContext editContext, ViewModel viewModel, TextModel model, DispatcherQueue dispatcherQueue, Func<bool> isReadOnly)
    {
        _editContext = editContext;
        _viewModel = viewModel;
        _model = model;
        _dispatcherQueue = dispatcherQueue;
        _isReadOnly = isReadOnly;

        _editContextModel.Update(_model, _viewModel.GetSelections()[0]);
        _editContext.InputScope = CoreTextInputScope.Text;
        _editContext.InputPaneDisplayPolicy = CoreTextInputPaneDisplayPolicy.Manual;
        _editContext.TextRequested += OnTextRequested;
        _editContext.SelectionRequested += OnSelectionRequested;
        _editContext.TextUpdating += OnTextUpdating;
        _editContext.SelectionUpdating += OnSelectionUpdating;
        _editContext.FormatUpdating += OnFormatUpdating;
        _editContext.LayoutRequested += OnLayoutRequested;
        _editContext.CompositionStarted += OnCompositionStarted;
        _editContext.CompositionCompleted += OnCompositionCompleted;
    }

    /// <summary>Whether a composition is active (between <c>CompositionStarted</c> and <c>CompositionCompleted</c>).</summary>
    public bool IsComposing { get; private set; }

    /// <summary>Creates the IME input, or returns <see langword="null"/> when the text services are not available (Windows 10).</summary>
#pragma warning disable CA1031 // Whatever the failure is, the editor must keep working without IME.
    public static ImeInput? TryCreate(ViewModel viewModel, TextModel model, DispatcherQueue dispatcherQueue, Func<bool> isReadOnly)
    {
        try
        {
            CoreTextServicesManager manager = CoreTextServicesManager.GetForCurrentView();
            return new ImeInput(manager.CreateEditContext(), viewModel, model, dispatcherQueue, isReadOnly);
        }
        catch (Exception exception)
        {
            if (!_unavailableTraced)
            {
                _unavailableTraced = true;
                Debug.WriteLine($"LoomEditor: IME is unavailable ({exception.GetType().Name}); typing works without it.");
            }

            return null;
        }
    }
#pragma warning restore CA1031

    /// <summary>Tells the text services that the control got the focus.</summary>
    public void NotifyFocusEnter() => _editContext.NotifyFocusEnter();

    /// <summary>Tells the text services that the control lost the focus.</summary>
    public void NotifyFocusLeave() => _editContext.NotifyFocusLeave();

    /// <summary>
    /// Re-reads the lines of the primary selection and tells the text services what changed. The call is queued: the
    /// caller may be a cursor event raised inside an IME handler.
    /// </summary>
    public void RequestSync()
    {
        if (_applying || _disposed)
        {
            return;
        }

        QueueSync(notify: true);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _editContext.TextRequested -= OnTextRequested;
        _editContext.SelectionRequested -= OnSelectionRequested;
        _editContext.TextUpdating -= OnTextUpdating;
        _editContext.SelectionUpdating -= OnSelectionUpdating;
        _editContext.FormatUpdating -= OnFormatUpdating;
        _editContext.LayoutRequested -= OnLayoutRequested;
        _editContext.CompositionStarted -= OnCompositionStarted;
        _editContext.CompositionCompleted -= OnCompositionCompleted;
        _editContext.NotifyFocusLeave();
    }

    private void QueueSync(bool notify)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (!_disposed)
            {
                Sync(notify);
            }
        });
    }

    // Monaco: `_updateEditContext`. When the IME made the change itself it already knows the text: only our copy is refreshed.
    private void Sync(bool notify)
    {
        int previousLength = _editContextModel.Text.Length;
        (bool textChanged, bool selectionChanged) = _editContextModel.Update(_model, _viewModel.GetSelections()[0]);
        if (!notify)
        {
            return;
        }

        var selection = new CoreTextRange { StartCaretPosition = _editContextModel.Selection.Start, EndCaretPosition = _editContextModel.Selection.End };
        if (textChanged)
        {
            _editContext.NotifyTextChanged(
                new CoreTextRange { StartCaretPosition = 0, EndCaretPosition = previousLength }, _editContextModel.Text.Length, selection);
        }
        else if (selectionChanged)
        {
            _editContext.NotifySelectionChanged(selection);
        }
    }

    private void OnTextRequested(CoreTextEditContext sender, CoreTextTextRequestedEventArgs args)
    {
        string text = _editContextModel.Text;
        int start = Math.Clamp(args.Request.Range.StartCaretPosition, 0, text.Length);
        int end = Math.Clamp(args.Request.Range.EndCaretPosition, start, text.Length);
        args.Request.Text = text[start..end];
    }

    private void OnSelectionRequested(CoreTextEditContext sender, CoreTextSelectionRequestedEventArgs args)
    {
        (int start, int end) = _editContextModel.Selection;
        args.Request.Selection = new CoreTextRange { StartCaretPosition = start, EndCaretPosition = end };
    }

    // Every update is handled the same way, with or without a composition: the emoji panel sends no CompositionStarted.
    private void OnTextUpdating(CoreTextEditContext sender, CoreTextTextUpdatingEventArgs args)
    {
        if (_isReadOnly())
        {
            args.Result = CoreTextTextUpdatingResult.Failed;
            return;
        }

        var update = new TextUpdate(
            args.Range.StartCaretPosition, args.Range.EndCaretPosition, args.Text, args.NewSelection.StartCaretPosition, args.NewSelection.EndCaretPosition);
        TypeData? data = _editContextModel.OnTextUpdate(update);
        args.Result = CoreTextTextUpdatingResult.Succeeded;
        Apply(data);
    }

    private void OnSelectionUpdating(CoreTextEditContext sender, CoreTextSelectionUpdatingEventArgs args)
    {
        TypeData? data = _editContextModel.OnSelectionUpdate(args.Selection.StartCaretPosition, args.Selection.EndCaretPosition);
        args.Result = CoreTextSelectionUpdatingResult.Succeeded;
        Apply(data);
    }

    private void Apply(TypeData? data)
    {
        if (data is null)
        {
            return;
        }

        _applying = true;
        try
        {
            EditContextModel.Apply(_viewModel, data.Value);
        }
        finally
        {
            _applying = false;
        }

        QueueSync(notify: false);
    }

    // Underlines come with P3-53.
    private static void OnFormatUpdating(CoreTextEditContext sender, CoreTextFormatUpdatingEventArgs args) =>
        args.Result = CoreTextFormatUpdatingResult.Succeeded;

    // The candidate window position comes with P3-52.
    private static void OnLayoutRequested(CoreTextEditContext sender, CoreTextLayoutRequestedEventArgs args)
    {
    }

    private void OnCompositionStarted(CoreTextEditContext sender, CoreTextCompositionStartedEventArgs args)
    {
        IsComposing = true;
        _viewModel.StartComposition();
    }

    private void OnCompositionCompleted(CoreTextEditContext sender, CoreTextCompositionCompletedEventArgs args)
    {
        IsComposing = false;
        _applying = true;
        try
        {
            _viewModel.EndComposition("keyboard");
        }
        finally
        {
            _applying = false;
        }

        QueueSync(notify: true);
    }
}
