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

    private static string LongLine() => string.Concat(Enumerable.Repeat("aaaaaaaaa ", 30));

    private static TestCodeEditorOptions ForcedOptions(AccessibilitySupport setting) => new(
        Wrapping: new EditorWrappingOptions { WordWrap = WordWrap.Off },
        Accessibility: new EditorAccessibilityOptions { Setting = setting });

    [Fact]
    public void SetIsDominatedByLongLines_InScreenReaderMode_WrapsTheLines() =>
        TestCodeEditor.WithTestCodeEditor(LongLine(), editor =>
        {
            ViewModel viewModel = editor.ViewModel;
            Assert.Equal(1, viewModel.GetLineCount());
            viewModel.SetIsDominatedByLongLines(true);
            Assert.True(viewModel.IsDominatedByLongLines);
            Assert.True(viewModel.GetLineCount() > 1);
            viewModel.SetIsDominatedByLongLines(false);
            Assert.Equal(1, viewModel.GetLineCount());
        }, ForcedOptions(AccessibilitySupport.Enabled));

    [Fact]
    public void SetIsDominatedByLongLines_SettingDisabled_DoesNotWrap() =>
        TestCodeEditor.WithTestCodeEditor(LongLine(), editor =>
        {
            editor.ViewModel.SetIsDominatedByLongLines(true);
            Assert.Equal(1, editor.ViewModel.GetLineCount());
        }, ForcedOptions(AccessibilitySupport.Disabled));

    [Fact]
    public void SetIsDominatedByLongLines_SameValue_DoesNotRecompute() =>
        TestCodeEditor.WithTestCodeEditor(LongLine(), editor =>
        {
            ViewModel viewModel = editor.ViewModel;
            viewModel.SetIsDominatedByLongLines(true);
            int count = 0;
            viewModel.LayoutInfoChanged += (_, _) => count++;
            viewModel.SetIsDominatedByLongLines(true);
            Assert.Equal(0, count);
        }, ForcedOptions(AccessibilitySupport.Enabled));
}
