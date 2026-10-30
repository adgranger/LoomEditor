using LoomEditor.View.Layout;
using LoomEditor.WinUI.Controls.ViewZones;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace LoomEditor.WinUI.Controls;

public sealed partial class LoomEditor
{
    private readonly HashSet<string> _hostZoneIds = [];
    private readonly HostViewZoneLayer _hostZoneLayer;

    /// <summary>
    /// Opens a gap after a line and puts an element in it (Monaco: <c>changeViewZones</c> + <c>addZone</c>). The zone follows edits like any view zone. The
    /// content is sized to the zone (the width of the text area, the height of the zone) and receives its own pointer and keyboard input. Without the template
    /// part <c>PART_ZoneWidgetCanvas</c> the zone is an empty gap. The theme of the content is the host's job. On a side of a <see cref="LoomDiffEditor"/> the
    /// zone is one more zone of the side: its height counts as extra height when the diff aligns the two sides.
    /// </summary>
    /// <param name="afterLineNumber">The zone shows after this model line; 0 is before the first line.</param>
    /// <param name="height">The height of the zone in DIPs, rounded up to a whole pixel.</param>
    /// <param name="content">The element shown in the zone, or <see langword="null"/> for an empty gap.</param>
    /// <returns>The id of the zone, for <see cref="RemoveViewZone"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="afterLineNumber"/> is outside <c>0 … line count</c>, or <paramref name="height"/> is not finite or not positive.</exception>
    /// <remarks>Threading: UI thread only.</remarks>
    public string AddViewZone(int afterLineNumber, double height, FrameworkElement? content = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(afterLineNumber, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(afterLineNumber, _context.Model.LineCount);
        if (!double.IsFinite(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "The height must be finite and greater than 0.");
        }

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

        InvalidateCanvas();
        return id;
    }

    /// <summary>
    /// Removes a zone added with <see cref="AddViewZone"/>. When the focus is inside its content, the editor takes it first.
    /// </summary>
    /// <param name="id">The id returned by <see cref="AddViewZone"/>.</param>
    /// <returns><see langword="false"/> when the id was not returned by <see cref="AddViewZone"/> (comment and diff zones cannot be removed here) or was already removed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <remarks>Threading: UI thread only.</remarks>
    public bool RemoveViewZone(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (!_hostZoneIds.Remove(id))
        {
            return false;
        }

        if (XamlRoot is not null
            && _hostZoneLayer.HasFocusWithin(id, FocusManager.GetFocusedElement(XamlRoot) as DependencyObject))
        {
            // Before the element leaves the tree: a removed focused element would leave the focus on the window.
            Focus(FocusState.Programmatic);
        }

        _hostZoneLayer.Remove(id);
        _viewModel.ViewZones.Change(accessor => accessor.RemoveZone(id));
        InvalidateCanvas();
        return true;
    }

    /// <summary>
    /// Resizes a zone added with <see cref="AddViewZone"/>. Its element, and the focus inside it, stay as they are: only the gap and the size of the element change.
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
        if (!double.IsFinite(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "The height must be finite and greater than 0.");
        }

        if (!_hostZoneIds.Contains(id) || _viewModel.ViewZones.GetZone(id) is not { } zone)
        {
            return false;
        }

        zone.HeightInPx = (int)Math.Ceiling(height);
        _viewModel.ViewZones.Change(accessor => accessor.LayoutZone(id));
        InvalidateCanvas();
        return true;
    }
}
