using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no widget zones (comment threads, ADR 0014): all tests are new.
public class TextViewRendererWidgetZoneTests
{
    private const string Text = "L1\nL2\nL3\nL4";

    private static void AddWidgetZone(ViewHarness harness, int afterLine, int height) =>
        harness.ViewModel.ViewZones.Change(accessor =>
            accessor.AddZone(new ViewZone { AfterLineNumber = afterLine, HeightInPx = height, Kind = ViewZoneKind.Widget }));

    [Fact]
    public void WidgetZone_DrawsNothing()
    {
        using var plain = new ViewHarness(Text);
        using var withZone = new ViewHarness(Text);
        AddWidgetZone(withZone, 2, 50);

        int before = plain.Render().Calls.Count;

        Assert.Equal(before, withZone.Render().Calls.Count);
        Assert.DoesNotContain(withZone.Render().Calls, call => call is DrawImageCall { Opacity: 1f });
    }

    [Fact]
    public void WidgetZone_ReservesHeight()
    {
        using var harness = new ViewHarness(Text);
        int lineHeight = harness.Measurer.FontInfo.LineHeight;
        AddWidgetZone(harness, 2, 50);

        Assert.Equal(2 * lineHeight + 50, harness.ViewModel.ViewLayout.GetVerticalOffsetForLineNumber(3));
    }
}
