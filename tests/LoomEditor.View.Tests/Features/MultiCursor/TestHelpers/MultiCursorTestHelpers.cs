// Ported from microsoft/vscode: src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts (fromRange, testMulticursor, setTextFocus)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Features.MultiCursor;
using LoomEditor.View.Tests.Features.Find.TestHelpers;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.MultiCursor.TestHelpers;

/// <summary>An editor with a find controller (and its fake widget) and a multi-cursor controller, like Monaco's <c>testMulticursor</c>.</summary>
internal static class MultiCursorTestHelpers
{
    /// <summary>Monaco: <c>fromRange</c>.</summary>
    public static int[] FromRange(Selection s) => [s.Range.StartLineNumber, s.Range.StartColumn, s.Range.EndLineNumber, s.Range.EndColumn];

    public static int[][] Selections(TestCodeEditor editor) => [.. editor.ViewModel.GetSelections().Select(FromRange)];

    /// <summary>Monaco: <c>setTextFocus</c>.</summary>
    public static void SetTextFocus(TestCodeEditor editor, bool hasFocus) => editor.ViewModel.SetHasFocus(hasFocus);

    /// <summary>Monaco: <c>testMulticursor</c> (<c>selectedTextMatchMode</c> goes through <paramref name="matchMode"/>).</summary>
    public static void TestMulticursor(
        string[] text,
        Action<TestCodeEditor, FindController, MultiCursorSelectionController, TestFindWidget> callback,
        bool hasTextFocus = true,
        SelectedTextMatchMode matchMode = SelectedTextMatchMode.FindOptions) =>
        TestCodeEditor.WithTestCodeEditor(text, editor =>
        {
            var widget = new TestFindWidget();
            using var findController = new FindController(editor.ViewModel, new ManualEditorScheduler()) { Widget = widget };
            using var controller = new MultiCursorSelectionController(editor.ViewModel, new MultiCursorOptions(matchMode));

            callback(editor, findController, controller, widget);
        }, new TestCodeEditorOptions(HasTextFocus: hasTextFocus));

    public static void Select(TestCodeEditor editor, params Selection[] selections) => editor.ViewModel.SetSelections("test", selections);
}
