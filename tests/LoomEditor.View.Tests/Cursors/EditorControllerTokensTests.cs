// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Events;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// The tests that type in the languages of EditorControllerLanguages are ported by P5-39 … P5-46.
public class EditorControllerTokensTests
{
    private sealed class EmptyTokensSupport : ITokenizationSupport
    {
        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) => new([], state);
    }

    // Monaco: 'issue #46314: ViewModel is out of sync with Model!'
    [Fact]
    public void Issue46314ViewModelIsOutOfSyncWithModel()
    {
        using var languages = new EditorControllerLanguages();
        const string LanguageId = "modelModeTest1";
        languages.Services.RegisterLanguage(LanguageId);
        languages.Services.Add(languages.Services.TokenizationRegistry.Register(LanguageId, new EmptyTokensSupport()));
        TextModel model = languages.CreateTextModel("Just text", LanguageId);

        EditorControllerLanguages.WithTestCodeEditor(model, null, (editor1, cursor1) =>
        {
            EditorControllerLanguages.WithTestCodeEditor(model, null, (editor2, cursor2) =>
            {
                void OnEvent(object? sender, OutgoingViewModelEvent e)
                {
                    if (e is CursorStateChangedEvent)
                    {
                        model.Tokenization.TokenizeIfCheap(1);
                    }
                }

                cursor1.EventDispatcher.OnEvent += OnEvent;
                model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "-")]);
                cursor1.EventDispatcher.OnEvent -= OnEvent;

                Assert.Equal("-Just text", model.GetLineContent(1));
            });
        });
    }

    [Fact]
    public void ExtractAutoClosingSpecialColumns_AnnotatedLine_MarksColumns()
    {
        AutoClosingColumnType[] columns = EditorControllerLanguages.ExtractAutoClosingSpecialColumns(4, "a|b!c");

        Assert.Equal(
            [AutoClosingColumnType.Normal, AutoClosingColumnType.Normal, AutoClosingColumnType.Special1, AutoClosingColumnType.Special2, AutoClosingColumnType.Normal],
            columns);
    }

    [Fact]
    public void AutoClosingLanguageTokenizationSupport_StringAndComment_GivesStandardTokenTypes()
    {
        using var languages = new EditorControllerLanguages();
        languages.SetupAutoClosingLanguageTokenization();
        TextModel model = languages.CreateTextModel("var a = 'x'; // c", EditorControllerLanguages.AutoClosingLanguageId);

        model.Tokenization.ForceTokenization(1);
        LineTokens tokens = model.Tokenization.GetLineTokens(1);

        Assert.Equal(4, tokens.GetCount());
        Assert.Equal(
            [StandardTokenType.Other, StandardTokenType.String, StandardTokenType.Other, StandardTokenType.Comment],
            Enumerable.Range(0, 4).Select(tokens.GetStandardTokenType));
    }

    [Fact]
    public void CreateTextModel_WithLanguageId_SetsLanguageOfModel()
    {
        using var languages = new EditorControllerLanguages();

        TextModel model = languages.CreateTextModel("text", EditorControllerLanguages.ElectricCharLanguageId);

        Assert.Equal(EditorControllerLanguages.ElectricCharLanguageId, model.Tokenization.LanguageId);
    }
}
