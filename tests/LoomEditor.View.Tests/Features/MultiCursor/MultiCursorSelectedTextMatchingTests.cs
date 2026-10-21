// Ported from microsoft/vscode: src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts (suite 'Selected text occurrence matching', without the highlight tests of P5-77)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Features.MultiCursor;
using static LoomEditor.View.Tests.Features.MultiCursor.TestHelpers.MultiCursorTestHelpers;

namespace LoomEditor.View.Tests.Features.MultiCursor;

// Not ported: 'a selection session does not survive replacing the editor model' (editor.setModel: one model per editor).
public class MultiCursorSelectedTextMatchingTests
{
    private static readonly string[] Text = ["foo", "FOObar", "foobar", "FOO", "foo"];

    public enum MultiCursorAction
    {
        AddSelectionToNext,
        AddSelectionToPrevious,
        MoveSelectionToNext,
        MoveSelectionToPrevious,
        SelectHighlights,
    }

    public enum Scenario
    {
        SelectedTextMatchCaseFalseFindHidden,
        SelectedTextMatchCaseFalseFindVisible,
        EmptySelectionCaseInsensitiveSetting,
        SelectedTextMatchCaseTrueFindHidden,
        SelectedTextMatchCaseTrueFindVisible,
        EmptySelectionCaseSensitiveSetting,
    }

    private static void Run(MultiCursorAction action, MultiCursorSelectionController controller, FindController find)
    {
        switch (action)
        {
            case MultiCursorAction.AddSelectionToNext:
                controller.AddSelectionToNextFindMatch(find);
                break;
            case MultiCursorAction.AddSelectionToPrevious:
                controller.AddSelectionToPreviousFindMatch(find);
                break;
            case MultiCursorAction.MoveSelectionToNext:
                controller.MoveSelectionToNextFindMatch(find);
                break;
            case MultiCursorAction.MoveSelectionToPrevious:
                controller.MoveSelectionToPreviousFindMatch(find);
                break;
            default:
                controller.SelectAll(find);
                break;
        }
    }

    private static SelectedTextMatchMode ToMode(bool caseSensitive) => caseSensitive ? SelectedTextMatchMode.CaseSensitive : SelectedTextMatchMode.CaseInsensitive;

    private static int[][] Lines(params int[] lines) => [.. lines.Select(line => new[] { line, 1, line, 4 })];

    private static string FindOptions(FindReplaceState state) => $"{state.MatchCase},{state.WholeWord},{state.IsRegex}";

    // Monaco: assertSelectedText
    private static void AssertSelectedText(MultiCursorAction action, int startLine, int[] expectedLines, SelectedTextMatchMode mode, bool isRevealed) =>
        TestMulticursor(Text, (editor, find, controller, _) =>
        {
            FindReplaceState state = find.State;
            bool matchCase = mode == SelectedTextMatchMode.CaseSensitive;
            state.Change(new NewFindReplaceState { SearchString = "FOO.*", IsRevealed = isRevealed, MatchCase = !matchCase, WholeWord = true, IsRegex = true }, false);
            Select(editor, new Selection(startLine, 1, startLine, 4));

            Run(action, controller, find);

            Assert.Equal(Lines(expectedLines), Selections(editor));
            Assert.Equal($"{!matchCase},True,True", FindOptions(state));
        }, matchMode: mode);

    // Monaco: assertEmptySelection
    private static void AssertEmptySelection(MultiCursorAction action, int startLine, int[] expectedLines, SelectedTextMatchMode mode) =>
        TestMulticursor(Text, (editor, find, controller, _) =>
        {
            FindReplaceState state = find.State;
            state.Change(new NewFindReplaceState { MatchCase = false, WholeWord = false, IsRegex = true }, false);
            Select(editor, new Selection(startLine, 2, startLine, 2));

            Run(action, controller, find);
            Run(action, controller, find);

            Assert.Equal(Lines(expectedLines), Selections(editor));
            Assert.Equal("True,True,False", FindOptions(state));
        }, matchMode: mode);

    // Monaco: assertFindOptionsFeedback
    private static void AssertFindOptionsFeedback(MultiCursorAction action, int startLine, int[] expectedLines, bool hasTextFocus)
    {
        string? actual = null;
        TestMulticursor(Text, (editor, find, controller, widget) =>
        {
            FindReplaceState state = find.State;
            state.Change(new NewFindReplaceState { SearchString = "foo", IsRevealed = !hasTextFocus, MatchCase = true, WholeWord = true, IsRegex = true }, false);
            Select(editor, new Selection(startLine, 1, startLine, 4));
            int before = widget.HighlightFindOptionsCount;

            Run(action, controller, find);

            actual = $"{FindOptions(state)}|{widget.HighlightFindOptionsCount - before}";
            Assert.Equal(Lines(expectedLines), Selections(editor));
        }, hasTextFocus, SelectedTextMatchMode.CaseInsensitive);

        Assert.Equal($"True,True,True|{(hasTextFocus ? 0 : 1)}", actual);
    }

    // The 30 tests Monaco generates from `${action.id}: <scenario>` (ids: editor.action.addSelectionToNextFindMatch,
    // editor.action.addSelectionToPreviousFindMatch, editor.action.moveSelectionToNextFindMatch,
    // editor.action.moveSelectionToPreviousFindMatch, editor.action.selectHighlights); the scenario names are
    // 'selected text, match case false, Find visible false' / 'true', 'empty selection always starts whole-word and case-sensitive, caseInsensitive setting',
    // 'selected text, match case true, Find visible false' / 'true', '... caseSensitive setting'.
    public static TheoryData<MultiCursorAction, Scenario, int, string> GeneratedCases()
    {
        var data = new TheoryData<MultiCursorAction, Scenario, int, string>();

        void Add(MultiCursorAction action, int startLine, string caseFalse, string empty, string caseTrue)
        {
            data.Add(action, Scenario.SelectedTextMatchCaseFalseFindHidden, startLine, caseFalse);
            data.Add(action, Scenario.SelectedTextMatchCaseFalseFindVisible, startLine, caseFalse);
            data.Add(action, Scenario.EmptySelectionCaseInsensitiveSetting, startLine, empty);
            data.Add(action, Scenario.SelectedTextMatchCaseTrueFindHidden, startLine, caseTrue);
            data.Add(action, Scenario.SelectedTextMatchCaseTrueFindVisible, startLine, caseTrue);
            data.Add(action, Scenario.EmptySelectionCaseSensitiveSetting, startLine, empty);
        }

        Add(MultiCursorAction.AddSelectionToNext, 1, "1,2", "1,5", "1,3");
        Add(MultiCursorAction.AddSelectionToPrevious, 5, "5,4", "5,1", "5,3");
        Add(MultiCursorAction.MoveSelectionToNext, 1, "2", "5", "3");
        Add(MultiCursorAction.MoveSelectionToPrevious, 5, "4", "1", "3");
        Add(MultiCursorAction.SelectHighlights, 1, "1,2,3,4,5", "1,5", "1,3,5");
        return data;
    }

    [Theory]
    [MemberData(nameof(GeneratedCases))]
    public void GeneratedActionScenarios_MatchMonaco(MultiCursorAction action, Scenario scenario, int startLine, string expected)
    {
        int[] lines = [.. expected.Split(',').Select(int.Parse)];
        switch (scenario)
        {
            case Scenario.SelectedTextMatchCaseFalseFindHidden:
                AssertSelectedText(action, startLine, lines, SelectedTextMatchMode.CaseInsensitive, false);
                break;
            case Scenario.SelectedTextMatchCaseFalseFindVisible:
                AssertSelectedText(action, startLine, lines, SelectedTextMatchMode.CaseInsensitive, true);
                break;
            case Scenario.EmptySelectionCaseInsensitiveSetting:
                AssertEmptySelection(action, startLine, lines, SelectedTextMatchMode.CaseInsensitive);
                break;
            case Scenario.SelectedTextMatchCaseTrueFindHidden:
                AssertSelectedText(action, startLine, lines, SelectedTextMatchMode.CaseSensitive, false);
                break;
            case Scenario.SelectedTextMatchCaseTrueFindVisible:
                AssertSelectedText(action, startLine, lines, SelectedTextMatchMode.CaseSensitive, true);
                break;
            default:
                AssertEmptySelection(action, startLine, lines, SelectedTextMatchMode.CaseSensitive);
                break;
        }
    }

    // Monaco: 'defaults to Find and reports live option changes'
    [Fact]
    public void DefaultsToFind_AndReportsLiveOptionChanges() => TestMulticursor(Text, (_, _, controller, _) =>
    {
        List<SelectedTextMatchMode> values = [controller.Options.SelectedTextMatchMode];
        int changes = 0;
        controller.OptionsChanged += (_, _) => changes++;
        foreach (SelectedTextMatchMode mode in new[] { SelectedTextMatchMode.CaseSensitive, SelectedTextMatchMode.CaseInsensitive })
        {
            controller.SetOptions(new MultiCursorOptions(mode));
            values.Add(controller.Options.SelectedTextMatchMode);
        }

        Assert.Equal([SelectedTextMatchMode.FindOptions, SelectedTextMatchMode.CaseSensitive, SelectedTextMatchMode.CaseInsensitive], values);
        Assert.Equal(2, changes);
    });

    // Monaco: 'default selected-text matching uses Find match case and whole word options'
    [Fact]
    public void DefaultSelectedTextMatching_UsesFindMatchCaseAndWholeWordOptions()
    {
        List<int[][]> actual = [];
        foreach ((bool matchCase, bool wholeWord) in new[] { (false, false), (false, true), (true, false), (true, true) })
        {
            TestMulticursor(Text, (editor, find, controller, _) =>
            {
                find.State.Change(new NewFindReplaceState { MatchCase = matchCase, WholeWord = wholeWord }, false);
                Select(editor, new Selection(1, 1, 1, 4));
                controller.AddSelectionToNextFindMatch(find);
                actual.Add(Selections(editor));
            });
        }

        Assert.Equal<int[][]>(
            [[[1, 1, 1, 4], [2, 1, 2, 4]], [[1, 1, 1, 4], [4, 1, 4, 4]], [[1, 1, 1, 4], [3, 1, 3, 4]], [[1, 1, 1, 4], [5, 1, 5, 4]]],
            actual);
    }

    // Monaco: 'issue #18239: replacing selected prefixes preserves differently cased identifiers'
    [Fact]
    public void Issue18239_ReplacingSelectedPrefixesPreservesDifferentlyCasedIdentifiers() => TestMulticursor(
        ["accountQryConn *grpc.ClientConn", "accountQryClient proto.AccountQueryClient"],
        (editor, find, controller, _) =>
        {
            find.State.Change(new NewFindReplaceState { MatchCase = false, WholeWord = true }, false);
            Select(editor, new Selection(1, 1, 1, 8));
            controller.AddSelectionToNextFindMatch(find);
            editor.ViewModel.Type("voucher", "test");

            Assert.Equal("voucherQryConn *grpc.ClientConn\nvoucherQryClient proto.AccountQueryClient", editor.Model.GetValue());
        },
        matchMode: SelectedTextMatchMode.CaseSensitive);

    // Monaco: 'select all replaces only selected-text occurrences while regex Find is visible'
    [Fact]
    public void SelectAll_ReplacesOnlySelectedTextOccurrencesWhileRegexFindIsVisible() => TestMulticursor(Text, (editor, find, controller, _) =>
    {
        find.State.Change(new NewFindReplaceState { SearchString = "FOO.*", IsRevealed = true, MatchCase = false, WholeWord = true, IsRegex = true }, false);
        Select(editor, new Selection(1, 1, 1, 4));

        controller.SelectAll(find);
        editor.ViewModel.Type("baz", "test");

        Assert.Equal("baz\nFOObar\nbazbar\nFOO\nbaz", editor.Model.GetValue());
    }, matchMode: SelectedTextMatchMode.CaseSensitive);

    // Monaco: 'changing Find options ends caret-started sessions before applying the selected-text matching setting'
    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 2)]
    public void ChangingFindOptions_EndsCaretStartedSessionsBeforeApplyingTheSelectedTextMatchingSetting(bool caseSensitive, int expectedLine) =>
        TestMulticursor(Text, (editor, find, controller, _) =>
        {
            Select(editor, new Selection(1, 2, 1, 2));
            controller.AddSelectionToNextFindMatch(find);
            find.State.Change(new NewFindReplaceState { MatchCase = true, WholeWord = true }, false);
            find.State.Change(new NewFindReplaceState { MatchCase = false, WholeWord = false }, false);
            controller.AddSelectionToNextFindMatch(find);

            Assert.Equal([[1, 1, 1, 4], [expectedLine, 1, expectedLine, 4]], Selections(editor));
        }, matchMode: ToMode(caseSensitive));

    // Monaco: 'mixed-case initial selections are compared using the selection setting, not Find'
    [Fact]
    public void MixedCaseInitialSelections_AreComparedUsingTheSelectionSetting() => TestMulticursor(["foo", "FOO", "fooBar"], (editor, find, controller, _) =>
    {
        find.State.Change(new NewFindReplaceState { MatchCase = true }, false);
        Select(editor, new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4));
        controller.AddSelectionToNextFindMatch(find);

        Assert.Equal([[1, 1, 1, 4], [2, 1, 2, 4], [3, 1, 3, 4]], Selections(editor));
    }, matchMode: SelectedTextMatchMode.CaseInsensitive);

    // Monaco: 'Find-focused mixed-case initial selections respect Find match case false'
    [Fact]
    public void FindFocusedMixedCaseInitialSelections_RespectFindMatchCaseFalse() => TestMulticursor(["foo", "FOO", "fooBar"], (editor, find, controller, _) =>
    {
        find.State.Change(new NewFindReplaceState { SearchString = "foo", IsRevealed = true, MatchCase = false }, false);
        Select(editor, new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4));
        controller.AddSelectionToNextFindMatch(find);

        Assert.Equal([[1, 1, 1, 4], [2, 1, 2, 4], [3, 1, 3, 4]], Selections(editor));
    }, hasTextFocus: false, matchMode: SelectedTextMatchMode.CaseSensitive);

    // Monaco: 'Find-focused mixed-case initial selections respect Find match case true'
    [Fact]
    public void FindFocusedMixedCaseInitialSelections_RespectFindMatchCaseTrue() => TestMulticursor(["foo", "FOO", "fooBar"], (editor, find, controller, _) =>
    {
        find.State.Change(new NewFindReplaceState { SearchString = "foo", IsRevealed = true, MatchCase = true }, false);
        Select(editor, new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4));
        controller.AddSelectionToNextFindMatch(find);

        Assert.Equal([[1, 1, 1, 4], [2, 1, 2, 4]], Selections(editor));
    }, hasTextFocus: false, matchMode: SelectedTextMatchMode.CaseInsensitive);

    // Monaco: 'switching focus transfers matching rules between selection and Find sessions'
    [Fact]
    public void SwitchingFocus_TransfersMatchingRulesBetweenSelectionAndFindSessions() => TestMulticursor(
        ["foo", "FOO", "fooqux", "foobar", "bar", "BAR", "barista"],
        (editor, find, controller, _) =>
        {
            Select(editor, new Selection(1, 1, 1, 4));
            controller.MoveSelectionToNextFindMatch(find);
            List<int[][]> actual = [Selections(editor)];

            find.State.Change(new NewFindReplaceState { SearchString = "bar", IsRevealed = true, MatchCase = false, WholeWord = true }, false);
            SetTextFocus(editor, false);
            controller.MoveSelectionToNextFindMatch(find);
            actual.Add(Selections(editor));

            SetTextFocus(editor, true);
            controller.MoveSelectionToNextFindMatch(find);
            actual.Add(Selections(editor));

            Assert.Equal<int[][]>([[[3, 1, 3, 4]], [[5, 1, 5, 4]], [[7, 1, 7, 4]]], actual);
        },
        matchMode: SelectedTextMatchMode.CaseSensitive);

    // Monaco: 'the default Find session survives returning focus to the editor'
    [Fact]
    public void TheDefaultFindSession_SurvivesReturningFocusToTheEditor() => TestMulticursor(["foo", "bar", "bar"], (editor, find, controller, _) =>
    {
        SetTextFocus(editor, false);
        Select(editor, new Selection(1, 1, 1, 4));
        find.State.Change(new NewFindReplaceState { SearchString = "bar", IsRevealed = true }, false);
        controller.AddSelectionToNextFindMatch(find);

        SetTextFocus(editor, true);
        controller.AddSelectionToNextFindMatch(find);

        Assert.Equal([[1, 1, 1, 4], [2, 1, 2, 4], [3, 1, 3, 4]], Selections(editor));
    });

    private static readonly string[] CrlfText = ["foo.", "bar", "FOO.", "BAR", "xfoo.", "bar"];

    // Monaco: 'multiline CRLF selections match literal substrings with match case false'
    [Fact]
    public void MultilineCrlfSelections_MatchLiteralSubstringsWithMatchCaseFalse() => TestMulticursor(CrlfText, (editor, find, controller, _) =>
    {
        editor.Model.SetEol(EndOfLineSequence.CRLF);
        find.State.Change(new NewFindReplaceState { SearchString = "unrelated.*", IsRevealed = true, MatchCase = true, WholeWord = true, IsRegex = true }, false);
        Select(editor, new Selection(1, 1, 2, 4));
        controller.SelectAll(find);

        Assert.Equal([[1, 1, 2, 4], [3, 1, 4, 4], [5, 2, 6, 4]], Selections(editor));
    }, matchMode: SelectedTextMatchMode.CaseInsensitive);

    // Monaco: 'multiline CRLF selections match literal substrings with match case true'
    [Fact]
    public void MultilineCrlfSelections_MatchLiteralSubstringsWithMatchCaseTrue() => TestMulticursor(CrlfText, (editor, find, controller, _) =>
    {
        editor.Model.SetEol(EndOfLineSequence.CRLF);
        find.State.Change(new NewFindReplaceState { SearchString = "unrelated.*", IsRevealed = true, MatchCase = false, WholeWord = true, IsRegex = true }, false);
        Select(editor, new Selection(1, 1, 2, 4));
        controller.SelectAll(find);

        Assert.Equal([[1, 1, 2, 4], [5, 2, 6, 4]], Selections(editor));
    }, matchMode: SelectedTextMatchMode.CaseSensitive);

    private static readonly string[] FindFocusedText = ["foo", "BAR", "bar", "barista", "foo"];

    private static void ToggleMatchModeSettings(MultiCursorSelectionController controller)
    {
        controller.SetOptions(new MultiCursorOptions(SelectedTextMatchMode.CaseInsensitive));
        controller.SetOptions(new MultiCursorOptions(SelectedTextMatchMode.CaseSensitive));
    }

    // Monaco: 'editor.action.addSelectionToNextFindMatch: Find-focused commands retain Find matching rules'
    [Fact]
    public void AddSelectionToNextFindMatch_FindFocusedCommandsRetainFindMatchingRules() => TestMulticursor(FindFocusedText, (editor, find, controller, _) =>
    {
        Select(editor, new Selection(1, 1, 1, 4));
        find.State.Change(new NewFindReplaceState { SearchString = "bar", IsRevealed = true, MatchCase = false, WholeWord = true }, false);
        ToggleMatchModeSettings(controller);
        controller.AddSelectionToNextFindMatch(find);

        Assert.Equal([[1, 1, 1, 4], [2, 1, 2, 4]], Selections(editor));
    }, hasTextFocus: false, matchMode: SelectedTextMatchMode.CaseSensitive);

    // Monaco: 'editor.action.addSelectionToPreviousFindMatch: Find-focused commands retain Find matching rules'
    [Fact]
    public void AddSelectionToPreviousFindMatch_FindFocusedCommandsRetainFindMatchingRules() => TestMulticursor(FindFocusedText, (editor, find, controller, _) =>
    {
        Select(editor, new Selection(5, 1, 5, 4));
        find.State.Change(new NewFindReplaceState { SearchString = "bar", IsRevealed = true, MatchCase = false, WholeWord = true }, false);
        ToggleMatchModeSettings(controller);
        controller.AddSelectionToPreviousFindMatch(find);

        Assert.Equal([[5, 1, 5, 4], [3, 1, 3, 4]], Selections(editor));
    }, hasTextFocus: false, matchMode: SelectedTextMatchMode.CaseSensitive);

    // Monaco: 'editor.action.selectHighlights: Find-focused commands retain Find matching rules'
    [Fact]
    public void SelectHighlights_FindFocusedCommandsRetainFindMatchingRules() => TestMulticursor(FindFocusedText, (editor, find, controller, _) =>
    {
        Select(editor, new Selection(1, 1, 1, 4));
        find.State.Change(new NewFindReplaceState { SearchString = "bar", IsRevealed = true, MatchCase = false, WholeWord = true }, false);
        ToggleMatchModeSettings(controller);
        controller.SelectAll(find);

        Assert.Equal([[2, 1, 2, 4], [3, 1, 3, 4]], Selections(editor));
    }, hasTextFocus: false, matchMode: SelectedTextMatchMode.CaseSensitive);

    // Monaco: 'Find-focused regex select all follows Find options in both independent matching modes'
    [Theory]
    [InlineData(true, false, "1,4,5")]
    [InlineData(false, true, "1,5")]
    public void FindFocusedRegexSelectAll_FollowsFindOptionsInBothIndependentMatchingModes(bool caseSensitive, bool matchCase, string expectedLines) =>
        TestMulticursor(Text, (editor, find, controller, _) =>
        {
            find.State.Change(new NewFindReplaceState { SearchString = "^foo$", IsRevealed = true, MatchCase = matchCase, IsRegex = true }, false);
            Select(editor, new Selection(1, 1, 1, 4));

            controller.SelectAll(find);

            Assert.Equal(Lines([.. expectedLines.Split(',').Select(int.Parse)]), Selections(editor));
        }, hasTextFocus: false, matchMode: ToMode(caseSensitive));

    // Monaco: 'add next occurrence does not highlight unrelated Find options'
    [Fact]
    public void AddNextOccurrence_DoesNotHighlightUnrelatedFindOptions() =>
        AssertFindOptionsFeedback(MultiCursorAction.AddSelectionToNext, 1, [1, 2], true);

    // Monaco: 'add previous occurrence does not highlight unrelated Find options'
    [Fact]
    public void AddPreviousOccurrence_DoesNotHighlightUnrelatedFindOptions() =>
        AssertFindOptionsFeedback(MultiCursorAction.AddSelectionToPrevious, 5, [5, 4], true);

    // Monaco: 'select all occurrences does not highlight unrelated Find options'
    [Fact]
    public void SelectAllOccurrences_DoesNotHighlightUnrelatedFindOptions() =>
        AssertFindOptionsFeedback(MultiCursorAction.SelectHighlights, 1, [1, 2, 3, 4, 5], true);

    // Monaco: 'Find-focused occurrence selection still highlights Find options'
    [Fact]
    public void FindFocusedOccurrenceSelection_StillHighlightsFindOptions() =>
        AssertFindOptionsFeedback(MultiCursorAction.AddSelectionToNext, 1, [1, 5], false);
}
