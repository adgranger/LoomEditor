using LoomEditor.Core.Snippets;

namespace LoomEditor.Core.Tests.Snippets;

// The two templates of the Gallery buttons (samples/LoomEditor.Gallery/Phase10Samples.cs): copied here to keep the samples valid without UI.
public class GallerySnippetTemplatesTests
{
    private const string ForLoop = "for (let ${1:i} = 0; $1 < ${2:array}.length; $1++) {\n\tconst ${3:element} = $2[$1];\n\t$0\n}";
    private const string LogSelection = "console.log('${TM_SELECTED_TEXT}', ${TM_SELECTED_TEXT});$0";

    [Fact]
    public void ForLoop_HasThreeTabStopsWithMirrorsAndAFinalTabStop()
    {
        TextmateSnippet snippet = new SnippetParser().Parse(ForLoop, true);

        Dictionary<int, int> countByIndex = snippet.Placeholders.GroupBy(placeholder => placeholder.Index).ToDictionary(group => group.Key, group => group.Count());

        Assert.Equal(new Dictionary<int, int> { [0] = 1, [1] = 4, [2] = 2, [3] = 1 }, countByIndex);
        Assert.True(snippet.Placeholders.Single(placeholder => placeholder.Index == 0).IsFinalTabstop);
        Assert.Contains("for (let i = 0; i < array.length; i++) {", snippet.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void LogSelection_UsesTheSelectedTextVariableTwiceAndHasOnlyTheFinalTabStop()
    {
        TextmateSnippet snippet = new SnippetParser().Parse(LogSelection, true);

        int variables = 0;
        snippet.Walk(marker =>
        {
            if (marker is Variable { Name: "TM_SELECTED_TEXT" })
            {
                variables++;
            }

            return true;
        });

        Assert.Equal(2, variables);
        Placeholder only = Assert.Single(snippet.Placeholders);
        Assert.True(only.IsFinalTabstop);
    }
}
