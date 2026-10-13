// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Edits;

/// <summary>A content change plus whether markers must be moved (Monaco: <c>IInternalModelContentChange</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
/// <param name="Change">The change itself.</param>
/// <param name="ForceMoveMarkers">True when markers at the change position must move.</param>
internal sealed record InternalModelContentChange(ModelContentChange Change, bool ForceMoveMarkers);
