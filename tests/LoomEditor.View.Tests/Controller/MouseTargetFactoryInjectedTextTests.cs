using LoomEditor.Core.Decorations;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Controller;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Controller;

// Monaco has no test for the hit test or the viewport inline decorations of injected text (they need a DOM): all tests are new.
public class MouseTargetFactoryInjectedTextTests
{
    // "hello" with "XYZ" injected after "he": the view line is "heXYZllo" (view columns 3 to 5 are the injected text).
    private static readonly TestCodeEditorOptions Options = new(EditorId: 1);

    private sealed class FakeLineHitTester(TestCodeEditor editor) : ILineHitTester
    {
        public double GetLineWidth(int lineNumber) => editor.ViewModel.GetLineLength(lineNumber) * 10;

        public Position? GetPositionAtCoordinate(int lineNumber, double mouseContentHorizontalOffset)
        {
            int index = (int)Math.Floor((mouseContentHorizontalOffset / 10) + 0.5);
            return new Position(lineNumber, Math.Clamp(index, 0, editor.ViewModel.GetLineLength(lineNumber)) + 1);
        }
    }

    private static void WithInjectedText(Action<TestCodeEditor> callback) =>
        TestCodeEditor.WithTestCodeEditor("hello", editor =>
        {
            editor.Model.ChangeDecorations(1, accessor => accessor.AddDecoration(
                new Range(1, 3, 1, 3),
                ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
                {
                    ShowIfCollapsed = true,
                    Description = "test",
                    After = new InjectedTextOptions { Content = "XYZ", InlineClassName = "findMatchInline" },
                })));
            callback(editor);
        }, Options);

    private static MouseTarget HitTest(TestCodeEditor editor, double contentX) =>
        MouseTargetFactory.CreateMouseTarget(
            editor.ViewModel, new FakeLineHitTester(editor), editor.ViewModel.LayoutInfo.ContentLeft + contentX, 3);

    [Fact]
    public void HitTest_InsideInjectedText_NormalizesAndReportsIt()
    {
        WithInjectedText(editor =>
        {
            Assert.Equal("heXYZllo", editor.ViewModel.GetLineContent(1));

            MouseTarget target = HitTest(editor, 40);

            Assert.Equal(MouseTargetType.ContentText, target.Type);
            Assert.Equal("XYZ", target.InjectedText!.Options.Content);
            Assert.NotEqual(5, target.Position!.Value.Column);
            Assert.Equal(Range.Collapsed(target.Position.Value), target.Range);
        });
    }

    [Fact]
    public void HitTest_OnModelText_HasNoInjectedText()
    {
        WithInjectedText(editor =>
        {
            MouseTarget target = HitTest(editor, 70);

            Assert.Equal(MouseTargetType.ContentText, target.Type);
            Assert.Null(target.InjectedText);
            Assert.Equal(new Position(1, 8), target.Position);
        });
    }

    [Fact]
    public void DoubleClickOnInjectedText_DoesNotSelectAWord()
    {
        WithInjectedText(editor =>
        {
            var controller = new ViewController(editor.ViewModel);

            controller.DispatchMouse(new MouseDispatchData(
                new Position(1, 5), 5, NavigationCommandRevealType.Regular, false, false, 2, false, false, false, false, true, false,
                OnInjectedText: true));

            Assert.True(editor.ViewModel.GetSelection().IsEmpty);
        });
    }

    [Fact]
    public void ViewportInlineDecorations_IncludeTheInjectedTextOnes()
    {
        WithInjectedText(editor =>
        {
            var range = new Range(1, 1, 1, editor.ViewModel.GetLineMaxColumn(1));

            IReadOnlyList<InlineDecoration> decorations = editor.ViewModel.GetViewportInlineDecorations(range, 1);

            InlineDecoration injected = Assert.Single(decorations, d => d.InlineClassName == "findMatchInline");
            Assert.Equal(new Range(1, 3, 1, 6), injected.Range);
        });
    }
}
