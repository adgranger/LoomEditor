// Ported from microsoft/vscode: src/vs/editor/browser/editorBrowser.ts (IViewZoneChangeAccessor)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>Adds, removes and re-lays out view zones in bulk (Monaco: <c>IViewZoneChangeAccessor</c>).</summary>
/// <remarks>Threading: only valid inside the callback of <see cref="ViewZones.Change"/>; calls after it throw <see cref="InvalidOperationException"/>.</remarks>
internal interface IViewZoneChangeAccessor
{
    /// <summary>Adds a zone, assigns its <see cref="ViewZone.Id"/> and returns it.</summary>
    string AddZone(ViewZone zone);

    /// <summary>Removes a zone; unknown ids are ignored.</summary>
    void RemoveZone(string id);

    /// <summary>Recomputes the position and height of a zone after its fields changed; unknown ids are ignored.</summary>
    void LayoutZone(string id);
}
