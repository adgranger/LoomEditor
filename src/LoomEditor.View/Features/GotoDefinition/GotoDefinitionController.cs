// Ported from microsoft/vscode: src/vs/editor/contrib/gotoSymbol/browser/goToCommands.ts (SymbolNavigationAction.runEditorCommand, _onResult, _openReference)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Events;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.GotoDefinition;

/// <summary>
/// Asks the definition providers for the symbol at a position and goes to the first result.
/// With several results Monaco opens a peek view; LoomEditor has none, so it goes to the first reference (behavior difference).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class GotoDefinitionController(IEditorContext context) : IDisposable
{
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Goes to the definition of the symbol at <paramref name="position"/> (the cursor when null).
    /// A request started from the keyboard is dropped if the text or the selection changes while waiting.
    /// </summary>
    public void Run(Position? position = null)
    {
        if (context is not EditorContext editorContext || !editorContext.LanguageFeatures.DefinitionProviders.Has(context.GetLanguageId()))
        {
            return;
        }

        bool fromMouse = position is not null;
        Position target = position ?? context.Selection.Position;
        TextModel model = context.Model;
        int versionId = model.VersionId;
        Selection startSelection = context.Selection;

        Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;
        CancellationToken token = cts.Token;

        UiContinuation.Then(
            DefinitionProviders.GetDefinitionsAtPosition(editorContext.LanguageFeatures.DefinitionProviders, model, context.GetLanguageId(), target, token),
            context.GetUiDispatcher(),
            links =>
            {
                if (model.VersionId != versionId || (!fromMouse && context.Selection != startSelection))
                {
                    return;
                }

                OnResults(links, target);
            },
            token: token);
    }

    /// <summary>Cancels the request in progress.</summary>
    public void Dispose() => Cancel();

    private void Cancel()
    {
        if (_cts is { } cts)
        {
            _cts = null;
            cts.Cancel();
            cts.Dispose();
        }
    }

    private void OnResults(IReadOnlyList<LocationLink> links, Position position)
    {
        var results = new DefinitionResults(links);
        IEditorAnnouncer announcer = context.GetAnnouncer();
        announcer.Announce(results.AriaMessage(context.GetDocumentUri()));
        if (results.FirstReference is not { } first)
        {
            announcer.Announce(AccessibilityMessages.NoDefinitionFound(context.Model.GetWordAtPosition(position)?.Word));
            return;
        }

        OpenReference(first);
    }

    // Monaco: _openReference
    private void OpenReference(LocationLink link)
    {
        Range range = link.TargetSelectionRange ?? link.Range;
        if (string.Equals(link.Uri, context.GetDocumentUri(), StringComparison.Ordinal))
        {
            if (context.GetViewModel() is not { } viewModel)
            {
                return;
            }

            Range validated = context.Model.ValidateRange(range);
            viewModel.SetSelections("code.jump", [Selection.Collapsed(validated.Start)]);
            Range viewRange = viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(Range.Collapsed(validated.Start));
            viewModel.RevealRange("code.jump", true, viewRange, VerticalRevealType.NearTopIfOutsideViewport, ScrollType.Smooth);
        }
        else if (!(context is EditorContext { LocationOpener: { } opener } && opener.Open(link)))
        {
            context.GetAnnouncer().Announce(AccessibilityMessages.DefinitionInOtherDocument(link.Uri));
        }
    }
}
