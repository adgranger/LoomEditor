// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/indentRangeProvider.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Features.Folding;

/// <summary>The range provider of phase 5: indentation, region markers and the off-side rule (Monaco: <c>IndentRangeProvider</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class IndentRangeProvider(TextModel model, LanguageConfigurationRegistry registry, IFoldingLimitReporter limitReporter) : IRangeProvider
{
    private const string IdIndentProvider = "indent";

    public string Id => IdIndentProvider;

    public void Dispose()
    {
    }

    public FoldingRegions? Compute()
    {
        FoldingRules foldingRules = registry.GetLanguageConfiguration(model.Tokenization.GetLanguageId()).FoldingRules;
        return IndentRanges.ComputeRanges(model, foldingRules.OffSide, foldingRules.Markers, limitReporter);
    }
}