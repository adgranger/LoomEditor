using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Features.SelectionAnchor;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Accessibility;

// The twin of the WinUI accessibility (no upstream test, all tests are new).
public class LoomEditorAutomationPeerTests
{
    [Fact]
    public async Task Peer_ControlType_IsEdit()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor();
            Window window = EditorWindow.Open(editor);

            AutomationPeer peer = ControlAutomationPeer.CreatePeerForElement(editor);

            Assert.Equal(AutomationControlType.Edit, peer.GetAutomationControlType());
            Assert.Equal("Code editor", peer.GetName());
            AutomationProperties.SetName(editor, "Script");
            Assert.Equal("Script", peer.GetName());
            window.Close();
        });
    }

    [Fact]
    public async Task Peer_Value_IsModelText()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = "one\r\ntwo" };
            Window window = EditorWindow.Open(editor);

            var value = Assert.IsAssignableFrom<IValueProvider>(ControlAutomationPeer.CreatePeerForElement(editor));

            Assert.Equal("one\ntwo", value.Value);
            Assert.False(value.IsReadOnly);
            value.SetValue("three");
            Assert.Equal("three", editor.Model.GetValue());
            window.Close();
        });
    }

    [Fact]
    public async Task Peer_SetValue_ReadOnly_Throws()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = "abc", IsReadOnly = true };
            Window window = EditorWindow.Open(editor);

            var value = Assert.IsAssignableFrom<IValueProvider>(ControlAutomationPeer.CreatePeerForElement(editor));

            Assert.True(value.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => value.SetValue("x"));
            Assert.Equal("abc", editor.Model.GetValue());
            window.Close();
        });
    }

    [Fact]
    public async Task Announcement_SetsLiveRegionText()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = "1\n2\n3\n4\nfive" };
            Window window = EditorWindow.Open(editor);
            editor.SetSelections([Selection.Collapsed(new Position(5, 3))]);

            // There is no "go to line" announcement in WinUI: setting the selection anchor on line 5 announces its position.
            editor.Commands.Execute(SelectionAnchorCommands.SetSelectionAnchor);

            TextBlock liveRegion = editor.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Name == "PART_LiveRegion");
            Assert.Equal(AutomationLiveSetting.Assertive, AutomationProperties.GetLiveSetting(liveRegion));
            Assert.Equal(AccessibilityMessages.AnchorSet(5, 3), liveRegion.Text);
            window.Close();
        });
    }

    [Fact]
    public async Task HighContrast_On_UsesHighContrastTheme()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor();
            Window window = EditorWindow.Open(editor);
            Assert.NotNull(editor.SystemHighContrast);
            Assert.Equal(0, CountBlack(EditorWindow.Capture(window)));

            editor.SystemHighContrast.Update(ColorValues(ColorContrastPreference.High));
            Dispatcher.UIThread.RunJobs();

            // The dark high contrast scheme has a black background (the dark theme's is not).
            Assert.True(CountBlack(EditorWindow.Capture(window)) > EditorWindow.Width * EditorWindow.Height / 2);

            editor.SystemHighContrast.Update(ColorValues(ColorContrastPreference.NoPreference));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, CountBlack(EditorWindow.Capture(window)));
            window.Close();
        });
    }

    private static int CountBlack(uint[] pixels) => pixels.Count(pixel => pixel == Colors.Black.ToUInt32());

    private static PlatformColorValues ColorValues(ColorContrastPreference contrast) => new()
    {
        ThemeVariant = PlatformThemeVariant.Dark,
        ContrastPreference = contrast,
        AccentColor1 = Colors.Cyan,
    };
}
