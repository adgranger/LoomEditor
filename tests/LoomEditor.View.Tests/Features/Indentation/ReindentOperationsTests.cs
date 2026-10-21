using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.View.Features.Indentation;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Indentation;

public class ReindentOperationsTests
{
    private static List<IdentifiedEditOperation> Reindent(string text, string? languageId = TestLanguages.TypeScript)
    {
        using var services = new TestEditorServices();
        if (languageId is not null)
        {
            services.RegisterLanguage(languageId);
            services.Add(TestLanguages.RegisterLanguageConfiguration(services.LanguageConfigurationRegistry, languageId));
        }

        TextModel model = services.CreateTextModel(text, languageId);
        return ReindentOperations.GetReindentEditOperations(model, services.LanguageConfigurationRegistry, 1, model.LineCount);
    }

    [Fact]
    public void GetReindentEditOperations_JavaScriptRules_IndentsTheBodyOfABlock()
    {
        List<IdentifiedEditOperation> edits = Reindent("function f() {\nfoo();\n}");

        IdentifiedEditOperation edit = Assert.Single(edits);
        Assert.Equal(new LoomEditor.Core.Text.Range(2, 1, 2, 1), edit.Range);
        Assert.Equal("    ", edit.Text);
        Assert.True(edit.ForceMoveMarkers);
    }

    [Fact]
    public void GetReindentEditOperations_NoIndentationRules_ReturnsNoEdit() =>
        Assert.Empty(Reindent("a\n b\nc", languageId: null));

    [Fact]
    public void GetReindentEditOperations_EmptyModel_ReturnsNoEdit() =>
        Assert.Empty(Reindent(string.Empty));

    [Fact]
    public void GetReindentEditOperations_ClosingBraceLine_IsOutdented()
    {
        List<IdentifiedEditOperation> edits = Reindent("{\n    a\n    }");

        IdentifiedEditOperation edit = Assert.Single(edits);
        Assert.Equal(new LoomEditor.Core.Text.Range(3, 1, 3, 5), edit.Range);
        Assert.Equal(string.Empty, edit.Text);
    }

    [Fact]
    public void IndentUtils_GenerateIndent_NegativeCount_ReturnsEmpty() =>
        Assert.Equal(string.Empty, IndentUtils.GenerateIndent(-3, 4, insertSpaces: true));

    [Fact]
    public void IndentUtils_GenerateIndent_TabsThenSpaces_WhenNotInsertSpaces() =>
        Assert.Equal("\t\t  ", IndentUtils.GenerateIndent(10, 4, insertSpaces: false));

    [Fact]
    public void GetSpaceCnt_TabCountsTabSize() =>
        Assert.Equal(6, IndentUtils.GetSpaceCnt("\t  ", 4));
}
