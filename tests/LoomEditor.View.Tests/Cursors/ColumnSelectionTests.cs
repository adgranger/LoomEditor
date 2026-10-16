// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (column selection tests)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public class ColumnSelectionTests
{
    private static readonly string[] GulpLines =
    [
        "var gulp = require(\"gulp\");",
        "var path = require(\"path\");",
        "var rimraf = require(\"rimraf\");",
        "var isarray = require(\"isarray\");",
        "var merge = require(\"merge-stream\");",
        "var concat = require(\"gulp-concat\");",
        "var newer = require(\"gulp-newer\");",
    ];

    private static readonly string[] PropertyLines =
    [
        "<property id=\"SomeThing\" key=\"SomeKey\" value=\"000\"/>",
        "<property id=\"SomeThing\" key=\"SomeKey\" value=\"000\"/>",
        "<property id=\"SomeThing\" Key=\"SomeKey\" value=\"000\"/>",
        "<property id=\"SomeThing\" key=\"SomeKey\" value=\"000\"/>",
        "<property id=\"SomeThing\" key=\"SoMEKEy\" value=\"000\"/>",
        "<property id=\"SomeThing\" key=\"SomeKey\" value=\"000\"/>",
        "<property id=\"SomeThing\" key=\"SomeKey\" value=\"000\"/>",
        "<property id=\"SomeThing\" key=\"SomeKey\" valuE=\"000\"/>",
        "<property id=\"SomeThing\" key=\"SomeKey\" value=\"000\"/>",
        "<property id=\"SomeThing\" key=\"SomeKey\" value=\"00X\"/>",
    ];

    private static void ColumnSelect(ViewModel vm, Position position, int mouseColumn, bool doColumnSelect) =>
        CoreNavigationCommands.ColumnSelect.RunCoreEditorCommand(vm, new ColumnSelectCommandArgs(position, position, mouseColumn, doColumnSelect));

    private static void Run(CoreEditorCommand<BaseCommandArgs> command, ViewModel vm, int times = 1)
    {
        for (int i = 0; i < times; i++)
        {
            command.RunCoreEditorCommand(vm, new BaseCommandArgs());
        }
    }

    private static Selection[] Sels(int startColumn, params (int Line, int End)[] lines) =>
        lines.Select(l => new Selection(l.Line, startColumn, l.Line, l.End)).ToArray();

    // Monaco: 'column select 1'
    [Fact]
    public void ColumnSelect1() =>
        TestCodeEditor.WithTestCodeEditor(
            [
                "\tprivate compute(a:number): boolean {",
                "\t\tif (a + 3 === 0 || a + 5 === 0) {",
                "\t\t\treturn false;",
                "\t\t}",
                "\t}",
            ],
            editor =>
            {
                ViewModel vm = editor.ViewModel;
                MoveTo(vm, 1, 7, false);
                AssertCursor(vm, new Position(1, 7));

                ColumnSelect(vm, new Position(4, 4), 15, true);

                AssertCursor(
                    vm,
                    [
                        new Selection(1, 7, 1, 12),
                        new Selection(2, 4, 2, 9),
                        new Selection(3, 3, 3, 6),
                        new Selection(4, 4, 4, 4),
                    ]);
            });

    // Monaco: 'issue #4905 - column select is biased to the right'
    [Fact]
    public void Issue4905_ColumnSelectIsBiasedToTheRight() =>
        TestCodeEditor.WithTestCodeEditor(GulpLines, editor =>
        {
            ViewModel vm = editor.ViewModel;
            MoveTo(vm, 1, 4, false);
            AssertCursor(vm, new Position(1, 4));

            ColumnSelect(vm, new Position(4, 1), 1, true);

            AssertCursor(
                vm,
                [
                    new Selection(1, 4, 1, 1),
                    new Selection(2, 4, 2, 1),
                    new Selection(3, 4, 3, 1),
                    new Selection(4, 4, 4, 1),
                ]);
        });

    // Monaco: 'issue #20087: column select with mouse'
    [Fact]
    public void Issue20087_ColumnSelectWithMouse() =>
        TestCodeEditor.WithTestCodeEditor(PropertyLines, editor =>
        {
            ViewModel vm = editor.ViewModel;
            Selection[] expected = Enumerable.Range(1, 10).Reverse().Select(line => new Selection(line, 10, line, 1)).ToArray();

            MoveTo(vm, 10, 10, false);
            AssertCursor(vm, new Position(10, 10));

            ColumnSelect(vm, new Position(1, 1), 1, true);
            AssertCursor(vm, expected);

            ColumnSelect(vm, new Position(1, 1), 1, true);
            AssertCursor(vm, expected);
        });

    // Monaco: 'issue #20087: column select with keyboard'
    [Fact]
    public void Issue20087_ColumnSelectWithKeyboard() =>
        TestCodeEditor.WithTestCodeEditor(PropertyLines, editor =>
        {
            ViewModel vm = editor.ViewModel;
            MoveTo(vm, 10, 10, false);
            AssertCursor(vm, new Position(10, 10));

            Run(CoreNavigationCommands.CursorColumnSelectLeft, vm);
            AssertCursor(vm, [new Selection(10, 10, 10, 9)]);

            Run(CoreNavigationCommands.CursorColumnSelectLeft, vm);
            AssertCursor(vm, [new Selection(10, 10, 10, 8)]);

            Run(CoreNavigationCommands.CursorColumnSelectRight, vm);
            AssertCursor(vm, [new Selection(10, 10, 10, 9)]);

            Run(CoreNavigationCommands.CursorColumnSelectUp, vm);
            AssertCursor(vm, [new Selection(10, 10, 10, 9), new Selection(9, 10, 9, 9)]);

            Run(CoreNavigationCommands.CursorColumnSelectDown, vm);
            AssertCursor(vm, [new Selection(10, 10, 10, 9)]);
        });

    // Monaco: 'issue #118062: Column selection cannot select first position of a line'
    [Fact]
    public void Issue118062_ColumnSelectionCanSelectFirstPositionOfALine() =>
        TestCodeEditor.WithTestCodeEditor("hello world", editor =>
        {
            ViewModel vm = editor.ViewModel;
            MoveTo(vm, 1, 2, false);
            AssertCursor(vm, new Position(1, 2));

            Run(CoreNavigationCommands.CursorColumnSelectLeft, vm);
            AssertCursor(vm, [new Selection(1, 2, 1, 1)]);
        });

    // Monaco: 'column select with keyboard'
    [Fact]
    public void ColumnSelectWithKeyboard() =>
        TestCodeEditor.WithTestCodeEditor(GulpLines, editor =>
        {
            ViewModel vm = editor.ViewModel;
            MoveTo(vm, 1, 4, false);
            AssertCursor(vm, new Position(1, 4));

            Run(CoreNavigationCommands.CursorColumnSelectRight, vm);
            AssertCursor(vm, [new Selection(1, 4, 1, 5)]);

            Run(CoreNavigationCommands.CursorColumnSelectDown, vm);
            AssertCursor(vm, [new Selection(1, 4, 1, 5), new Selection(2, 4, 2, 5)]);

            Run(CoreNavigationCommands.CursorColumnSelectDown, vm);
            AssertCursor(vm, [new Selection(1, 4, 1, 5), new Selection(2, 4, 2, 5), new Selection(3, 4, 3, 5)]);

            Run(CoreNavigationCommands.CursorColumnSelectDown, vm, 4);
            AssertCursor(vm, Sels(4, (1, 5), (2, 5), (3, 5), (4, 5), (5, 5), (6, 5), (7, 5)));

            Run(CoreNavigationCommands.CursorColumnSelectRight, vm);
            AssertCursor(vm, Sels(4, (1, 6), (2, 6), (3, 6), (4, 6), (5, 6), (6, 6), (7, 6)));

            // 10 times
            Run(CoreNavigationCommands.CursorColumnSelectRight, vm, 10);
            AssertCursor(vm, Sels(4, (1, 16), (2, 16), (3, 16), (4, 16), (5, 16), (6, 16), (7, 16)));

            // 10 times
            Run(CoreNavigationCommands.CursorColumnSelectRight, vm, 10);
            AssertCursor(vm, Sels(4, (1, 26), (2, 26), (3, 26), (4, 26), (5, 26), (6, 26), (7, 26)));

            // 2 times => reaching the ending of lines 1 and 2
            Run(CoreNavigationCommands.CursorColumnSelectRight, vm, 2);
            AssertCursor(vm, Sels(4, (1, 28), (2, 28), (3, 28), (4, 28), (5, 28), (6, 28), (7, 28)));

            // 4 times => reaching the ending of line 3
            Run(CoreNavigationCommands.CursorColumnSelectRight, vm, 4);
            AssertCursor(vm, Sels(4, (1, 28), (2, 28), (3, 32), (4, 32), (5, 32), (6, 32), (7, 32)));

            // 2 times => reaching the ending of line 4
            Run(CoreNavigationCommands.CursorColumnSelectRight, vm, 2);
            AssertCursor(vm, Sels(4, (1, 28), (2, 28), (3, 32), (4, 34), (5, 34), (6, 34), (7, 34)));

            // 1 time => reaching the ending of line 7
            Run(CoreNavigationCommands.CursorColumnSelectRight, vm);
            AssertCursor(vm, Sels(4, (1, 28), (2, 28), (3, 32), (4, 34), (5, 35), (6, 35), (7, 35)));

            // 3 times => reaching the ending of lines 5 & 6
            Run(CoreNavigationCommands.CursorColumnSelectRight, vm, 3);
            AssertCursor(vm, Sels(4, (1, 28), (2, 28), (3, 32), (4, 34), (5, 37), (6, 37), (7, 35)));

            // cannot go anywhere anymore
            Run(CoreNavigationCommands.CursorColumnSelectRight, vm);
            AssertCursor(vm, Sels(4, (1, 28), (2, 28), (3, 32), (4, 34), (5, 37), (6, 37), (7, 35)));

            // cannot go anywhere anymore even if we insist
            Run(CoreNavigationCommands.CursorColumnSelectRight, vm, 4);
            AssertCursor(vm, Sels(4, (1, 28), (2, 28), (3, 32), (4, 34), (5, 37), (6, 37), (7, 35)));

            // can easily go back
            Run(CoreNavigationCommands.CursorColumnSelectLeft, vm);
            AssertCursor(vm, Sels(4, (1, 28), (2, 28), (3, 32), (4, 34), (5, 36), (6, 36), (7, 35)));
        });

    [Fact]
    public void ColumnSelect_OverTabsAndSpaces_SelectsTheSameVisibleColumns() =>
        TestCodeEditor.WithTestCodeEditor(["\tab", "    cd"], editor =>
        {
            ViewModel vm = editor.ViewModel;

            ColumnSelectResult result = ColumnSelection.ColumnSelect(vm.CursorConfig, vm, 1, 4, 2, 6);

            Assert.Equal(2, result.ViewStates.Count);
            Assert.Equal(new Selection(1, 2, 1, 4), result.ViewStates[0].Selection);
            Assert.Equal(new Selection(2, 5, 2, 7), result.ViewStates[1].Selection);
            Assert.False(result.Reversed);
        });
}
