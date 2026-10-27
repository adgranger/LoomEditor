using System.Diagnostics;
using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Tests.Diff.TestHelpers;

namespace LoomEditor.Core.Tests.Diff;

public class TimeoutTests
{
    // Monaco: InfiniteTimeout
    [Fact]
    public void InfiniteTimeout_AlwaysValid()
    {
        for (int i = 0; i < 100; i++)
        {
            Assert.True(InfiniteTimeout.Instance.IsValid());
        }
    }

    // Monaco: DateTimeout
    [Fact]
    public void DateTimeout_ExpiresAndStaysInvalid()
    {
        DateTimeout t = new(1);
        Stopwatch sw = Stopwatch.StartNew();
        while (t.IsValid() && sw.Elapsed < TimeSpan.FromSeconds(5))
        {
            Thread.SpinWait(100);
        }

        Assert.False(t.IsValid());
        Assert.False(t.IsValid());
    }

    [Fact]
    public void DateTimeout_NotPositive_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DateTimeout(0));
    }

    [Fact]
    public void FakeTimeout_AfterNCalls_TurnsInvalid()
    {
        FakeTimeout t = new(2);
        Assert.True(t.IsValid());
        Assert.True(t.IsValid());
        Assert.False(t.IsValid());
    }
}
