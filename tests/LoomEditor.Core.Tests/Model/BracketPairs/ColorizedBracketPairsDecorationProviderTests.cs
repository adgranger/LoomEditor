using LoomEditor.Core.Decorations;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.BracketPairs;
using LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.BracketPairs;

// No upstream test targets the provider (the bracket levels are tested by colorizedBracketsVSBrackets): all tests are new.
public class ColorizedBracketPairsDecorationProviderTests
{
    private static readonly CharacterPair[] Brackets = [new("{", "}"), new("[", "]"), new("(", ")")];

    private static TextModel Create(string text) =>
        TestLanguageModels.Create(text, TestLanguageModels.DefaultLanguageId, new LanguageConfiguration { Brackets = Brackets }).Model;

    private static string[] ClassNames(IEnumerable<ModelDecoration> decorations) =>
        [
            .. decorations
                .Where(d => d.Options.Description == "BracketPairColorization")
                .OrderBy(d => d.Range.StartLineNumber).ThenBy(d => d.Range.StartColumn)
                .Select(d => d.Options.InlineClassName!),
        ];

    [Fact]
    public void GetDecorationsInRange_NestedBrackets_ClassNamesFollowNestingLevel()
    {
        var model = Create("{ [ ( ) ] }");
        var provider = new ColorizedBracketPairsDecorationProvider(model);

        string[] classes = ClassNames(provider.GetDecorationsInRange(model.FullModelRange, 0, false));

        Assert.Equal(
            ["bracket-highlighting-0", "bracket-highlighting-1", "bracket-highlighting-2", "bracket-highlighting-2", "bracket-highlighting-1", "bracket-highlighting-0"],
            classes);
    }

    [Fact]
    public void GetDecorationsInRange_IndependentColorPool_UsesLevelOfEqualBracketType()
    {
        var model = Create("{ [ ] }");
        var provider = new ColorizedBracketPairsDecorationProvider(model);
        model.UpdateBracketPairColorizationOptions(new BracketPairColorizationOptions(IndependentColorPoolPerBracketType: true));
        provider.HandleDidChangeOptions();

        string[] classes = ClassNames(provider.GetDecorationsInRange(model.FullModelRange, 0, false));

        Assert.Equal(["bracket-highlighting-0", "bracket-highlighting-0", "bracket-highlighting-0", "bracket-highlighting-0"], classes);
    }

    [Fact]
    public void GetDecorationsInRange_UnexpectedClosingBracket_UsesUnexpectedClassName()
    {
        var model = Create("{ } }");
        var provider = new ColorizedBracketPairsDecorationProvider(model);

        string[] classes = ClassNames(provider.GetDecorationsInRange(model.FullModelRange, 0, false));

        Assert.Equal(["bracket-highlighting-0", "bracket-highlighting-0", "unexpected-closing-bracket"], classes);
    }

    [Fact]
    public void GetDecorationsInRange_Disabled_ReturnsNothing()
    {
        var model = Create("{ }");
        var provider = new ColorizedBracketPairsDecorationProvider(model);
        model.UpdateBracketPairColorizationOptions(new BracketPairColorizationOptions(Enabled: false));
        provider.HandleDidChangeOptions();

        Assert.Empty(provider.GetDecorationsInRange(model.FullModelRange, 0, false));
        Assert.Empty(provider.GetAllDecorations(0, false));
    }

    [Fact]
    public void GetAllDecorations_ReturnsTheDecorationsOfTheWholeText()
    {
        var model = Create("{\n}\n");
        var provider = new ColorizedBracketPairsDecorationProvider(model);

        Assert.Equal(2, provider.GetAllDecorations(0, false).Count);
    }

    [Fact]
    public void TextModel_GetDecorationsInRange_IncludesBracketColors()
    {
        // The text ends with a line break: Monaco's getAllDecorations stops at column 1 of the last line.
        var model = Create("{ [ ] }\n");

        Assert.Equal(4, ClassNames(model.GetDecorationsInRange(model.FullModelRange)).Length);
        Assert.Equal(4, ClassNames(model.GetAllDecorations()).Length);
        Assert.Equal(4, ClassNames(model.GetLineDecorations(1)).Length);
    }

    [Fact]
    public void UpdateBracketPairColorizationOptions_RaisesDecorationsChanged()
    {
        var model = Create("{ }");
        var changed = 0;
        model.DecorationsChanged += (_, _) => changed++;

        model.UpdateBracketPairColorizationOptions(new BracketPairColorizationOptions(Enabled: false));
        Assert.Equal(1, changed);
        Assert.Empty(ClassNames(model.GetDecorationsInRange(model.FullModelRange)));

        model.UpdateBracketPairColorizationOptions(new BracketPairColorizationOptions(Enabled: false));
        Assert.Equal(1, changed);
    }

    [Fact]
    public void Edit_ThatChangesBrackets_RaisesDecorationsChanged()
    {
        var model = Create("{ }");
        Assert.Equal(2, ClassNames(model.GetDecorationsInRange(model.FullModelRange)).Length);
        var changed = 0;
        model.DecorationsChanged += (_, _) => changed++;

        model.ApplyEdits([new TextEdit(new Range(1, 2, 1, 2), "{ ")]);

        Assert.True(changed > 0);
        Assert.Equal(3, ClassNames(model.GetDecorationsInRange(model.FullModelRange)).Length);
    }
}
