namespace LoomEditor.Core.Model.Tokens;

/// <summary>
/// What the tokenizers need to know about their model besides its text: Monaco's <c>ITextModel.getLanguageId()</c> and
/// <c>isAttachedToEditor()</c>, which <see cref="TextModel"/> only gets in P4-29 / P4-30.
/// </summary>
/// <remarks>Threading: read by the tokenizing thread; an implementation must make its values safe to read there.</remarks>
internal interface ITokenizationModelInfo
{
    /// <summary>The language id of the model.</summary>
    string LanguageId { get; }

    /// <summary>Whether the model is attached to an editor (background tokenization only runs then).</summary>
    bool IsAttachedToEditor { get; }
}
