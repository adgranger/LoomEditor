using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LoomEditor.Avalonia.Controls.Overlay;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls.ViewZones;

/// <summary>Keeps the controls of the host view zones over their zones, in the zone-widget layer shared with the comment threads.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class HostViewZoneLayer(LoomEditor editor)
{
    private readonly Dictionary<string, Control> _elements = [];
    private readonly HashSet<string> _seen = [];
    private OverlayHost? _layer;
    private bool _arrangePending;

    /// <summary>Moves every control to a new layer (the template was applied again), or to none.</summary>
    public void Attach(OverlayHost? layer)
    {
        foreach (Control element in _elements.Values)
        {
            _layer?.Remove(element);
        }

        _layer = layer;
        foreach (Control element in _elements.Values)
        {
            Park(element);
        }
    }

    /// <summary>Adds a control for a zone; it stays hidden until the next <see cref="Arrange"/>.</summary>
    public void Add(string id, Control element)
    {
        _elements[id] = element;
        Park(element);
    }

    /// <summary>True when the focused visual is inside the control of the zone.</summary>
    public bool HasFocusWithin(string id, Visual? focused) =>
        focused is not null && _elements.TryGetValue(id, out Control? element) && (ReferenceEquals(focused, element) || element.IsVisualAncestorOf(focused));

    /// <summary>Removes the control of a zone from the layer.</summary>
    public void Remove(string id)
    {
        if (_elements.Remove(id, out Control? element))
        {
            _layer?.Remove(element);
        }
    }

    /// <summary>Arranges the controls right after the current frame: Avalonia refuses a layout change during the render pass.</summary>
    public void ArrangeAfterDraw()
    {
        if (_arrangePending || _elements.Count == 0)
        {
            return;
        }

        _arrangePending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _arrangePending = false;
            Arrange();
        });
    }

    /// <summary>Puts every control over its zone, or hides it when its zone is not in the viewport.</summary>
    public void Arrange()
    {
        if (_layer is null || _elements.Count == 0)
        {
            return;
        }

        _seen.Clear();
        EditorLayoutInfo layout = editor.ViewModel.LayoutInfo;
        double scrollTop = editor.ScrollTop;
        foreach (ViewWhitespaceViewportData zone in editor.ViewModel.ViewLayout.GetWhitespaceViewportData())
        {
            if (zone.Height < 1 || !_elements.TryGetValue(zone.Id, out Control? element))
            {
                continue;
            }

            _seen.Add(zone.Id);
            double width = Math.Max(0, layout.ContentWidth);
            element.Width = width;
            element.Height = zone.Height;
            _layer.Show(element, new OverlayRect(layout.ContentLeft, zone.VerticalOffset - scrollTop, width, zone.Height));
        }

        foreach ((string id, Control element) in _elements)
        {
            if (!_seen.Contains(id) && element.IsVisible)
            {
                OverlayHost.Hide(element);
            }
        }
    }

    private void Park(Control element)
    {
        _layer?.Show(element, new OverlayRect(0, 0, 0, 0));
        element.IsVisible = false;
    }
}
