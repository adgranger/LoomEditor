// Ported from microsoft/vscode: src/vs/editor/contrib/gotoSymbol/browser/goToCommands.ts (DefinitionAction id)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.GotoDefinition;

/// <summary>Ids of the commands exposed by <see cref="GotoDefinitionFeature"/>. Same ids as Monaco.</summary>
public static class GotoDefinitionCommands
{
    /// <summary>Goes to the definition of the symbol at the cursor (F12).</summary>
    public const string RevealDefinition = "editor.action.revealDefinition";
}
