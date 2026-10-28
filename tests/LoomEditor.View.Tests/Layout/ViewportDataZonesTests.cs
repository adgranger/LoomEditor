using LoomEditor.Core.Text;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no equivalent: view zones are DOM nodes there.
public class ViewportDataZonesTests
{
    private const string Text = "a\nb\nc\nd\ne";

    private static ViewportData Data(ViewModel vm) => vm.GetViewportData([Selection.Collapsed(new Position(1, 1))]);

    [Fact]
    public void ZoneViewportData_ListsZonesInViewport() => TestCodeEditor.WithTestCodeEditor(Text, editor =>
    {
        ViewModel vm = editor.ViewModel;
        string id = "";
        vm.ViewZones.Change(a => id = a.AddZone(new ViewZone { AfterLineNumber = 2, HeightInPx = 33 }));

        ViewZoneViewportData zone = Assert.Single(Data(vm).ZoneViewportData);

        Assert.Equal(id, zone.Zone.Id);
        Assert.Equal(33, zone.Height);
        Assert.Equal(2 * vm.ViewLayoutOptions.LineHeight, zone.VerticalOffset);
    });

    [Fact]
    public void ZoneViewportData_NonZoneWhitespace_Ignored() => TestCodeEditor.WithTestCodeEditor(Text, editor =>
    {
        ViewModel vm = editor.ViewModel;
        vm.ViewLayout.ChangeWhitespace(a => a.InsertWhitespace(1, 0, 20, 0));

        ViewportData data = Data(vm);

        Assert.Single(data.WhitespaceViewportData);
        Assert.Empty(data.ZoneViewportData);
    });
}
