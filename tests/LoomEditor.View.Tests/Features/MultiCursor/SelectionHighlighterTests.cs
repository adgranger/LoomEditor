// Ported from microsoft/vscode: src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts (suite 'Selected text occurrence matching highlights')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Features.MultiCursor;
using LoomEditor.View.Tests.Features.Find.TestHelpers;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Features.MultiCursor.TestHelpers.MultiCursorTestHelpers;

namespace LoomEditor.View.Tests.Features.MultiCursor;

public class SelectionHighlighterTests
{
    private static readonly string[] Text = ["foo", "FOO", "fooBar", "FOOBar"];

    private static void WithHighlighter(
        string[] text,
        Action<TestCodeEditor, FindController, MultiCursorSelectionController, ManualEditorScheduler> callback,
        bool hasTextFocus = true,
        SelectedTextMatchMode matchMode = SelectedTextMatchMode.FindOptions,
        MultiCursorOptions? options = null) =>
        TestCodeEditor.WithTestCodeEditor(text, editor =>
        {
            var scheduler = new ManualEditorScheduler();
            using var findController = new FindController(editor.ViewModel, scheduler) { Widget = new TestFindWidget() };
            MultiCursorOptions multiCursorOptions = options ?? new MultiCursorOptions(matchMode);
            using var controller = new MultiCursorSelectionController(editor.ViewModel, multiCursorOptions);
            var decorations = new EditorDecorationsCollection(editor.Model, 1);
            using var highlighter = new SelectionHighlighter(editor.ViewModel, controller, findController, decorations, scheduler, multiCursorOptions);

            callback(editor, findController, controller, scheduler);
        }, new TestCodeEditorOptions(HasTextFocus: hasTextFocus));

    // Monaco: highlights(editor)
    private static int[][] Highlights(TestCodeEditor editor) =>
        [.. editor.Model.GetAllDecorations()
            .Where(decoration => decoration.Options.ClassName == "selectionHighlight")
            .Select(decoration => decoration.Range)
            .OrderBy(range => range, Comparer<Range>.Create(Range.CompareUsingStarts))
            .Select(range => new[] { range.StartLineNumber, range.StartColumn, range.EndLineNumber, range.EndColumn })];

    private static int[][] Lines(params int[] lines) => [.. lines.Select(line => new[] { line, 1, line, 4 })];

    // Monaco: assertSelectionHighlighting
    private static void AssertSelectionHighlighting(SelectedTextMatchMode mode, bool hasTextFocus, int[] expectedLines) =>
        WithHighlighter(Text, (editor, find, _, _) =>
        {
            find.State.Change(
                new NewFindReplaceState { SearchString = "f.*", IsRevealed = true, MatchCase = mode != SelectedTextMatchMode.CaseSensitive, WholeWord = true, IsRegex = true },
                false);
            Select(editor, new Selection(1, 1, 1, 4));

            Assert.Equal(Lines(expectedLines), Highlights(editor));
        }, hasTextFocus, mode);

    // Monaco: assertDuplicateFindHighlights
    private static void AssertDuplicateFindHighlights(SelectedTextMatchMode mode, int[] expectedDifferentCaseLines) =>
        WithHighlighter(Text, (editor, find, _, _) =>
        {
            Select(editor, new Selection(1, 1, 1, 4));
            bool matchCase = mode == SelectedTextMatchMode.CaseSensitive;
            find.State.Change(new NewFindReplaceState { SearchString = "foo", IsRevealed = true, MatchCase = matchCase, WholeWord = false }, false);
            int[][] sameOptions = Highlights(editor);

            find.State.Change(new NewFindReplaceState { MatchCase = !matchCase }, false);

            Assert.Empty(sameOptions);
            Assert.Equal(Lines(expectedDifferentCaseLines), Highlights(editor));
        }, matchMode: mode);

    // Monaco: 'selection highlighting uses match case false, editor focused true'
    [Fact]
    public void SelectionHighlighting_UsesMatchCaseFalse_EditorFocusedTrue() =>
        AssertSelectionHighlighting(SelectedTextMatchMode.CaseInsensitive, true, [2, 3, 4]);

    // Monaco: 'only suppress duplicate Find highlights with the same matching rules, match case false'
    [Fact]
    public void OnlySuppressDuplicateFindHighlightsWithTheSameMatchingRules_MatchCaseFalse() =>
        AssertDuplicateFindHighlights(SelectedTextMatchMode.CaseInsensitive, [2, 3, 4]);

    // Monaco: 'selection highlighting uses match case true, editor focused true'
    [Fact]
    public void SelectionHighlighting_UsesMatchCaseTrue_EditorFocusedTrue() =>
        AssertSelectionHighlighting(SelectedTextMatchMode.CaseSensitive, true, [3]);

    // Monaco: 'only suppress duplicate Find highlights with the same matching rules, match case true'
    [Fact]
    public void OnlySuppressDuplicateFindHighlightsWithTheSameMatchingRules_MatchCaseTrue() =>
        AssertDuplicateFindHighlights(SelectedTextMatchMode.CaseSensitive, [3]);

    // Monaco: 'selection highlighting refreshes on case-only configuration changes without cursor movement'
    [Fact]
    public void SelectionHighlighting_RefreshesOnCaseOnlyConfigurationChangesWithoutCursorMovement() =>
        WithHighlighter(Text, (editor, _, controller, scheduler) =>
        {
            Select(editor, new Selection(1, 1, 1, 4));
            // Drain the initial scheduled update so it cannot mask missing configuration invalidation.
            scheduler.RunAll();
            List<int[][]> actual = [Highlights(editor)];

            controller.SetOptions(new MultiCursorOptions(SelectedTextMatchMode.CaseSensitive));
            scheduler.RunAll();
            actual.Add(Highlights(editor));

            controller.SetOptions(new MultiCursorOptions(SelectedTextMatchMode.CaseInsensitive));
            scheduler.RunAll();
            actual.Add(Highlights(editor));

            Assert.Equal<int[][]>([Lines(2, 3, 4), Lines(3), Lines(2, 3, 4)], actual);
        });

    // Monaco: 'Find regex select-all discards an existing selection-driven session'
    [Fact]
    public void FindRegexSelectAll_DiscardsAnExistingSelectionDrivenSession() =>
        WithHighlighter(["foo", "fooBar", "bar", "barista"], (editor, find, controller, _) =>
        {
            Select(editor, new Selection(1, 1, 1, 4));
            controller.AddSelectionToNextFindMatch(find);

            find.State.Change(new NewFindReplaceState { SearchString = "bar$", IsRevealed = true, IsRegex = true, MatchCase = true }, false);
            SetTextFocus(editor, false);
            controller.SelectAll(find);
            find.State.Change(new NewFindReplaceState { IsRevealed = false }, false);
            int[][] afterFind = Highlights(editor);

            SetTextFocus(editor, true);
            controller.AddSelectionToNextFindMatch(find);

            Assert.Equal([[2, 4, 2, 7], [4, 1, 4, 4]], afterFind);
            Assert.Equal([[3, 1, 3, 4], [4, 1, 4, 4]], Selections(editor));
        }, matchMode: SelectedTextMatchMode.CaseInsensitive);

    [Fact]
    public void EmptySelection_NoHighlights() => WithHighlighter(Text, (editor, _, _, scheduler) =>
    {
        Select(editor, new Selection(1, 1, 1, 4));
        Assert.NotEmpty(Highlights(editor));

        Select(editor, new Selection(1, 2, 1, 2));
        scheduler.RunAll();

        Assert.Empty(Highlights(editor));
    });

    [Fact]
    public void WhitespaceOnlySelection_NoHighlights() => WithHighlighter(["a  b", "c  d"], (editor, _, _, _) =>
    {
        Select(editor, new Selection(1, 2, 1, 4));

        Assert.Empty(Highlights(editor));
    });

    [Fact]
    public void SelectionLongerThan200_NoHighlights()
    {
        string line = new('x', 201);

        WithHighlighter([line, line], (editor, _, _, _) =>
        {
            Select(editor, new Selection(1, 1, 1, 202));

            Assert.Empty(Highlights(editor));
        });
    }

    [Fact]
    public void MultilineSelection_OptionOff_NoHighlights() => WithHighlighter(["foo", "bar", "foo", "bar"], (editor, _, _, _) =>
    {
        Select(editor, new Selection(1, 1, 2, 4));

        Assert.Empty(Highlights(editor));
    });

    [Fact]
    public void HighlightsDoNotOverlapSelections() => WithHighlighter(["foo foo", "foo"], (editor, _, _, _) =>
    {
        Select(editor, new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4));

        Assert.Equal([[1, 5, 1, 8]], Highlights(editor));
    });

    [Fact]
    public void Dispose_ClearsDecorations()
    {
        int[][]? highlightsAfterDispose = null;
        TestCodeEditor.WithTestCodeEditor(Text, editor =>
        {
            var scheduler = new ManualEditorScheduler();
            using var findController = new FindController(editor.ViewModel, scheduler);
            var options = MultiCursorOptions.Default;
            using var controller = new MultiCursorSelectionController(editor.ViewModel, options);
            var highlighter = new SelectionHighlighter(editor.ViewModel, controller, findController, new EditorDecorationsCollection(editor.Model, 1), scheduler, options);
            Select(editor, new Selection(1, 1, 1, 4));
            Assert.NotEmpty(Highlights(editor));

            highlighter.Dispose();

            highlightsAfterDispose = Highlights(editor);
        });

        Assert.Empty(highlightsAfterDispose!);
    }

    [Fact]
    public void Attach_WithFindFeature_HighlightsTheOtherOccurrences()
    {
        var context = new EditorContext();
        context.SetText("foo bar foo");
        using var host = new EditorFeatureHost(context, [() => new FindFeature(), () => new MultiCursorFeature()]);

        context.Selection = new Selection(1, 1, 1, 4);

        Assert.Single(context.Model.GetAllDecorations(), d => d.Options.ClassName == "selectionHighlight");
    }
}
