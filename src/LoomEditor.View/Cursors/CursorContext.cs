// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorContext.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Cursors;

/// <summary>What the cursors read: the model, the view model, the coordinates converter and the configuration (Monaco: <c>CursorContext</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class CursorContext(
    TextModel model,
    ICursorSimpleModel viewModel,
    ICoordinatesConverter coordinatesConverter,
    CursorConfiguration cursorConfig)
{
    public TextModel Model { get; } = model;

    public ICursorSimpleModel ViewModel { get; } = viewModel;

    public ICoordinatesConverter CoordinatesConverter { get; } = coordinatesConverter;

    public CursorConfiguration CursorConfig { get; } = cursorConfig;
}
