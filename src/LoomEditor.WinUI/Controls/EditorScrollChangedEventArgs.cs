namespace LoomEditor.WinUI.Controls;

/// <summary>The scroll change reported by <see cref="LoomEditor.ScrollChanged"/> (Monaco: <c>IScrollEvent</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
public sealed class EditorScrollChangedEventArgs : EventArgs
{
    internal EditorScrollChangedEventArgs(bool scrollTopChanged, bool scrollLeftChanged, bool scrollHeightChanged, double scrollTop, double scrollLeft)
    {
        ScrollTopChanged = scrollTopChanged;
        ScrollLeftChanged = scrollLeftChanged;
        ScrollHeightChanged = scrollHeightChanged;
        ScrollTop = scrollTop;
        ScrollLeft = scrollLeft;
    }

    /// <summary>Whether the vertical position changed.</summary>
    public bool ScrollTopChanged { get; }

    /// <summary>Whether the horizontal position changed.</summary>
    public bool ScrollLeftChanged { get; }

    /// <summary>Whether the scroll height changed.</summary>
    public bool ScrollHeightChanged { get; }

    /// <summary>The vertical position, in DIPs.</summary>
    public double ScrollTop { get; }

    /// <summary>The horizontal position, in DIPs.</summary>
    public double ScrollLeft { get; }
}
