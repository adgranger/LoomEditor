// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.EditorControllerLanguages;

namespace LoomEditor.View.Tests.Cursors;

public partial class AutoClosingTests
{
    private static readonly string[] OvertypeLines = [string.Empty, "y=();"];

    // Monaco: 'issue #37315 - overtypes only those characters that it inserted'
    [Fact]
    public void Issue37315_OvertypesOnlyThoseCharactersThatItInserted()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(OvertypeLines, LanguageId), (editor, model, viewModel) =>
        {
            AssertCursor(viewModel, new Position(1, 1));

            viewModel.Type("x=(", "keyboard");
            Assert.Equal("x=()", model.GetLineContent(1));

            viewModel.Type("asd", "keyboard");
            Assert.Equal("x=(asd)", model.GetLineContent(1));

            // overtype!
            viewModel.Type(")", "keyboard");
            Assert.Equal("x=(asd)", model.GetLineContent(1));

            // do not overtype!
            viewModel.SetSelections("test", [new Selection(2, 4, 2, 4)]);
            viewModel.Type(")", "keyboard");
            Assert.Equal("y=());", model.GetLineContent(2));
        });
    }

    // Monaco: 'issue #37315 - stops overtyping once cursor leaves area'
    [Fact]
    public void Issue37315_StopsOvertypingOnceCursorLeavesArea()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(OvertypeLines, LanguageId), (editor, model, viewModel) =>
        {
            AssertCursor(viewModel, new Position(1, 1));

            viewModel.Type("x=(", "keyboard");
            Assert.Equal("x=()", model.GetLineContent(1));

            viewModel.SetSelections("test", [new Selection(1, 5, 1, 5)]);
            viewModel.Type(")", "keyboard");
            Assert.Equal("x=())", model.GetLineContent(1));
        });
    }

    // Monaco: 'issue #37315 - it overtypes only once'
    [Fact]
    public void Issue37315_ItOvertypesOnlyOnce()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(OvertypeLines, LanguageId), (editor, model, viewModel) =>
        {
            AssertCursor(viewModel, new Position(1, 1));

            viewModel.Type("x=(", "keyboard");
            Assert.Equal("x=()", model.GetLineContent(1));

            viewModel.Type(")", "keyboard");
            Assert.Equal("x=()", model.GetLineContent(1));

            viewModel.SetSelections("test", [new Selection(1, 4, 1, 4)]);
            viewModel.Type(")", "keyboard");
            Assert.Equal("x=())", model.GetLineContent(1));
        });
    }

    // Monaco: 'issue #37315 - it can remember multiple auto-closed instances'
    [Fact]
    public void Issue37315_ItCanRememberMultipleAutoClosedInstances()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(OvertypeLines, LanguageId), (editor, model, viewModel) =>
        {
            AssertCursor(viewModel, new Position(1, 1));

            viewModel.Type("x=(", "keyboard");
            Assert.Equal("x=()", model.GetLineContent(1));

            viewModel.Type("(", "keyboard");
            Assert.Equal("x=(())", model.GetLineContent(1));

            viewModel.Type(")", "keyboard");
            Assert.Equal("x=(())", model.GetLineContent(1));

            viewModel.Type(")", "keyboard");
            Assert.Equal("x=(())", model.GetLineContent(1));
        });
    }

    // Monaco: 'issue #205598 - retains auto-closed actions created by a reentrant decorations listener'
    [Fact]
    public void Issue205598_RetainsAutoClosedActionsCreatedByAReentrantDecorationsListener()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts([string.Empty], LanguageId), (editor, model, viewModel) =>
        {
            var results = new List<(string Value, int AutoClosedDecorations)>();
            for (int iteration = 0; iteration < 3; iteration++)
            {
                viewModel.ExecuteEdits("test", [new IdentifiedEditOperation(model.FullModelRange, string.Empty)], _ => [new Selection(1, 1, 1, 1)]);
                for (int pair = 0; pair <= iteration; pair++)
                {
                    viewModel.Type("(", "keyboard");
                }

                bool armed = true;
                void Listener(object? sender, ModelDecorationsChangedEventArgs e)
                {
                    if (armed)
                    {
                        armed = false;
                        viewModel.Type("(", "keyboard");
                    }
                }

                model.DecorationsChanged += Listener;
                int maxColumn = model.GetLineMaxColumn(1);
                viewModel.SetSelections("test", [new Selection(1, maxColumn, 1, maxColumn)]);
                model.DecorationsChanged -= Listener;
                viewModel.Type(")", "keyboard");
                viewModel.SetSelections("test", [new Selection(1, 1, 1, 1)]);
                results.Add((
                    model.GetValue(),
                    model.GetDecorationsInRange(model.FullModelRange).Count(d => d.Options.Description.StartsWith("auto-closed-", StringComparison.Ordinal))));
            }

            Assert.Equal(
                [("()()", 0), ("(())()", 0), ("((()))()", 0)],
                results);
        });
    }

    // Monaco: 'issue #118270 - auto closing deletes only those characters that it inserted'
    [Fact]
    public void Issue118270_AutoClosingDeletesOnlyThoseCharactersThatItInserted()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(OvertypeLines, LanguageId), (editor, model, viewModel) =>
        {
            AssertCursor(viewModel, new Position(1, 1));

            viewModel.Type("x=(", "keyboard");
            Assert.Equal("x=()", model.GetLineContent(1));

            viewModel.Type("asd", "keyboard");
            Assert.Equal("x=(asd)", model.GetLineContent(1));

            DeleteLeft(editor);
            DeleteLeft(editor);
            DeleteLeft(editor);
            Assert.Equal("x=()", model.GetLineContent(1));

            // delete closing char!
            DeleteLeft(editor);
            Assert.Equal("x=", model.GetLineContent(1));

            // do not delete closing char!
            viewModel.SetSelections("test", [new Selection(2, 4, 2, 4)]);
            DeleteLeft(editor);
            Assert.Equal("y=);", model.GetLineContent(2));
        });
    }

    // Monaco: 'issue #78527 - does not close quote on odd count'
    [Fact]
    public void Issue78527_DoesNotCloseQuoteOnOddCount()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["std::cout << '\"' << entryMap"], LanguageId), (editor, model, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 29, 1, 29)]);

            viewModel.Type("[", "keyboard");
            Assert.Equal("std::cout << '\"' << entryMap[]", model.GetLineContent(1));

            viewModel.Type("\"", "keyboard");
            Assert.Equal("std::cout << '\"' << entryMap[\"\"]", model.GetLineContent(1));

            viewModel.Type("a", "keyboard");
            Assert.Equal("std::cout << '\"' << entryMap[\"a\"]", model.GetLineContent(1));

            viewModel.Type("\"", "keyboard");
            Assert.Equal("std::cout << '\"' << entryMap[\"a\"]", model.GetLineContent(1));

            viewModel.Type("]", "keyboard");
            Assert.Equal("std::cout << '\"' << entryMap[\"a\"]", model.GetLineContent(1));
        });
    }

    // Monaco: 'issue #85983 - editor.autoClosingBrackets: beforeWhitespace is incorrect for Python'
    [Fact]
    public void Issue85983_AutoClosingBracketsBeforeWhitespaceIsIncorrectForPython()
    {
        using var languages = new EditorControllerLanguages();
        const string PythonModeId = "pythonMode";
        var pairs = new List<AutoClosingPairConditional>
        {
            new("{", "}"),
            new("[", "]"),
            new("(", ")"),
            new("\"", "\"", ["string"]),
        };
        foreach (string prefix in new[] { "r", "R", "u", "U", "f", "F", "b", "B" })
        {
            pairs.Add(new(prefix + "\"", "\"", ["string", "comment"]));
        }

        pairs.Add(new("'", "'", ["string", "comment"]));
        foreach (string prefix in new[] { "r", "R", "u", "U", "f", "F", "b", "B" })
        {
            pairs.Add(new(prefix + "'", "'", ["string", "comment"]));
        }

        pairs.Add(new("`", "`", ["string"]));
        languages.Services.RegisterLanguage(PythonModeId);
        languages.Services.Add(languages.Services.LanguageConfigurationRegistry.Register(PythonModeId, new LanguageConfiguration { AutoClosingPairs = pairs }));

        var opts = new CursorOpts(["foo'hello'"], PythonModeId, EditorOpts: new EditorCursorOptions { AutoClosingBrackets = EditorAutoClosingStrategy.BeforeWhitespace });
        languages.UsingCursor(opts, (editor, model, viewModel) =>
        {
            AssertType(model, viewModel, 1, 4, "(", "(", "does not auto close @ (1, 4)");
        });
    }

    // Monaco: 'issue #78833 - Add config to use old brackets/quotes overtyping'
    [Fact]
    public void Issue78833_AddConfigToUseOldBracketsQuotesOvertyping()
    {
        using var languages = new EditorControllerLanguages();
        var opts = new CursorOpts(OvertypeLines, LanguageId, EditorOpts: new EditorCursorOptions { AutoClosingOvertype = EditorAutoClosingEditStrategy.Always });
        languages.UsingCursor(opts, (editor, model, viewModel) =>
        {
            AssertCursor(viewModel, new Position(1, 1));

            viewModel.Type("x=(", "keyboard");
            Assert.Equal("x=()", model.GetLineContent(1));

            viewModel.Type(")", "keyboard");
            Assert.Equal("x=()", model.GetLineContent(1));

            viewModel.SetSelections("test", [new Selection(1, 4, 1, 4)]);
            viewModel.Type(")", "keyboard");
            Assert.Equal("x=()", model.GetLineContent(1));

            viewModel.SetSelections("test", [new Selection(2, 4, 2, 4)]);
            viewModel.Type(")", "keyboard");
            Assert.Equal("y=();", model.GetLineContent(2));
        });
    }

    // Monaco: 'issue #53357: Over typing ignores characters after backslash'
    [Fact]
    public void Issue53357_OverTypingIgnoresCharactersAfterBackslash()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["console.log();"], LanguageId), (editor, model, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 13, 1, 13)]);

            viewModel.Type("'", "keyboard");
            Assert.Equal("console.log('');", model.GetValue());

            viewModel.Type("it", "keyboard");
            Assert.Equal("console.log('it');", model.GetValue());

            viewModel.Type("\\", "keyboard");
            Assert.Equal("console.log('it\\');", model.GetValue());

            viewModel.Type("'", "keyboard");
            Assert.Equal("console.log('it\\'');", model.GetValue());
        });
    }

    // Monaco: 'issue #84998: Overtyping Brackets doesn't work after backslash'
    [Fact]
    public void Issue84998_OvertypingBracketsDoesNotWorkAfterBackslash()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts([string.Empty], LanguageId), (editor, model, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 1, 1, 1)]);

            viewModel.Type("\\", "keyboard");
            Assert.Equal("\\", model.GetValue());

            viewModel.Type("(", "keyboard");
            Assert.Equal("\\()", model.GetValue());

            viewModel.Type("abc", "keyboard");
            Assert.Equal("\\(abc)", model.GetValue());

            viewModel.Type("\\", "keyboard");
            Assert.Equal("\\(abc\\)", model.GetValue());

            viewModel.Type(")", "keyboard");
            Assert.Equal("\\(abc\\)", model.GetValue());
        });
    }

    // Monaco: 'issue #20891: All cursors should do the same thing'
    [Fact]
    public void Issue20891_AllCursorsShouldDoTheSameThing()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["var a = asd"], LanguageId), (editor, model, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 9, 1, 9), new Selection(1, 12, 1, 12)]);

            // type a `
            viewModel.Type("`", "keyboard");

            Assert.Equal("var a = `asd`", model.GetValue());
        });
    }

    // Monaco: 'All cursors should do the same thing when deleting left'
    [Fact]
    public void AllCursorsShouldDoTheSameThingWhenDeletingLeft()
    {
        using var languages = new EditorControllerLanguages();
        TextModel model = languages.CreateTextModel("var a = ()", LanguageId);
        WithTestCodeEditor(model, null, (editor, viewModel) =>
        {
            viewModel.SetSelections("test", [new Selection(1, 4, 1, 4), new Selection(1, 10, 1, 10)]);

            // delete left
            CoreEditingCommands.DeleteLeft.RunCoreEditingCommand(viewModel);

            Assert.Equal("va a = )", model.GetValue());
        });
    }
}
