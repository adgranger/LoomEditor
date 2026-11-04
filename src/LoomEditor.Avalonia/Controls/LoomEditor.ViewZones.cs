using Avalonia;
using Avalonia.Controls;
using LoomEditor.Avalonia.Controls.ViewZones;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls;

public sealed partial class LoomEditor
{
    private readonly HashSet<string> _hostZoneIds = [];
    private readonly HostViewZoneLayer _hostZoneLayer;

    /// <summary>
    /// Opens a gap after a line and puts a control in it (Monaco: <c>changeViewZones</c> + <c>addZone</c>). The zone follows edits like any view zone. The
    /// content is sized to the zone (the width of the text area, the height of the zone) and receives its own pointer and keyboard input. Without the template
    /// part <c>PART_ZoneWidgetCanvas</c> the zone is an empty gap. The theme of the content is the host's job.
    /// </summary>
    /// <param name="afterLineNumber">The zone shows after this model line; 0 is before the first line.</param>
    /// <param name="height">The height of the zone in DIPs, rounded up to a whole pixel.</param>
    /// <param name="content">The control shown in the zone, or <see langword="null"/> for an empty gap.</param>
    /// <returns>The id of the zone, for <see cref="RemoveViewZone"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="afterLineNumber"/> is outside <c>0 to line count</c>, or <paramref name="height"/> is not finite or not positive.</exception>
    /// <remarks>Threading: UI thread only.</remarks>
    public string AddViewZone(int afterLineNumber, double height, Control? content = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(afterLineNumber, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(afterLineNumber, _context.Model.LineCount);
        CheckZoneHeight(height);

        string id = string.Empty;
        _viewModel.ViewZones.Change(accessor => id = accessor.AddZone(new ViewZone
        {
            AfterLineNumber = afterLineNumber,
            HeightInPx = (int)Math.Ceiling(height),
            Kind = ViewZoneKind.Widget,
            SuppressMouseDown = true,
        }));
        _hostZoneIds.Add(id);
        if (content is not null)
        {
            _hostZoneLayer.Add(id, content);
        }

        Invalidate();
        return id;
    }

    /// <summary>
    /// Removes a zone added with <see cref="AddViewZone"/>. When the focus is inside its content, the editor takes it first.
    /// </summary>
    /// <param name="id">The id returned by <see cref="AddViewZone"/>.</param>
    /// <returns><see langword="false"/> when the id was not returned by <see cref="AddViewZone"/> or was already removed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <remarks>Threading: UI thread only.</remarks>
    public bool RemoveViewZone(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (!_hostZoneIds.Remove(id))
        {
            return false;
        }

        // Before the control leaves the tree: a removed focused control would leave the focus on the window.
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is Visual focused && _hostZoneLayer.HasFocusWithin(id, focused))
        {
            Focus();
        }

        _hostZoneLayer.Remove(id);
        _viewModel.ViewZones.Change(accessor => accessor.RemoveZone(id));
        Invalidate();
        return true;
    }

    /// <summary>
    /// Resizes a zone added with <see cref="AddViewZone"/>. Its control, and the focus inside it, stay as they are: only the gap and the size of the control change.
    /// </summary>
    /// <param name="id">The id returned by <see cref="AddViewZone"/>.</param>
    /// <param name="height">The new height of the zone in DIPs, rounded up to a whole pixel.</param>
    /// <returns><see langword="false"/> when the id was not returned by <see cref="AddViewZone"/> or was already removed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="height"/> is not finite or not positive.</exception>
    /// <remarks>Threading: UI thread only.</remarks>
    public bool SetViewZoneHeight(string id, double height)
    {
        ArgumentNullException.ThrowIfNull(id);
        CheckZoneHeight(height);

        if (!_hostZoneIds.Contains(id) || _viewModel.ViewZones.GetZone(id) is not { } zone)
        {
            return false;
        }

        zone.HeightInPx = (int)Math.Ceiling(height);
        _viewModel.ViewZones.Change(accessor => accessor.LayoutZone(id));
        Invalidate();
        return true;
    }

    private static void CheckZoneHeight(double height)
    {
        if (!double.IsFinite(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "The height must be finite and greater than 0.");
        }
    }
}
