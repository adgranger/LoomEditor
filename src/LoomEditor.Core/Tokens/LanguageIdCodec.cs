// Ported from microsoft/vscode: src/vs/editor/common/services/languagesRegistry.ts (and languages.ts: ILanguageIdCodec)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>Maps language names to the small integers stored in token metadata (Monaco: <c>ILanguageIdCodec</c>).</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal interface ILanguageIdCodec
{
    /// <summary><c>encodeLanguageId</c>.</summary>
    int EncodeLanguageId(string languageId);

    /// <summary><c>decodeLanguageId</c>.</summary>
    string DecodeLanguageId(int languageId);
}

/// <summary>Monaco's <c>LanguageIdCodec</c>: the null language is 0, plain text is 1, others follow in order.</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class LanguageIdCodec : ILanguageIdCodec
{
    /// <summary><c>NULL_LANGUAGE_ID</c>.</summary>
    public const string NullLanguageId = "vs.editor.nullLanguage";

    /// <summary><c>PLAINTEXT_LANGUAGE_ID</c>.</summary>
    public const string PlainTextLanguageId = "plaintext";

    private readonly List<string?> _languageIdToLanguage = [];
    private readonly Dictionary<string, int> _languageToLanguageId = new(StringComparer.Ordinal);
    private int _nextLanguageId;

    /// <summary>Creates a codec that knows the null and plain text languages.</summary>
    public LanguageIdCodec()
    {
        RegisterCore(NullLanguageId, LanguageId.Null);
        RegisterCore(PlainTextLanguageId, LanguageId.PlainText);
        _nextLanguageId = 2;
    }

    /// <summary><c>register</c>: gives <paramref name="language"/> the next id, unless it has one.</summary>
    public void Register(string language)
    {
        ArgumentNullException.ThrowIfNull(language);
        if (_languageToLanguageId.ContainsKey(language))
        {
            return;
        }

        RegisterCore(language, _nextLanguageId++);
    }

    /// <inheritdoc />
    public int EncodeLanguageId(string languageId) =>
        _languageToLanguageId.TryGetValue(languageId, out int id) ? id : LanguageId.Null;

    /// <inheritdoc />
    public string DecodeLanguageId(int languageId) =>
        languageId >= 0 && languageId < _languageIdToLanguage.Count && _languageIdToLanguage[languageId] is { Length: > 0 } language
            ? language
            : NullLanguageId;

    private void RegisterCore(string language, int languageId)
    {
        while (_languageIdToLanguage.Count <= languageId)
        {
            _languageIdToLanguage.Add(null);
        }

        _languageIdToLanguage[languageId] = language;
        _languageToLanguageId[language] = languageId;
    }
}
