// Ported from microsoft/vscode: src/vs/editor/common/core/editOperation.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Edits;

/// <summary>Identifies the origin of a single edit operation (Monaco: <c>ISingleEditOperationIdentifier</c>).</summary>
/// <remarks>Threading: immutable value type.</remarks>
/// <param name="Major">The major part of the identifier.</param>
/// <param name="Minor">The minor part of the identifier.</param>
internal readonly record struct EditOperationIdentifier(int Major, int Minor);
