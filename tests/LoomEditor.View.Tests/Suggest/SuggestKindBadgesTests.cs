using LoomEditor.Core.Languages.Features;
using LoomEditor.View.Suggest;

namespace LoomEditor.View.Tests.Suggest;

public class SuggestKindBadgesTests
{
    public static TheoryData<CompletionItemKind, string> Labels => new()
    {
        { CompletionItemKind.Method, "Method" }, { CompletionItemKind.Function, "Function" },
        { CompletionItemKind.Constructor, "Constructor" }, { CompletionItemKind.Field, "Field" },
        { CompletionItemKind.Variable, "Variable" }, { CompletionItemKind.Class, "Class" },
        { CompletionItemKind.Struct, "Struct" }, { CompletionItemKind.Interface, "Interface" },
        { CompletionItemKind.Module, "Module" }, { CompletionItemKind.Property, "Property" },
        { CompletionItemKind.Event, "Event" }, { CompletionItemKind.Operator, "Operator" },
        { CompletionItemKind.Unit, "Unit" }, { CompletionItemKind.Value, "Value" },
        { CompletionItemKind.Constant, "Constant" }, { CompletionItemKind.Enum, "Enum" },
        { CompletionItemKind.EnumMember, "Enum Member" }, { CompletionItemKind.Keyword, "Keyword" },
        { CompletionItemKind.Text, "Text" }, { CompletionItemKind.Color, "Color" },
        { CompletionItemKind.File, "File" }, { CompletionItemKind.Reference, "Reference" },
        { CompletionItemKind.Customcolor, "Custom Color" }, { CompletionItemKind.Folder, "Folder" },
        { CompletionItemKind.TypeParameter, "Type Parameter" }, { CompletionItemKind.User, "User" },
        { CompletionItemKind.Issue, "Issue" }, { CompletionItemKind.Tool, "Tool" },
        { CompletionItemKind.Snippet, "Snippet" },
    };

    // Monaco: CompletionItemKinds.toLabel
    [Theory]
    [MemberData(nameof(Labels))]
    public void ToLabel_AllKinds(CompletionItemKind kind, string expected) => Assert.Equal(expected, SuggestKindBadges.ToLabel(kind));

    // Monaco: CompletionItemKinds.toLabel
    [Fact]
    public void ToLabel_Unknown_Empty() => Assert.Equal("", SuggestKindBadges.ToLabel((CompletionItemKind)999));

    [Fact]
    public void Glyph_AllKinds_TwoLettersAndDistinct()
    {
        var glyphs = Enum.GetValues<CompletionItemKind>().Select(SuggestKindBadges.Glyph).ToList();
        Assert.Equal(29, glyphs.Count);
        Assert.All(glyphs, g => Assert.Equal(2, g.Length));
        Assert.Equal(glyphs.Count, glyphs.Distinct().Count());
    }

    [Fact]
    public void Glyph_Spot()
    {
        Assert.Equal("Me", SuggestKindBadges.Glyph(CompletionItemKind.Method));
        Assert.Equal("EM", SuggestKindBadges.Glyph(CompletionItemKind.EnumMember));
        Assert.Equal("Sn", SuggestKindBadges.Glyph(CompletionItemKind.Snippet));
    }
}
