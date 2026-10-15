// Ported from microsoft/vscode: src/vs/editor/common/core/characterClassifier.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>A set of UTF-16 code units (Monaco: <c>CharacterSet</c>).</summary>
/// <remarks>Threading: mutable, not thread-safe while being modified.</remarks>
internal sealed class CharacterSet
{
    private readonly CharacterClassifier<bool> _actual = new(false);

    public void Add(int charCode) => _actual.Set(charCode, true);

    public bool Has(int charCode) => _actual.Get(charCode);

    public void Clear() => _actual.Clear();
}
