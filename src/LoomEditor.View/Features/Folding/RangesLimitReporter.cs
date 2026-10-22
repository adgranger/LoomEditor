// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/folding.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Folding;

/// <summary>Records how many folding ranges were found and whether they were limited (Monaco: <c>RangesLimitReporter</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class RangesLimitReporter(Func<int> getLimit) : IFoldingLimitReporter
{
    /// <summary>Raised when <see cref="Computed"/> or <see cref="Limited"/> changes (Monaco: <c>onDidChange</c>).</summary>
    public event EventHandler? Changed;

    public int Limit => getLimit();

    public int Computed { get; private set; }

    /// <summary>The limit when the ranges were limited, else <see langword="null"/> (Monaco: <c>false</c>).</summary>
    public int? Limited { get; private set; }

    public void Update(int computed, int? limited)
    {
        if (computed != Computed || limited != Limited)
        {
            Computed = computed;
            Limited = limited;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
