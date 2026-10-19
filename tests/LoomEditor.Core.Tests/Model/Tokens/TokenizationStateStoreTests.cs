using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.Tokens;

public class TokenizationStateStoreTests
{
    private sealed class TestState(int value) : IState
    {
        public int Value { get; } = value;

        public IState Clone() => new TestState(Value);

        public bool Equals(IState? other) => other is TestState s && s.Value == Value;
    }

    private static TrackingTokenizationStateStore<TestState> CreateValidStore(int lineCount)
    {
        var store = new TrackingTokenizationStateStore<TestState>(lineCount);
        for (int line = 1; line <= lineCount; line++)
        {
            store.SetEndState(line, new TestState(line));
        }

        return store;
    }

    [Fact]
    public void TrackingStore_SetEndStateChanged_InvalidatesNextLine()
    {
        TrackingTokenizationStateStore<TestState> store = CreateValidStore(5);
        Assert.True(store.AllStatesValid());

        bool unchanged = store.SetEndState(2, new TestState(2));
        bool changed = store.SetEndState(2, new TestState(99));

        Assert.False(unchanged);
        Assert.True(changed);
        Assert.Equal(3, store.GetFirstInvalidEndStateLineNumber());
        Assert.False(store.AllStatesValid());
    }

    [Fact]
    public void TrackingStore_AcceptChange_InsertedLinesAreInvalid()
    {
        TrackingTokenizationStateStore<TestState> store = CreateValidStore(5);

        // line 2 is replaced by 3 lines
        store.AcceptChange(new OffsetRange(2, 3), 3);

        Assert.Equal(2, store.GetFirstInvalidEndStateLineNumber());
        Assert.Equal(1, store.GetEndState(1)!.Value);
        Assert.Null(store.GetEndState(2));
        Assert.Null(store.GetEndState(3));
        Assert.Equal(2, store.GetEndState(4)!.Value); // the last state is kept for the last new line
        Assert.Equal(3, store.GetEndState(5)!.Value);
    }

    [Fact]
    public void FixedArray_Replace_GrowsAndShrinks()
    {
        var array = new FixedArray<int>(0);
        array.Set(0, 10);
        array.Set(1, 11);
        array.Set(2, 12);

        array.Replace(1, 1, 3);

        Assert.Equal([10, 0, 0, 0, 12], [array.Get(0), array.Get(1), array.Get(2), array.Get(3), array.Get(4)]);

        array.Replace(1, 3, 1);

        Assert.Equal([10, 0, 12, 0], [array.Get(0), array.Get(1), array.Get(2), array.Get(3)]);
    }
}
