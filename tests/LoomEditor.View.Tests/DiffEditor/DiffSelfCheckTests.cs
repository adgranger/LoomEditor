using LoomEditor.Gallery;
using LoomEditor.View.Features.Scheduling;

namespace LoomEditor.View.Tests.DiffEditor;

// New tests: the self-check of the Gallery (compiled into this project) must pass here too, with a synchronous dispatcher.
public class DiffSelfCheckTests
{
    private sealed class ImmediateScheduler : IEditorScheduler
    {
        public IDisposable Schedule(TimeSpan delay, Action action)
        {
            action();
            return new Noop();
        }

        private sealed class Noop : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private static readonly Lazy<Task<IReadOnlyList<DiffCheckResult>>> Results =
        new(() => DiffSelfCheck.RunAsync(InlineUiDispatcher.Instance, new ImmediateScheduler()));

    private static async Task AssertPasses(string name)
    {
        IReadOnlyList<DiffCheckResult> results = await Results.Value;

        DiffCheckResult result = Assert.Single(results, r => r.Name == name);
        Assert.True(result.Passed, $"{name}: {result.Detail}");
    }

    [Fact]
    public Task SelfCheck_Algorithm_Passes() => AssertPasses("algorithm");

    [Fact]
    public Task SelfCheck_Large_Passes() => AssertPasses("large");

    [Fact]
    public async Task SelfCheck_ProviderMatchesDirectComputation()
    {
        await AssertPasses("provider");
        await AssertPasses("provider-cancel");
    }

    [Fact]
    public Task SelfCheck_AccessibleModelLabel() => AssertPasses("accessible-model");

    [Fact]
    public async Task SelfCheck_AllChecks_Pass_WithSynchronousDispatcher()
    {
        IReadOnlyList<DiffCheckResult> results = await Results.Value;

        Assert.Equal(9, results.Count);
        Assert.All(results, r => Assert.True(r.Passed, $"{r.Name}: {r.Detail}"));
    }

    [Fact]
    public void Report_Format_PassFailAndSummary()
    {
        string report = DiffSelfCheck.FormatReport([new DiffCheckResult("a", true, "ok"), new DiffCheckResult("b", false, "broken")]);

        Assert.Equal("PASS  a — ok\nFAIL  b — broken\nSUMMARY: 1 passed, 1 failed\n", report);
    }
}
