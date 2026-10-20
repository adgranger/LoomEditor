// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model;

public sealed partial class TextModel : IVirtualModel
{
    /// <summary>
    /// The language configurations of the editor of the model (Monaco: the <c>ILanguageConfigurationService</c>
    /// parameter); a new registry with the plain text defaults when the model was created without one.
    /// </summary>
    internal LanguageConfigurationRegistry LanguageConfigurationRegistry { get; }

    /// <summary>
    /// The language configuration of the model's language changed, or the language changed
    /// (Monaco: <c>onDidChangeLanguageConfiguration</c>; its event is empty).
    /// </summary>
    internal event EventHandler? LanguageConfigurationChanged;

    /// <summary>The word at a position, or <see langword="null"/> (Monaco: <c>getWordAtPosition</c>).</summary>
    internal WordAtPosition? GetWordAtPosition(Position position) => _tokenization.GetWordAtPosition(position);

    /// <summary>The word at a position, cut at the position (Monaco: <c>getWordUntilPosition</c>).</summary>
    internal WordAtPosition GetWordUntilPosition(Position position) => _tokenization.GetWordUntilPosition(position);

    IViewLineTokens IVirtualModel.GetLineTokens(int lineNumber) => _tokenization.GetLineTokens(lineNumber);

    string IVirtualModel.GetLanguageId() => _tokenization.GetLanguageId();

    string IVirtualModel.GetLanguageIdAtPosition(int lineNumber, int column) => _tokenization.GetLanguageIdAtPosition(lineNumber, column);

    void IVirtualModel.ForceTokenization(int lineNumber) => _tokenization.ForceTokenization(lineNumber);

    internal void RaiseLanguageConfigurationChanged() => LanguageConfigurationChanged?.Invoke(this, EventArgs.Empty);
}
