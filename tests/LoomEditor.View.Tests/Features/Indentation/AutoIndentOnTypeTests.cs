// Ported from microsoft/vscode: src/vs/editor/contrib/indentation/test/browser/indentation.test.ts (the 'Auto Indent On Type' suites)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Indentation;

// Not ported: the 26 test.skip tests of the file (they fail in Monaco too) and the 7 placeholder tests
// 'temp issue because there should be at least one passing test in a suite'.
public partial class AutoIndentOnTypeTests
{
    // Monaco: setup + createTextModel(text, languageId, {}) + withTestCodeEditor(model, { autoIndent }, ...).
    private static void Run(
        string languageId,
        string text,
        EditorAutoIndentStrategy autoIndent,
        Action<TestCodeEditor, TextModel, ViewModel> test,
        StandardTokenTypeData[][]? tokens = null)
    {
        using var services = new TestEditorServices();
        services.RegisterLanguage(languageId);
        services.Add(TestLanguages.RegisterLanguageConfiguration(services.LanguageConfigurationRegistry, languageId));
        TextModel model = services.CreateTextModel(text, languageId);
        TestCodeEditor.WithTestCodeEditor(
            model,
            editor =>
            {
                if (tokens is not null)
                {
                    // Registered after the model is created, as in Monaco.
                    services.Add(TestLanguages.RegisterTokenizationSupport(services.TokenizationRegistry, services.LanguageIdCodec, tokens, languageId));
                }

                test(editor, model, editor.ViewModel);
            },
            // An empty viewport: Monaco's test editor has no view, so no line is tokenized until it is forced (isCheapToTokenize).
            new TestCodeEditorOptions(OuterWidth: 0, OuterHeight: 0, CursorOptions: EditorCursorOptions.Default with { AutoIndent = autoIndent }));
    }

    private static void SetSelection(ViewModel viewModel, Selection selection) => viewModel.SetSelections("test", [selection]);

    private static string Join(params string[] lines) => string.Join('\n', lines);
}
