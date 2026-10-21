// Ported from microsoft/vscode: src/vs/editor/contrib/find/test/browser/findModel.test.ts (findTest, fromRange, _getFindState, assertFindState)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Find.TestHelpers;

internal static class FindModelTestHelpers
{
    private static readonly string[] TextArr =
    [
        "// my cool header",
        "#include \"cool.h\"",
        "#include <iostream>",
        "",
        "int main() {",
        "    cout << \"hello world, Hello!\" << endl;",
        "    cout << \"hello world again\" << endl;",
        "    cout << \"Hello world again\" << endl;",
        "    cout << \"helloworld again\" << endl;",
        "}",
        "// blablablaciao",
        "",
    ];

    /// <summary>An <see cref="ITextSnapshot"/> that returns a fixed list of chunks.</summary>
    private sealed class ChunkedSnapshot(IReadOnlyList<string> chunks) : ITextSnapshot
    {
        private int _next;

        public string? Read() => _next < chunks.Count ? chunks[_next++] : null;
    }

    // JavaScript substr(start, length): the length is clamped to the text.
    private static string Substr(string text, int start, int length) => text.Substring(start, Math.Min(length, text.Length - start));

    /// <summary>Monaco: <c>findTest</c> — runs the callback on a plain buffer, or on a buffer made of three chunks.</summary>
    public static void FindTest(bool chunked, Action<TestCodeEditor, ManualEditorScheduler> callback)
    {
        var scheduler = new ManualEditorScheduler();
        var text = string.Join(((char)10).ToString(), TextArr);
        if (!chunked)
        {
            TestCodeEditor.WithTestCodeEditor(text, editor => callback(editor, scheduler));
            return;
        }

        var snapshot = new ChunkedSnapshot([Substr(text, 0, 94), Substr(text, 94, 101), Substr(text, 195, 59)]);
        TestCodeEditor.WithTestCodeEditor(TextModel.FromSnapshot(snapshot), editor => callback(editor, scheduler));
    }

    public static void SetPosition(this TestCodeEditor editor, int lineNumber, int column) =>
        editor.ViewModel.SetSelections("test", [new Selection(lineNumber, column, lineNumber, column)]);

    public static void SetSelection(this TestCodeEditor editor, Range range) =>
        editor.ViewModel.SetSelections("test", [Selection.FromRange(range, SelectionDirection.LeftToRight)]);

    public static void SetSelection(this TestCodeEditor editor, Selection selection) =>
        editor.ViewModel.SetSelections("test", [selection]);

    public static Selection GetSelection(this TestCodeEditor editor) => editor.ViewModel.GetSelection();

    private static string Describe(IEnumerable<int[]> ranges) => string.Join(" ", ranges.Select(r => $"[{string.Join(",", r)}]"));

    /// <summary>Monaco: <c>assertFindState</c>.</summary>
    public static void AssertFindState(TestCodeEditor editor, int[] cursor, int[]? highlighted, int[][] findDecorations)
    {
        Assert.Equal(Describe([cursor]), Describe([ToArray(editor.GetSelection().Range)]));

        List<Range> currentFindMatches = [];
        List<Range> allFindMatches = [];
        foreach (ModelDecoration dec in editor.Model.GetAllDecorations())
        {
            if (dec.Options.ClassName == "currentFindMatch")
            {
                currentFindMatches.Add(dec.Range);
                allFindMatches.Add(dec.Range);
            }
            else if (dec.Options.ClassName == "findMatch")
            {
                allFindMatches.Add(dec.Range);
            }
        }

        currentFindMatches.Sort(Range.CompareUsingStarts);
        allFindMatches.Sort(Range.CompareUsingStarts);

        Assert.Equal(Describe(highlighted is null ? [] : [highlighted]), Describe(currentFindMatches.Select(ToArray)));
        Assert.Equal(Describe(findDecorations), Describe(allFindMatches.Select(ToArray)));
    }

    private static int[] ToArray(Range r) => [r.StartLineNumber, r.StartColumn, r.EndLineNumber, r.EndColumn];

    /// <summary>Compares the selections of the editor with the expected ones (Monaco: <c>map(s =&gt; s.toString())</c>).</summary>
    public static void AssertSelections(TestCodeEditor editor, params Selection[] expected) =>
        Assert.Equal(expected.Select(s => s.ToString()), editor.ViewModel.GetSelections().Select(s => s.ToString()));
}
