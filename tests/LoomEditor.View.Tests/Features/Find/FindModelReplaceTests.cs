// Ported from microsoft/vscode: src/vs/editor/contrib/find/test/browser/findModel.test.ts (suite 'FindModel')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Features.Find;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using LoomEditor.View.Tests.Features.Find.TestHelpers;

namespace LoomEditor.View.Tests.Features.Find;
public class FindModelReplaceTests
{
    // Monaco: 'replace hello'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceHello(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", ReplaceString = "hi", WholeWord = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        CursorTestHelpers.MoveTo(editor.ViewModel, 6, 20);
        FindModelTestHelpers.AssertFindState(editor, [6, 20, 6, 20], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);
        Assert.Equal("    cout << \"hello world, Hello!\" << endl;", editor.Model.GetLineContent(6));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [6, 27, 6, 32], [6, 27, 6, 32], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);
        Assert.Equal("    cout << \"hello world, Hello!\" << endl;", editor.Model.GetLineContent(6));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], [7, 14, 7, 19], [[6, 14, 6, 19], [7, 14, 7, 19], [8, 14, 8, 19]]);
        Assert.Equal("    cout << \"hello world, hi!\" << endl;", editor.Model.GetLineContent(6));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 19], [8, 14, 8, 19], [[6, 14, 6, 19], [8, 14, 8, 19]]);
        Assert.Equal("    cout << \"hi world again\" << endl;", editor.Model.GetLineContent(7));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19]]);
        Assert.Equal("    cout << \"hi world again\" << endl;", editor.Model.GetLineContent(8));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [6, 16, 6, 16], null, []);
        Assert.Equal("    cout << \"hi world, hi!\" << endl;", editor.Model.GetLineContent(6));
        findModel.Dispose();
    });

    // Monaco: 'replace bla'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceBla(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "bla", ReplaceString = "ciao" }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[11, 4, 11, 7], [11, 7, 11, 10], [11, 10, 11, 13]]);

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [11, 4, 11, 7], [11, 4, 11, 7], [[11, 4, 11, 7], [11, 7, 11, 10], [11, 10, 11, 13]]);
        Assert.Equal("// blablablaciao", editor.Model.GetLineContent(11));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [11, 8, 11, 11], [11, 8, 11, 11], [[11, 8, 11, 11], [11, 11, 11, 14]]);
        Assert.Equal("// ciaoblablaciao", editor.Model.GetLineContent(11));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [11, 12, 11, 15], [11, 12, 11, 15], [[11, 12, 11, 15]]);
        Assert.Equal("// ciaociaoblaciao", editor.Model.GetLineContent(11));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [11, 16, 11, 16], null, []);
        Assert.Equal("// ciaociaociaociao", editor.Model.GetLineContent(11));
        findModel.Dispose();
    });

    // Monaco: 'replaceAll hello'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceAllHello(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", ReplaceString = "hi", WholeWord = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        CursorTestHelpers.MoveTo(editor.ViewModel, 6, 20);
        FindModelTestHelpers.AssertFindState(editor, [6, 20, 6, 20], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);
        Assert.Equal("    cout << \"hello world, Hello!\" << endl;", editor.Model.GetLineContent(6));

        findModel.ReplaceAll();
        FindModelTestHelpers.AssertFindState(editor, [6, 17, 6, 17], null, []);
        Assert.Equal("    cout << \"hi world, hi!\" << endl;", editor.Model.GetLineContent(6));
        Assert.Equal("    cout << \"hi world again\" << endl;", editor.Model.GetLineContent(7));
        Assert.Equal("    cout << \"hi world again\" << endl;", editor.Model.GetLineContent(8));
        findModel.Dispose();
    });

    // Monaco: 'replaceAll two spaces with one space'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceAllTwoSpacesWithOneSpace(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "  ", ReplaceString = " " }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 1, 6, 3], [6, 3, 6, 5], [7, 1, 7, 3], [7, 3, 7, 5], [8, 1, 8, 3], [8, 3, 8, 5], [9, 1, 9, 3], [9, 3, 9, 5]]);

        findModel.ReplaceAll();
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 1, 6, 3], [7, 1, 7, 3], [8, 1, 8, 3], [9, 1, 9, 3]]);
        Assert.Equal("  cout << \"hello world, Hello!\" << endl;", editor.Model.GetLineContent(6));
        Assert.Equal("  cout << \"hello world again\" << endl;", editor.Model.GetLineContent(7));
        Assert.Equal("  cout << \"Hello world again\" << endl;", editor.Model.GetLineContent(8));
        Assert.Equal("  cout << \"helloworld again\" << endl;", editor.Model.GetLineContent(9));
        findModel.Dispose();
    });

    // Monaco: 'replaceAll bla'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceAllBla(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "bla", ReplaceString = "ciao" }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[11, 4, 11, 7], [11, 7, 11, 10], [11, 10, 11, 13]]);

        findModel.ReplaceAll();
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);
        Assert.Equal("// ciaociaociaociao", editor.Model.GetLineContent(11));
        findModel.Dispose();
    });

    // Monaco: 'replaceAll bla with \\t\\n'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceAllBlaWithTN(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "bla", ReplaceString = "<\\n\\t>", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[11, 4, 11, 7], [11, 7, 11, 10], [11, 10, 11, 13]]);

        findModel.ReplaceAll();
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);
        Assert.Equal("// <", editor.Model.GetLineContent(11));
        Assert.Equal("\t><", editor.Model.GetLineContent(12));
        Assert.Equal("\t><", editor.Model.GetLineContent(13));
        Assert.Equal("\t>ciao", editor.Model.GetLineContent(14));
        findModel.Dispose();
    });

    // Monaco: 'issue #3516: "replace all" moves page/cursor/focus/scroll to the place of the last replacement'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Issue3516ReplaceAllMovesPageCursorFocusScrollToThePlaceOfTheLastReplacement(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "include", ReplaceString = "bar" }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[2, 2, 2, 9], [3, 2, 3, 9]]);

        findModel.ReplaceAll();
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);

        Assert.Equal("#bar \"cool.h\"", editor.Model.GetLineContent(2));
        Assert.Equal("#bar <iostream>", editor.Model.GetLineContent(3));
        findModel.Dispose();
    });

    // Monaco: 'replace when search string has look ahed regex'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceWhenSearchStringHasLookAhedRegex(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello(?=\\sworld)", ReplaceString = "hi", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.Replace();

        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [7, 14, 7, 19], [8, 14, 8, 19]]);
        Assert.Equal("    cout << \"hello world, Hello!\" << endl;", editor.Model.GetLineContent(6));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], [7, 14, 7, 19], [[7, 14, 7, 19], [8, 14, 8, 19]]);
        Assert.Equal("    cout << \"hi world, Hello!\" << endl;", editor.Model.GetLineContent(6));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 19], [8, 14, 8, 19], [[8, 14, 8, 19]]);
        Assert.Equal("    cout << \"hi world again\" << endl;", editor.Model.GetLineContent(7));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [8, 16, 8, 16], null, []);
        Assert.Equal("    cout << \"hi world again\" << endl;", editor.Model.GetLineContent(8));
        findModel.Dispose();
    });

    // Monaco: 'replace when search string has look ahed regex and cursor is at the last find match'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceWhenSearchStringHasLookAhedRegexAndCursorIsAtTheLastFindMatch(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello(?=\\sworld)", ReplaceString = "hi", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        CursorTestHelpers.MoveTo(editor.ViewModel, 8, 14);

        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 14], null, [[6, 14, 6, 19], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.Replace();

        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 19], [8, 14, 8, 19], [[6, 14, 6, 19], [7, 14, 7, 19], [8, 14, 8, 19]]);

        Assert.Equal("    cout << \"Hello world again\" << endl;", editor.Model.GetLineContent(8));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [7, 14, 7, 19],]);
        Assert.Equal("    cout << \"hi world again\" << endl;", editor.Model.GetLineContent(8));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], [7, 14, 7, 19], [[7, 14, 7, 19]]);
        Assert.Equal("    cout << \"hi world, Hello!\" << endl;", editor.Model.GetLineContent(6));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [7, 16, 7, 16], null, []);
        Assert.Equal("    cout << \"hi world again\" << endl;", editor.Model.GetLineContent(7));
        findModel.Dispose();
    });

    // Monaco: 'replaceAll when search string has look ahed regex'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceAllWhenSearchStringHasLookAhedRegex(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello(?=\\sworld)", ReplaceString = "hi", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.ReplaceAll();

        Assert.Equal("    cout << \"hi world, Hello!\" << endl;", editor.Model.GetLineContent(6));
        Assert.Equal("    cout << \"hi world again\" << endl;", editor.Model.GetLineContent(7));
        Assert.Equal("    cout << \"hi world again\" << endl;", editor.Model.GetLineContent(8));

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);
        findModel.Dispose();
    });

    // Monaco: 'replace when search string has look ahed regex and replace string has capturing groups'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceWhenSearchStringHasLookAhedRegexAndReplaceStringHasCapturingGroups(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hel(lo)(?=\\sworld)", ReplaceString = "hi$1", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.Replace();

        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [7, 14, 7, 19], [8, 14, 8, 19]]);
        Assert.Equal("    cout << \"hello world, Hello!\" << endl;", editor.Model.GetLineContent(6));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], [7, 14, 7, 19], [[7, 14, 7, 19], [8, 14, 8, 19]]);
        Assert.Equal("    cout << \"hilo world, Hello!\" << endl;", editor.Model.GetLineContent(6));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 19], [8, 14, 8, 19], [[8, 14, 8, 19]]);
        Assert.Equal("    cout << \"hilo world again\" << endl;", editor.Model.GetLineContent(7));

        findModel.Replace();
        FindModelTestHelpers.AssertFindState(editor, [8, 18, 8, 18], null, []);
        Assert.Equal("    cout << \"hilo world again\" << endl;", editor.Model.GetLineContent(8));
        findModel.Dispose();
    });

    // Monaco: 'replaceAll when search string has look ahed regex and replace string has capturing groups'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceAllWhenSearchStringHasLookAhedRegexAndReplaceStringHasCapturingGroups(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "wo(rl)d(?=.*;$)", ReplaceString = "gi$1", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 20, 6, 25], [7, 20, 7, 25], [8, 20, 8, 25], [9, 19, 9, 24]]);

        findModel.ReplaceAll();

        Assert.Equal("    cout << \"hello girl, Hello!\" << endl;", editor.Model.GetLineContent(6));
        Assert.Equal("    cout << \"hello girl again\" << endl;", editor.Model.GetLineContent(7));
        Assert.Equal("    cout << \"Hello girl again\" << endl;", editor.Model.GetLineContent(8));
        Assert.Equal("    cout << \"hellogirl again\" << endl;", editor.Model.GetLineContent(9));

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);
        findModel.Dispose();
    });

    // Monaco: 'replaceAll when search string is multiline and has look ahed regex and replace string has capturing groups'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceAllWhenSearchStringIsMultilineAndHasLookAhedRegexAndReplaceStringHasCapturingGroups(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "wo(rl)d(.*;\\n)(?=.*hello)", ReplaceString = "gi$1$2", IsRegex = true, MatchCase = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 20, 7, 1], [8, 20, 9, 1]]);

        findModel.ReplaceAll();

        Assert.Equal("    cout << \"hello girl, Hello!\" << endl;", editor.Model.GetLineContent(6));
        Assert.Equal("    cout << \"Hello girl again\" << endl;", editor.Model.GetLineContent(8));

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);
        findModel.Dispose();
    });

    // Monaco: 'replaceAll preserving case'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceAllPreservingCase(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", ReplaceString = "goodbye", IsRegex = false, MatchCase = false, PreserveCase = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19], [9, 14, 9, 19],]);

        findModel.ReplaceAll();

        Assert.Equal("    cout << \"goodbye world, Goodbye!\" << endl;", editor.Model.GetLineContent(6));
        Assert.Equal("    cout << \"goodbye world again\" << endl;", editor.Model.GetLineContent(7));
        Assert.Equal("    cout << \"Goodbye world again\" << endl;", editor.Model.GetLineContent(8));
        Assert.Equal("    cout << \"goodbyeworld again\" << endl;", editor.Model.GetLineContent(9));

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);
        findModel.Dispose();
    });

    // Monaco: 'issue #18711 replaceAll with empty string'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Issue18711ReplaceAllWithEmptyString(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", ReplaceString = "", WholeWord = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.ReplaceAll();
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);
        Assert.Equal("    cout << \" world, !\" << endl;", editor.Model.GetLineContent(6));
        Assert.Equal("    cout << \" world again\" << endl;", editor.Model.GetLineContent(7));
        Assert.Equal("    cout << \" world again\" << endl;", editor.Model.GetLineContent(8));
        findModel.Dispose();
    });

    // Monaco: 'issue #32522 replaceAll with ^ on more than 1000 matches'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Issue32522ReplaceAllWithCaretOnMoreThan1000Matches(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var initialText = string.Concat(Enumerable.Range(0, 1100).Select(i => "line" + i + (char)10));
        editor.Model.SetValue(initialText);
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "^", ReplaceString = "a ", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        findModel.ReplaceAll();

        var expectedText = string.Concat(Enumerable.Range(0, 1100).Select(i => "a line" + i + (char)10)) + "a ";
        Assert.Equal(expectedText, editor.Model.GetValue());
        findModel.Dispose();
    });

    // Monaco: 'issue #19740 Find and replace capture group/backreference inserts `undefined` instead of empty string'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Issue19740FindAndReplaceCaptureGroupBackreferenceInsertsUndefinedInsteadOfEmptyString(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello(z)?", ReplaceString = "hi$1", IsRegex = true, MatchCase = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [7, 14, 7, 19], [9, 14, 9, 19]]);

        findModel.ReplaceAll();
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);
        Assert.Equal("    cout << \"hi world, Hello!\" << endl;", editor.Model.GetLineContent(6));
        Assert.Equal("    cout << \"hi world again\" << endl;", editor.Model.GetLineContent(7));
        Assert.Equal("    cout << \"hiworld again\" << endl;", editor.Model.GetLineContent(9));
        findModel.Dispose();
    });
}
