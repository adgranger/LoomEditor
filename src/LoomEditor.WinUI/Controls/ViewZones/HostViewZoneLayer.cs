using LoomEditor.View.Layout;
using LoomEditor.WinUI.Controls.Overlay;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.ViewZones;

/// <summary>Keeps the elements of the host view zones over their zones, in the zone-widget layer shared with the comment threads.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class HostViewZoneLayer(LoomEditor editor)
{
    private readonly Dictionary<string, FrameworkElement> _elements = [];
    private readonly HashSet<string> _seen = [];
    private OverlayHost? _layer;

    /// <summary>Moves every element to a new layer (the template was applied again), or to none.</summary>
    public void Attach(OverlayHost? layer)
    {
        foreach (FrameworkElement element in _elements.Values)
        {
            _layer?.Remove(element);
        }

        _layer = layer;
        foreach (FrameworkElement element in _elements.Values)
        {
            element.Visibility = Visibility.Collapsed;
            _layer?.Show(element, new OverlayRect(0, 0, 0, 0));
            element.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>Adds an element for a zone; it stays collapsed until the next <see cref="Arrange"/>.</summary>
    public void Add(string id, FrameworkElement element)
    {
        element.Visibility = Visibility.Collapsed;
        _elements[id] = element;
        _layer?.Show(element, new OverlayRect(0, 0, 0, 0));
        element.Visibility = Visibility.Collapsed;
    }

    /// <summary>True when the zone has an element.</summary>
    public bool Has(string id) => _elements.ContainsKey(id);

    /// <summary>True when the focused element is inside the element of the zone.</summary>
    public bool HasFocusWithin(string id, DependencyObject? focused) => _elements.TryGetValue(id, out FrameworkElement? element) && IsInside(focused, element);

    /// <summary>Removes the element of a zone from the layer.</summary>
    public void Remove(string id)
    {
        if (_elements.Remove(id, out FrameworkElement? element))
        {
            _layer?.Remove(element);
        }
    }

    /// <summary>True when the object is one of the elements or inside one.</summary>
    public bool Contains(DependencyObject? d)
    {
        if (_layer is null || !_layer.Contains(d))
        {
            return false;
        }

        foreach (FrameworkElement element in _elements.Values)
        {
            if (IsInside(d, element))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Puts every element over its zone, or collapses it when its zone is not in the viewport. Called right after the text is drawn.</summary>
    public void Arrange()
    {
        if (_layer is null || _elements.Count == 0)
        {
            return;
        }

        _seen.Clear();
        EditorLayoutInfo layout = editor.LayoutInfo;
        double scrollTop = editor.ScrollTop;
        foreach (ViewWhitespaceViewportData zone in editor.GetWhitespaceViewportData())
        {
            if (zone.Height < 1 || !_elements.TryGetValue(zone.Id, out FrameworkElement? element))
            {
                continue;
            }

            _seen.Add(zone.Id);
            double width = Math.Max(0, layout.ContentWidth);
            element.Width = width;
            element.Height = zone.Height;
            _layer.Show(element, new OverlayRect(layout.ContentLeft, zone.VerticalOffset - scrollTop, width, zone.Height));
        }

        foreach ((string id, FrameworkElement element) in _elements)
        {
            if (!_seen.Contains(id) && element.Visibility != Visibility.Collapsed)
            {
                OverlayHost.Hide(element);
            }
        }
    }

    private static bool IsInside(DependencyObject? d, FrameworkElement element)
    {
        while (d is not null)
        {
            if (ReferenceEquals(d, element))
            {
                return true;
            }

            d = VisualTreeHelper.GetParent(d);
        }

        return false;
    }
}
