using LoomEditor.View.Features.StickyScroll;

namespace LoomEditor.View.Layout;

internal sealed partial class ViewModel
{
    /// <summary>The sticky lines to draw, published by the sticky scroll feature; <see langword="null"/> when disabled or empty.</summary>
    public StickyScrollRenderData? StickyScroll { get; set; }
}
