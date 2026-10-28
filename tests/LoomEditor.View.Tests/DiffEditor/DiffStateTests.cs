using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

public class DiffStateTests
{
    [Fact]
    public void DiffState_FromDiffResult_WrapsMappings()
    {
        var change = new DetailedLineRangeMapping(new LineRange(2, 3), new LineRange(2, 4), null);
        var move = new MovedText(new LineRangeMapping(new LineRange(5, 6), new LineRange(1, 2)), []);
        var state = DiffState.FromDiffResult(new DocumentDiff(false, true, [change], [move]));

        Assert.Same(change, Assert.Single(state.Mappings).LineRangeMapping);
        Assert.Same(move, Assert.Single(state.MovedTexts));
        Assert.False(state.Identical);
        Assert.True(state.QuitEarly);
    }
}
