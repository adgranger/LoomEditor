using LoomEditor.Core.Decorations;
using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

public class DiffDecorationsTests
{
    private sealed class Target : IDiffDecorationTarget
    {
        public IReadOnlyList<ModelDeltaDecoration>? Last { get; private set; }

        public void SetDecorations(IReadOnlyList<ModelDeltaDecoration> decorations) => Last = decorations;
    }

    [Fact]
    public void Update_SetsBothSides()
    {
        Target o = new(), m = new();
        var state = new DiffState([new DiffMapping(new DetailedLineRangeMapping(new LineRange(1, 2), new LineRange(1, 2), null))], [], false, false);
        new DiffDecorations(o, m).Update(state, new DiffEditorOptions(), 10, 10);
        Assert.NotEmpty(o.Last!);
        Assert.NotEmpty(m.Last!);
    }

    [Fact]
    public void Update_NullDiff_ClearsBoth()
    {
        Target o = new(), m = new();
        var d = new DiffDecorations(o, m);
        var state = new DiffState([new DiffMapping(new DetailedLineRangeMapping(new LineRange(1, 2), new LineRange(1, 2), null))], [], false, false);
        d.Update(state, new DiffEditorOptions(), 10, 10);
        d.Update(null, new DiffEditorOptions(), 10, 10);
        Assert.Empty(o.Last!);
        Assert.Empty(m.Last!);
    }
}
