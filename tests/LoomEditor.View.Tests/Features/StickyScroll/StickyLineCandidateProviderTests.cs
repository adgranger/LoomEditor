using LoomEditor.View.Features.StickyScroll;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.StickyScroll;

public class StickyLineCandidateProviderTests
{
    // The text of Monaco's 'Testing the function getCandidateStickyLinesIntersecting', read with the indentation model.
    private const string Text = "class A {\n  void f() {\n    a();\n    b();\n  }\n  void g() {\n    c();\n  }\n}\nlast";

    private static void With(Action<TestCodeEditor, StickyLineCandidateProvider, ManualEditorScheduler> test)
    {
        TestCodeEditor.WithTestCodeEditor(Text, editor =>
        {
            var scheduler = new ManualEditorScheduler();
            using var provider = new StickyLineCandidateProvider(editor.Model, editor.ViewModel, scheduler, () => new EditorStickyScrollOptions());
            test(editor, provider, scheduler);
        });
    }

    [Fact]
    public void GetCandidates_FirstBlock()
    {
        With((_, provider, _) =>
        {
            IReadOnlyList<StickyLineCandidate> candidates = provider.GetCandidateStickyLinesIntersecting(new StickyRange(1, 3));

            Assert.True(candidates.Count >= 2);
            Assert.Equal(1, candidates[0].StartLineNumber);
            Assert.Equal(2, candidates[1].StartLineNumber);
        });
    }

    [Fact]
    public void GetCandidates_NestedBlocks_HaveIncreasingTops()
    {
        With((editor, provider, _) =>
        {
            IReadOnlyList<StickyLineCandidate> candidates = provider.GetCandidateStickyLinesIntersecting(new StickyRange(1, 3));

            Assert.Equal(0, candidates[0].Top);
            Assert.Equal(candidates[0].Height, candidates[1].Top);
            Assert.Equal(editor.ViewModel.ViewLayout.GetLineHeightForLineNumber(1), candidates[0].Height);
        });
    }

    // Monaco: 'issue #156268 - Do not reveal sticky lines when they are in a folded region '
    [Fact]
    public void GetCandidates_FoldedRegion_IsFilteredOut()
    {
        With((editor, provider, _) =>
        {
            Assert.Contains(provider.GetCandidateStickyLinesIntersecting(new StickyRange(1, 8)), c => c.StartLineNumber == 2);

            editor.ViewModel.SetHiddenAreas([new Range(2, 1, 5, 1)]);

            IReadOnlyList<StickyLineCandidate> candidates = provider.GetCandidateStickyLinesIntersecting(new StickyRange(1, 8));
            Assert.Contains(candidates, c => c.StartLineNumber == 6);
            Assert.DoesNotContain(candidates, c => c.StartLineNumber == 2);
        });
    }

    [Fact]
    public void ContentChange_UpdatesAfterTheDebounce()
    {
        With((editor, provider, scheduler) =>
        {
            int? version = provider.GetVersionId();
            int changes = 0;
            provider.StickyScrollChanged += (_, _) => changes++;

            editor.Model.SetValue(Text + "\n");
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(49));
            Assert.Equal(version, provider.GetVersionId());
            Assert.Equal(0, changes);

            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
            Assert.Equal(editor.Model.VersionId, provider.GetVersionId());
            Assert.Equal(1, changes);
        });
    }
}
