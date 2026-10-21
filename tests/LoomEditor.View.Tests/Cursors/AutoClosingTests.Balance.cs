// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public partial class AutoClosingTests
{
    // Monaco: 'issue #6841: Auto closing brackets should balance brackets'
    [Fact]
    public void Issue6841_AutoClosingBracketsShouldBalanceBrackets()
    {
        using var languages = new EditorControllerLanguages();
        const string BalancedId = "balancedAutoClosingLanguage";
        languages.Services.RegisterLanguage(BalancedId);
        languages.Services.Add(languages.Services.LanguageConfigurationRegistry.Register(BalancedId, new LanguageConfiguration
        {
            Brackets = [new("{", "}"), new("[", "]"), new("(", ")")],
            AutoClosingPairs = [new("{", "}"), new("[", "]"), new("(", ")")],
        }));

        languages.UsingCursor(new CursorOpts([string.Empty], BalancedId), (editor, model, viewModel) =>
        {
            model.BracketPairs.GetBracketsInRange(model.FullModelRange).ToArray();
            (string Text, Selection Selection, string Type, string Expected)[] testCases =
            [
                ("\n}", new Selection(1, 1, 1, 1), "{", "{\n}"),
                (string.Empty, new Selection(1, 1, 1, 1), "{", "{}"),
                ("{}", new Selection(1, 2, 1, 2), "{", "{{}}"),
                ("{\n", new Selection(2, 1, 2, 1), "{", "{\n{}"),
                ("\n]", new Selection(1, 1, 1, 1), "{", "{}\n]"),
                (")\n", new Selection(2, 1, 2, 1), "(", ")\n()"),
                ("function foo() {\n\n}", new Selection(2, 1, 2, 1), "{", "function foo() {\n{}\n}"),
                ("someFunction);", new Selection(1, 13, 1, 13), "(", "someFunction();"),
                ("someFunction;", new Selection(1, 13, 1, 13), "(", "someFunction();"),
                ("someFunctionsomeParam);", new Selection(1, 13, 1, 13), "(", "someFunction(someParam);"),
            ];
            var actual = new List<string>();
            foreach ((string text, Selection selection, string type, _) in testCases)
            {
                model.SetValue(text);
                viewModel.SetSelections("test", [selection]);
                viewModel.Type(type, "keyboard");
                actual.Add(model.GetValue());
            }

            Assert.Equal(testCases.Select(t => t.Expected), actual);
        });

        languages.UsingCursor(new CursorOpts([string.Empty, "}", string.Empty], BalancedId), (editor, model, viewModel) =>
        {
            model.BracketPairs.GetBracketsInRange(model.FullModelRange).ToArray();
            viewModel.SetSelections("test", [new Selection(1, 1, 1, 1), new Selection(3, 1, 3, 1)]);
            viewModel.Type("{", "keyboard");
            Assert.Equal("{\n}\n{", model.GetValue());
        });

        languages.UsingCursor(
            new CursorOpts([string.Empty, ")"], BalancedId, EditorOpts: new EditorCursorOptions { AutoClosingBrackets = EditorAutoClosingStrategy.Always }),
            (editor, model, viewModel) =>
            {
                viewModel.Type("(", "keyboard");
                Assert.Equal("()\n)", model.GetValue());
            });

        languages.Services.Add(languages.Services.LanguageConfigurationRegistry.Register(LanguageId, new LanguageConfiguration
        {
            Brackets = [new("{", "}")],
        }));
        languages.SetupAutoClosingLanguageTokenization();
        languages.UsingCursor(new CursorOpts([string.Empty, "\"}\""], LanguageId), (editor, model, viewModel) =>
        {
            viewModel.Type("{", "keyboard");
            Assert.Equal("{}\n\"}\"", model.GetValue());
        });

        languages.UsingCursor(new CursorOpts([string.Empty, "\"}\"", "// }"], LanguageId), (editor, model, viewModel) =>
        {
            model.Tokenization.ForceTokenization(model.LineCount);
            model.BracketPairs.GetBracketsInRange(model.FullModelRange).ToArray();
            viewModel.Type("{", "keyboard");
            Assert.Equal("{}\n\"}\"\n// }", model.GetValue());
        });
    }
}
