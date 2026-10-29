// Ported from microsoft/vscode: src/vs/editor/contrib/lineSelection/test/browser/lineSelection.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.LineSelection;

namespace LoomEditor.View.Tests.Features.LineSelection;

public class LineSelectionFeatureTests
{
    // Monaco: 'LineSelection' › ''
    [Fact]
    public void ExpandLineSelection_SelectsTheLineThenOneMoreEachTime()
    {
        const string Line1 = "    \tMy First Line\t ";
        const string Line5 = "1";
        string text = Line1 + "\r\n" + "\tMy Second Line" + "\n" + "    Third Line🐶" + "\n" + string.Empty + "\r\n" + Line5;

        var context = new EditorContext();
        context.SetText(text);
        new LineSelectionFeature().Attach(context);

        void Expand(Position position, Selection expected)
        {
            context.Selection = Selection.Collapsed(position);
            Run();
            Assert.Equal(expected, context.Selection);
        }

        void Run() => Assert.True(context.Commands.Execute(LineSelectionCommands.ExpandLineSelection));

        Expand(new Position(1, 1), new Selection(1, 1, 2, 1));
        Expand(new Position(1, 2), new Selection(1, 1, 2, 1));
        Expand(new Position(1, 5), new Selection(1, 1, 2, 1));
        Expand(new Position(1, 19), new Selection(1, 1, 2, 1));
        Expand(new Position(1, 20), new Selection(1, 1, 2, 1));
        Expand(new Position(1, 21), new Selection(1, 1, 2, 1));
        Run();
        Assert.Equal(new Selection(1, 1, 3, 1), context.Selection);
        Run();
        Assert.Equal(new Selection(1, 1, 4, 1), context.Selection);
        Run();
        Assert.Equal(new Selection(1, 1, 5, 1), context.Selection);
        Run();
        Assert.Equal(new Selection(1, 1, 5, Line5.Length + 1), context.Selection);
        Run();
        Assert.Equal(new Selection(1, 1, 5, Line5.Length + 1), context.Selection);
    }
}
