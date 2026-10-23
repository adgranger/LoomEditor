// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Guides;

namespace LoomEditor.Core.Model;

public sealed partial class TextModel
{
    /// <summary>The indent and bracket pair guides of the text (Monaco: <c>guides</c>).</summary>
    internal GuidesTextModelPart Guides { get; }
}
