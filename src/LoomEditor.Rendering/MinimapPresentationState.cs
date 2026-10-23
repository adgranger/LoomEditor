using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Minimap;

namespace LoomEditor.Rendering;

/// <summary>
/// What Monaco keeps in CSS classes of the minimap: the pointer over the minimap, the slider hovered or dragged, and the
/// "active" class of the <c>scroll</c> auto-hide mode with the timer that removes it.
/// </summary>
/// <remarks>
/// <para>
/// Behavior difference: Monaco's opacity transitions (100 ms for the slider, 0.5 s for auto-hide) are not animated; the
/// minimap and the slider appear and disappear instantly.
/// </para>
/// Threading: UI thread only. The timer is an <see cref="IEditorScheduler"/> (none: the minimap then stays visible after a scroll).
/// </remarks>
internal sealed class MinimapPresentationState : IMinimapPresentation, IDisposable
{
    private static readonly TimeSpan HideDelay = TimeSpan.FromMilliseconds(500);

    private readonly Action? _requestRedraw;
    private readonly RunOnceScheduler? _hideDelayedScheduler;
    private bool _mouseOverMinimap;
    private bool _sliderHovered;
    private bool _sliderActive;

    /// <summary>Creates the state; <paramref name="requestRedraw"/> is called when a change must be drawn.</summary>
    public MinimapPresentationState(IEditorScheduler? scheduler, Action? requestRedraw)
    {
        _requestRedraw = requestRedraw;
        _hideDelayedScheduler = scheduler is null ? null : new RunOnceScheduler(scheduler, HideImmediatelyIfMouseIsOutside, HideDelay);
    }

    /// <summary>The pointer is over the minimap. Monaco: <c>_isMouseOverMinimap</c> (the <c>:hover</c> of the minimap).</summary>
    public bool MouseOverMinimap
    {
        get => _mouseOverMinimap;
        set => Set(ref _mouseOverMinimap, value);
    }

    /// <summary>The pointer is over the slider. Monaco: <c>.minimap-slider:hover</c>.</summary>
    public bool SliderHovered
    {
        get => _sliderHovered;
        set => Set(ref _sliderHovered, value);
    }

    /// <summary>The slider is being dragged. Monaco: <c>.minimap-slider.active</c>.</summary>
    public bool SliderActive
    {
        get => _sliderActive;
        set => Set(ref _sliderActive, value);
    }

    /// <summary>The minimap was shown by a scroll and is not hidden yet. Monaco: the <c>active</c> class of the minimap node.</summary>
    public bool ScrollActive { get; private set; }

    /// <summary>Shows the minimap and hides it again 500 ms later. Monaco: <c>onScrollChanged</c> in <c>scroll</c> mode.</summary>
    public void OnScrolled()
    {
        ScrollActive = true;
        // Monaco: _hideSoon
        _hideDelayedScheduler?.Schedule();
    }

    /// <inheritdoc/>
    public void Dispose() => _hideDelayedScheduler?.Dispose();

    // Monaco: _hideImmediatelyIfMouseIsOutside
    private void HideImmediatelyIfMouseIsOutside()
    {
        if (_mouseOverMinimap)
        {
            _hideDelayedScheduler?.Schedule();
            return;
        }

        ScrollActive = false;
        _requestRedraw?.Invoke();
    }

    private void Set(ref bool field, bool value)
    {
        if (field != value)
        {
            field = value;
            _requestRedraw?.Invoke();
        }
    }
}
