// Ported from microsoft/vscode: src/vs/editor/common/tokenizationRegistry.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>
/// The tokenization supports by language and the color map that token colors refer to (Monaco:
/// <c>TokenizationRegistry</c>, also <c>ITokenizationRegistry</c>).
/// </summary>
/// <remarks>
/// <para>
/// Differences from Monaco: an <b>instance</b>, one per editor (ADR 0008, decision 4), not a global; not generic
/// (always <see cref="ITokenizationSupport"/>); no lazy factories (<c>registerFactory</c>, <c>getOrCreate</c>,
/// <c>isResolved</c>); the color map is ARGB integers (<c>0xAARRGGBB</c>, index 0 unused) because Core does not know the
/// theme types; <c>onDidChange</c> is <see cref="Changed"/>.
/// </para>
/// <para>Threading: not thread-safe; use it from the thread of the editor.</para>
/// </remarks>
internal sealed class TokenizationRegistry
{
    private readonly Dictionary<string, ITokenizationSupport> _tokenizationSupports = new(StringComparer.Ordinal);
    private IReadOnlyList<uint>? _colorMap;

    /// <summary>
    /// <c>onDidChange</c>: raised when a tokenization support is registered, unregistered or changed, and when the color
    /// map is changed.
    /// </summary>
    public event EventHandler<TokenizationSupportChangedEventArgs>? Changed;

    /// <summary>
    /// <c>handleChange</c>: raises <see cref="Changed"/> for languages. This is useful for languages that embed other
    /// languages.
    /// </summary>
    public void HandleChange(IReadOnlyList<string> languageIds) =>
        Changed?.Invoke(this, new TokenizationSupportChangedEventArgs(languageIds, false));

    /// <summary><c>register</c>: registers a tokenization support; dispose the result to unregister it.</summary>
    public IDisposable Register(string languageId, ITokenizationSupport support)
    {
        ArgumentNullException.ThrowIfNull(languageId);
        ArgumentNullException.ThrowIfNull(support);
        _tokenizationSupports[languageId] = support;
        HandleChange([languageId]);
        return new Registration(() =>
        {
            if (!_tokenizationSupports.TryGetValue(languageId, out ITokenizationSupport? current) || !ReferenceEquals(current, support))
            {
                return;
            }

            _tokenizationSupports.Remove(languageId);
            HandleChange([languageId]);
        });
    }

    /// <summary><c>get</c>: the tokenization support of a language, or <see langword="null"/>.</summary>
    public ITokenizationSupport? Get(string languageId) => _tokenizationSupports.GetValueOrDefault(languageId);

    /// <summary>
    /// <c>setColorMap</c>: sets the new color map that all tokens will use in their <see cref="ColorId"/> binary encoded
    /// foreground and background bits.
    /// </summary>
    public void SetColorMap(IReadOnlyList<uint> colorMap)
    {
        ArgumentNullException.ThrowIfNull(colorMap);
        _colorMap = colorMap;
        Changed?.Invoke(this, new TokenizationSupportChangedEventArgs([.. _tokenizationSupports.Keys], true));
    }

    /// <summary><c>getColorMap</c>.</summary>
    public IReadOnlyList<uint>? GetColorMap() => _colorMap;

    /// <summary><c>getDefaultBackground</c>: the ARGB default background, or <see langword="null"/> without a color map.</summary>
    public uint? GetDefaultBackground() =>
        _colorMap is not null && _colorMap.Count > ColorId.DefaultBackground ? _colorMap[ColorId.DefaultBackground] : null;

    private sealed class Registration(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
