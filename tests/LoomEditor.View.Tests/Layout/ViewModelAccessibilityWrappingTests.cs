using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

public sealed class ViewModelAccessibilityWrappingTests
{
    private const string Text = "    aaaa bbbb cccc";

    private static TestCodeEditorOptions Options(AccessibilitySupport setting) => new(
        Wrapping: new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 10, WrappingIndent = WrappingIndent.Same },
        Accessibility: new EditorAccessibilityOptions { Setting = setting });

    private static string[] ViewLines(ViewModel viewModel) =>
        [.. Enumerable.Range(1, viewModel.GetLineCount()).Select(viewModel.GetLineContent)];

    [Fact]
    public void Wrapped_SettingDisabled_ContinuationLinesKeepTheIndent() =>
        TestCodeEditor.WithTestCodeEditor(Text, editor =>
            Assert.Equal(["    aaaa ", "    bbbb ", "    cccc"], ViewLines(editor.ViewModel)), Options(AccessibilitySupport.Disabled));

    [Fact]
    public void Wrapped_SettingEnabled_ContinuationLinesHaveNoIndent() =>
        TestCodeEditor.WithTestCodeEditor(Text, editor =>
            Assert.Equal(["    aaaa ", "bbbb cccc"], ViewLines(editor.ViewModel)), Options(AccessibilitySupport.Enabled));

    [Fact]
    public void SetOptions_TogglingAccessibility_RewrapsTheLines() =>
        TestCodeEditor.WithTestCodeEditor(Text, editor =>
        {
            ViewModel viewModel = editor.ViewModel;
            Assert.Equal(3, viewModel.GetLineCount());
            viewModel.SetOptions(
                viewModel.LayoutOptions with { Accessibility = new EditorAccessibilityOptions { Setting = AccessibilitySupport.Enabled } },
                viewModel.ViewLayoutOptions);
            Assert.Equal(2, viewModel.GetLineCount());
        }, Options(AccessibilitySupport.Disabled));
}
