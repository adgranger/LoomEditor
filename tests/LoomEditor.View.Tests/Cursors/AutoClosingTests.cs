// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.EditorControllerLanguages;

namespace LoomEditor.View.Tests.Cursors;

public partial class AutoClosingTests
{
    private const string LanguageId = AutoClosingLanguageId;

    private static readonly string[] SampleLines =
    [
        "var a = [];",
        "var b = `asd`;",
        "var c = 'asd';",
        "var d = \"asd\";",
        "var e = /*3*/\t3;",
        "var f = /** 3 */3;",
        "var g = (3+5);",
        "var h = { a: 'value' };",
    ];

    // Monaco: the loop of the 'autoClosingPairs - ...' tests; the tokenization is forced before every column.
    private static void ForEachColumn(TextModel model, string[] annotatedLines, Action<int, int, AutoClosingColumnType> body)
    {
        for (int i = 0; i < annotatedLines.Length; i++)
        {
            int lineNumber = i + 1;
            AutoClosingColumnType[] autoCloseColumns = ExtractAutoClosingSpecialColumns(model.GetLineMaxColumn(lineNumber), annotatedLines[i]);
            for (int column = 1; column < autoCloseColumns.Length; column++)
            {
                model.Tokenization.ForceTokenization(lineNumber);
                body(lineNumber, column, autoCloseColumns[column]);
            }
        }
    }

    private static void AssertParenCloses(TextModel model, ViewModel viewModel, int lineNumber, int column, AutoClosingColumnType type)
    {
        if (type == AutoClosingColumnType.Special1)
        {
            AssertType(model, viewModel, lineNumber, column, "(", "()", $"auto closes @ ({lineNumber}, {column})");
        }
        else
        {
            AssertType(model, viewModel, lineNumber, column, "(", "(", $"does not auto close @ ({lineNumber}, {column})");
        }
    }

    // Monaco: 'issue #61070: backtick (`) should auto-close after a word character'
    [Fact]
    public void Issue61070_BacktickShouldAutoCloseAfterAWordCharacter()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["const markup = highlight"], LanguageId), (editor, model, viewModel) =>
        {
            model.Tokenization.ForceTokenization(1);
            AssertType(model, viewModel, 1, 25, "`", "``", "auto closes ` @ (1, 25)");
        });
    }

    // Monaco: 'issue #132912: quotes should not auto-close if they are closing a string'
    [Fact]
    public void Issue132912_QuotesShouldNotAutoCloseIfTheyAreClosingAString()
    {
        using var languages = new EditorControllerLanguages();
        languages.SetupAutoClosingLanguageTokenization();
        TextModel model = languages.CreateTextModel("const t2 = `something ${t1}", LanguageId);
        WithTestCodeEditor(model, null, (editor, viewModel) =>
        {
            model.Tokenization.ForceTokenization(1);
            AssertType(model, viewModel, 1, 28, "`", "`", "does not auto close ` @ (1, 28)");
        });
    }

    // Monaco: 'autoClosingPairs - open parens: default'
    [Fact]
    public void OpenParens_Default()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(SampleLines, LanguageId), (editor, model, viewModel) =>
        {
            string[] autoClosePositions =
            [
                "var| a| |=| [|]|;|",
                "var| b| |=| |`asd|`|;|",
                "var| c| |=| |'asd|'|;|",
                "var| d| |=| |\"asd|\"|;|",
                "var| e| |=| /*3*/|\t3|;|",
                "var| f| |=| /**| 3| */3|;|",
                "var| g| |=| (3+5|)|;|",
                "var| h| |=| {| a|:| |'value|'| |}|;|",
            ];
            ForEachColumn(model, autoClosePositions, (lineNumber, column, type) => AssertParenCloses(model, viewModel, lineNumber, column, type));
        });
    }

    // Monaco: 'autoClosingPairs - open parens: whitespace'
    [Fact]
    public void OpenParens_Whitespace()
    {
        using var languages = new EditorControllerLanguages();
        var opts = new CursorOpts(SampleLines, LanguageId, EditorOpts: new EditorCursorOptions { AutoClosingBrackets = EditorAutoClosingStrategy.BeforeWhitespace });
        languages.UsingCursor(opts, (editor, model, viewModel) =>
        {
            string[] autoClosePositions =
            [
                "var| a| =| [|];|",
                "var| b| =| `asd`;|",
                "var| c| =| 'asd';|",
                "var| d| =| \"asd\";|",
                "var| e| =| /*3*/|\t3;|",
                "var| f| =| /**| 3| */3;|",
                "var| g| =| (3+5|);|",
                "var| h| =| {| a:| 'value'| |};|",
            ];
            ForEachColumn(model, autoClosePositions, (lineNumber, column, type) => AssertParenCloses(model, viewModel, lineNumber, column, type));
        });
    }

    // Monaco: 'autoClosingPairs - open parens disabled/enabled open quotes enabled/disabled'
    [Fact]
    public void OpenParensDisabledEnabled_OpenQuotesEnabledDisabled()
    {
        using var languages = new EditorControllerLanguages();
        var first = new CursorOpts(
            ["var a = [];"],
            LanguageId,
            EditorOpts: new EditorCursorOptions { AutoClosingBrackets = EditorAutoClosingStrategy.BeforeWhitespace, AutoClosingQuotes = EditorAutoClosingStrategy.Never });
        languages.UsingCursor(first, (editor, model, viewModel) =>
        {
            ForEachColumn(model, ["var| a| =| [|];|"], (lineNumber, column, type) =>
            {
                AssertParenCloses(model, viewModel, lineNumber, column, type);
                AssertType(model, viewModel, lineNumber, column, "'", "'", $"does not auto close @ ({lineNumber}, {column})");
            });
        });

        var second = new CursorOpts(
            ["var b = [];"],
            LanguageId,
            EditorOpts: new EditorCursorOptions { AutoClosingBrackets = EditorAutoClosingStrategy.Never, AutoClosingQuotes = EditorAutoClosingStrategy.BeforeWhitespace });
        languages.UsingCursor(second, (editor, model, viewModel) =>
        {
            ForEachColumn(model, ["var b =| [|];|"], (lineNumber, column, type) =>
            {
                if (type == AutoClosingColumnType.Special1)
                {
                    AssertType(model, viewModel, lineNumber, column, "'", "''", $"auto closes @ ({lineNumber}, {column})");
                }
                else
                {
                    AssertType(model, viewModel, lineNumber, column, "'", "'", $"does not auto close @ ({lineNumber}, {column})");
                }

                AssertType(model, viewModel, lineNumber, column, "(", "(", $"does not auto close @ ({lineNumber}, {column})");
            });
        });
    }

    // Monaco: 'autoClosingPairs - configurable open parens'
    [Fact]
    public void ConfigurableOpenParens()
    {
        using var languages = new EditorControllerLanguages();
        languages.SetAutoClosingLanguageEnabledSet("abc");
        var opts = new CursorOpts(SampleLines, LanguageId, EditorOpts: new EditorCursorOptions { AutoClosingBrackets = EditorAutoClosingStrategy.LanguageDefined });
        languages.UsingCursor(opts, (editor, model, viewModel) =>
        {
            string[] autoClosePositions =
            [
                "v|ar |a = [|];|",
                "v|ar |b = `|asd`;|",
                "v|ar |c = '|asd';|",
                "v|ar d = \"|asd\";|",
                "v|ar e = /*3*/\t3;|",
                "v|ar f = /** 3| */3;|",
                "v|ar g = (3+5|);|",
                "v|ar h = { |a: 'v|alue' |};|",
            ];
            ForEachColumn(model, autoClosePositions, (lineNumber, column, type) => AssertParenCloses(model, viewModel, lineNumber, column, type));
        });
    }

    // Monaco: 'autoClosingPairs - auto-pairing can be disabled'
    [Fact]
    public void AutoPairingCanBeDisabled()
    {
        using var languages = new EditorControllerLanguages();
        var opts = new CursorOpts(
            SampleLines,
            LanguageId,
            EditorOpts: new EditorCursorOptions { AutoClosingBrackets = EditorAutoClosingStrategy.Never, AutoClosingQuotes = EditorAutoClosingStrategy.Never });
        languages.UsingCursor(opts, (editor, model, viewModel) =>
        {
            ForEachColumn(model, SampleLines, (lineNumber, column, type) =>
            {
                if (type == AutoClosingColumnType.Special1)
                {
                    AssertType(model, viewModel, lineNumber, column, "(", "()", $"auto closes @ ({lineNumber}, {column})");
                    AssertType(model, viewModel, lineNumber, column, "\"", "\"\"", $"auto closes @ ({lineNumber}, {column})");
                }
                else
                {
                    AssertType(model, viewModel, lineNumber, column, "(", "(", $"does not auto close @ ({lineNumber}, {column})");
                    AssertType(model, viewModel, lineNumber, column, "\"", "\"", $"does not auto close @ ({lineNumber}, {column})");
                }
            });
        });
    }

    // Monaco: 'autoClosingPairs - quote'
    [Fact]
    public void Quote()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(SampleLines, LanguageId), (editor, model, viewModel) =>
        {
            string[] autoClosePositions =
            [
                "var a |=| [|]|;|",
                "var b |=| `asd`|;|",
                "var c |=| 'asd'|;|",
                "var d |=| \"asd\"|;|",
                "var e |=| /*3*/|\t3;|",
                "var f |=| /**| 3 */3;|",
                "var g |=| (3+5)|;|",
                "var h |=| {| a:| 'value'| |}|;|",
            ];
            ForEachColumn(model, autoClosePositions, (lineNumber, column, type) =>
            {
                switch (type)
                {
                    case AutoClosingColumnType.Special1:
                        AssertType(model, viewModel, lineNumber, column, "'", "''", $"auto closes @ ({lineNumber}, {column})");
                        break;
                    case AutoClosingColumnType.Special2:
                        AssertType(model, viewModel, lineNumber, column, "'", "", $"over types @ ({lineNumber}, {column})");
                        break;
                    default:
                        AssertType(model, viewModel, lineNumber, column, "'", "'", $"does not auto close @ ({lineNumber}, {column})");
                        break;
                }
            });
        });
    }

    // Monaco: 'autoClosingPairs - multi-character autoclose'
    [Fact]
    public void MultiCharacterAutoclose()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts([string.Empty], LanguageId), (editor, model, viewModel) =>
        {
            model.SetValue("begi");
            viewModel.SetSelections("test", [new Selection(1, 5, 1, 5)]);
            viewModel.Type("n", "keyboard");
            Assert.Equal("beginend", model.GetLineContent(1));

            model.SetValue("/*");
            viewModel.SetSelections("test", [new Selection(1, 3, 1, 3)]);
            viewModel.Type("*", "keyboard");
            Assert.Equal("/** */", model.GetLineContent(1));
        });
    }

    // Monaco: 'issue #338693: undo after multi-character autoclose first removes only the auto-closed characters'
    [Fact]
    public void Issue338693_UndoAfterMultiCharacterAutocloseFirstRemovesOnlyTheAutoClosedCharacters()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["begi begi"], LanguageId), (editor, model, viewModel) =>
        {
            var states = new List<string>();
            void RecordState() => states.Add(model.GetLineContent(1) + " " + string.Join(" ", viewModel.GetSelections()));

            viewModel.SetSelections("test", [new Selection(1, 5, 1, 5), new Selection(1, 10, 1, 10)]);
            viewModel.Type("n", "keyboard");
            RecordState();
            Undo(editor);
            RecordState();
            Undo(editor);
            RecordState();

            Assert.Equal(
                ["beginend beginend [1,6 -> 1,6] [1,15 -> 1,15]", "begin begin [1,6 -> 1,6] [1,12 -> 1,12]", "begi begi [1,5 -> 1,5] [1,10 -> 1,10]"],
                states);
        });
    }

    // Monaco: 'issue #338693: multi-character autoclose does not insert an orphan close when a listener reentrantly removes the typed character'
    [Fact]
    public void Issue338693_MultiCharacterAutocloseDoesNotInsertAnOrphanCloseWhenAListenerReentrantlyRemovesTheTypedCharacter()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["begi"], LanguageId), (editor, model, viewModel) =>
        {
            bool isFirst = true;
            model.ContentChanged += (_, _) =>
            {
                if (isFirst)
                {
                    isFirst = false;
                    model.ApplyEdits([new TextEdit(new Range(1, 5, 1, 6), string.Empty)]);
                }
            };

            viewModel.SetSelections("test", [new Selection(1, 5, 1, 5)]);
            viewModel.Type("n", "keyboard");
            Assert.Equal("begi", model.GetLineContent(1));
        });
    }

    // Monaco: 'issue #338693: multi-character autoclose does not use stale selections after a reentrant edit'
    [Fact]
    public void Issue338693_MultiCharacterAutocloseDoesNotUseStaleSelectionsAfterAReentrantEdit()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["begi"], LanguageId), (editor, model, viewModel) =>
        {
            bool isFirst = true;
            model.ContentChanged += (_, _) =>
            {
                if (isFirst)
                {
                    isFirst = false;
                    model.ApplyEdits([new TextEdit(new Range(1, 1, 1, 1), "begin\n")]);
                }
            };

            viewModel.SetSelections("test", [new Selection(1, 5, 1, 5)]);
            viewModel.Type("n", "keyboard");
            Assert.Equal("begin\nbegin", model.GetValue());
        });
    }

    // Monaco: 'autoClosingPairs - doc comments can be turned off'
    [Fact]
    public void DocCommentsCanBeTurnedOff()
    {
        using var languages = new EditorControllerLanguages();
        var opts = new CursorOpts([string.Empty], LanguageId, EditorOpts: new EditorCursorOptions { AutoClosingComments = EditorAutoClosingStrategy.Never });
        languages.UsingCursor(opts, (editor, model, viewModel) =>
        {
            model.SetValue("/*");
            viewModel.SetSelections("test", [new Selection(1, 3, 1, 3)]);
            viewModel.Type("*", "keyboard");
            Assert.Equal("/**", model.GetLineContent(1));
        });
    }

    // Monaco: 'issue #72177: multi-character autoclose with conflicting patterns'
    [Fact]
    public void Issue72177_MultiCharacterAutocloseWithConflictingPatterns()
    {
        using var languages = new EditorControllerLanguages();
        const string MultiCharLanguageId = "autoClosingModeMultiChar";
        languages.Services.RegisterLanguage(MultiCharLanguageId);
        languages.Services.Add(languages.Services.LanguageConfigurationRegistry.Register(MultiCharLanguageId, new LanguageConfiguration
        {
            AutoClosingPairs =
            [
                new("(", ")"),
                new("(*", "*)"),
                new("<@", "@>"),
                new("<@@", "@@>"),
            ],
        }));

        languages.UsingCursor(new CursorOpts([string.Empty], MultiCharLanguageId), (editor, model, viewModel) =>
        {
            viewModel.Type("(", "keyboard");
            Assert.Equal("()", model.GetLineContent(1));
            viewModel.Type("*", "keyboard");
            Assert.Equal("(**)", model.GetLineContent(1)); // doesn't add entire close when already closed substring is there

            model.SetValue("(");
            viewModel.SetSelections("test", [new Selection(1, 2, 1, 2)]);
            viewModel.Type("*", "keyboard");
            Assert.Equal("(**)", model.GetLineContent(1)); // does add entire close if not already there

            model.SetValue(string.Empty);
            viewModel.Type("<@", "keyboard");
            Assert.Equal("<@@>", model.GetLineContent(1));
            viewModel.Type("@", "keyboard");
            Assert.Equal("<@@@@>", model.GetLineContent(1)); // autocloses when before multi-character closing brace
            viewModel.Type("(", "keyboard");
            Assert.Equal("<@@()@@>", model.GetLineContent(1)); // autocloses when before multi-character closing brace
        });
    }

    // Monaco: 'issue #55314: Do not auto-close when ending with open'
    [Fact]
    public void Issue55314_DoNotAutoCloseWhenEndingWithOpen()
    {
        using var languages = new EditorControllerLanguages();
        const string ElectricModeId = "myElectricMode";
        languages.Services.RegisterLanguage(ElectricModeId);
        languages.Services.Add(languages.Services.LanguageConfigurationRegistry.Register(ElectricModeId, new LanguageConfiguration
        {
            AutoClosingPairs =
            [
                new("{", "}"),
                new("[", "]"),
                new("(", ")"),
                new("'", "'", ["string", "comment"]),
                new("\"", "\"", ["string"]),
                new("B\"", "\"", ["string", "comment"]),
                new("`", "`", ["string", "comment"]),
                new("/**", " */", ["string"]),
            ],
        }));

        languages.UsingCursor(new CursorOpts(["little goat", "little LAMB", "little sheep", "Big LAMB"], ElectricModeId), (editor, model, viewModel) =>
        {
            const string Message = "does not double quote when ending with open";
            (int Line, int Column)[] cases = [(1, 4), (2, 4), (3, 4), (4, 2), (4, 3)];
            foreach ((int line, int column) in cases)
            {
                model.Tokenization.ForceTokenization(model.LineCount);
                AssertType(model, viewModel, line, column, "\"", "\"", Message);
            }
        });
    }

    // Monaco: 'issue #27937: Trying to add an item to the front of a list is cumbersome'
    [Fact]
    public void Issue27937_TryingToAddAnItemToTheFrontOfAListIsCumbersome()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["var arr = [\"b\", \"c\"];"], LanguageId), (editor, model, viewModel) =>
        {
            AssertType(model, viewModel, 1, 12, "\"", "\"", "does not over type and will not auto close");
        });
    }

    // Monaco: 'issue #25658 - Do not auto-close single/double quotes after word characters'
    [Fact]
    public void Issue25658_DoNotAutoCloseSingleDoubleQuotesAfterWordCharacters()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts([string.Empty], LanguageId), (editor, model, viewModel) =>
        {
            void TypeCharacters(string chars)
            {
                foreach (char c in chars)
                {
                    viewModel.Type(c.ToString(), "keyboard");
                }
            }

            void NewLine(int previousLineNumber)
            {
                viewModel.SetSelections("test", [new Selection(previousLineNumber, 1000, previousLineNumber, 1000)]);
                TypeCharacters("\n");
                model.Tokenization.ForceTokenization(model.LineCount);
            }

            // First gif
            model.Tokenization.ForceTokenization(model.LineCount);
            TypeCharacters("teste1 = teste' ok");
            Assert.Equal("teste1 = teste' ok", model.GetLineContent(1));

            NewLine(1);
            TypeCharacters("teste2 = teste 'ok");
            Assert.Equal("teste2 = teste 'ok'", model.GetLineContent(2));

            NewLine(2);
            TypeCharacters("teste3 = teste\" ok");
            Assert.Equal("teste3 = teste\" ok", model.GetLineContent(3));

            NewLine(3);
            TypeCharacters("teste4 = teste \"ok");
            Assert.Equal("teste4 = teste \"ok\"", model.GetLineContent(4));

            // Second gif
            NewLine(4);
            TypeCharacters("teste '");
            Assert.Equal("teste ''", model.GetLineContent(5));

            NewLine(5);
            TypeCharacters("teste \"");
            Assert.Equal("teste \"\"", model.GetLineContent(6));

            NewLine(6);
            TypeCharacters("teste'");
            Assert.Equal("teste'", model.GetLineContent(7));

            NewLine(7);
            TypeCharacters("teste\"");
            Assert.Equal("teste\"", model.GetLineContent(8));
        });
    }
}
