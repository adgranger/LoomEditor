// Ported from microsoft/vscode-oniguruma: src/onig.cc
// Upstream commit: 716aeaa229e4ae2e3b0057377b55743e9a3e995b  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using System.Text;

namespace LoomEditor.TextMate.Oniguruma;

/// <summary>
/// One compiled Oniguruma regex and its match region: the regex part of vscode-oniguruma's <c>OnigRegExp</c>
/// (<c>createOnigRegExp</c>, <c>hasGAnchor</c>, <c>_searchOnigRegExp</c>, <c>encodeOnigRegion</c>). The per-string
/// search cache is the scanner's (P4-04).
/// </summary>
/// <remarks>
/// <para>
/// Compiled with vscode-oniguruma's options (<c>ONIG_OPTION_CAPTURE_GROUP</c>, <c>ONIG_SYNTAX_DEFAULT</c>) but in
/// <b>UTF-16 LE</b> instead of UTF-8 (ADR 0007, decision 2): patterns and subjects are .NET strings passed as they are,
/// and offsets are UTF-16 code units.
/// Difference: byte escapes (<c>\xHH</c>) are compiled as code points (see <see cref="ToUtf16Pattern"/>).
/// </para>
/// <para>Threading: single-threaded; used and disposed by the thread that created it (ADR 0008, decision 5).</para>
/// </remarks>
internal sealed unsafe class OnigRegex : IDisposable
{
    private readonly OnigRegexHandle _regex;
    private readonly OnigRegionHandle _region;

    /// <summary>Compiles <paramref name="pattern"/> (<c>createOnigRegExp</c>).</summary>
    /// <exception cref="OnigException">The pattern does not compile; the message is Oniguruma's.</exception>
    public OnigRegex(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        OnigLibrary.EnsureInitialized();

        nint regex;
        OnigNative.OnigErrorInfo errorInfo = default;
        int status;
        string compiled = ToUtf16Pattern(pattern);
        fixed (char* chars = compiled)
        {
            byte* data = (byte*)chars;
            status = OnigNative.New(&regex, data, data + (compiled.Length * 2), OnigNative.OptionCaptureGroup,
                OnigLibrary.EncodingUtf16Le, OnigLibrary.DefaultSyntax, &errorInfo);
        }

        if (status != OnigNative.Normal)
        {
            throw new OnigException(ErrorMessage(status, &errorInfo));
        }

        _regex = new OnigRegexHandle(regex);
        _region = new OnigRegionHandle();
        Pattern = pattern;
        HasGAnchor = ContainsGAnchor(pattern);
    }

    /// <summary>The source pattern.</summary>
    public string Pattern { get; }

    /// <summary>Whether the pattern contains <c>\G</c> (results of such a regex are never cached).</summary>
    public bool HasGAnchor { get; }

    /// <summary>
    /// <c>\xHH</c> is a byte escape: Oniguruma rejects it in UTF-16 ("too short multibyte code string"). It is rewritten
    /// as <c>\x{HH}</c>, the code point; the UTF-8 BOM bytes <c>\xEF\xBB\xBF</c> (bundled CSS grammar) become <c>\x{FEFF}</c>.
    /// </summary>
    internal static string ToUtf16Pattern(string pattern)
    {
        pattern = pattern.Replace(@"\xEF\xBB\xBF", @"\x{FEFF}", StringComparison.Ordinal);
        if (!pattern.Contains(@"\x", StringComparison.Ordinal))
        {
            return pattern;
        }

        var result = new StringBuilder(pattern.Length + 8);
        for (int pos = 0; pos < pattern.Length; pos++)
        {
            char c = pattern[pos];
            if (c != '\\' || pos + 1 >= pattern.Length)
            {
                result.Append(c);
                continue;
            }

            char next = pattern[++pos];
            if (next != 'x' || pos + 1 >= pattern.Length || !char.IsAsciiHexDigit(pattern[pos + 1]))
            {
                result.Append(c).Append(next); // any other escape, including an escaped backslash, is copied as is
                continue;
            }

            int digits = pos + 2 < pattern.Length && char.IsAsciiHexDigit(pattern[pos + 2]) ? 2 : 1;
            result.Append(@"\x{").Append(pattern, pos + 1, digits).Append('}');
            pos += digits;
        }

        return result.ToString();
    }

    /// <summary>
    /// <c>hasGAnchor</c>: the naive scan of vscode-oniguruma (a backslash followed by <c>G</c>, even if that backslash
    /// is itself escaped).
    /// </summary>
    public static bool ContainsGAnchor(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        for (int pos = 0; pos < pattern.Length; pos++)
        {
            if (pattern[pos] == '\\' && pos + 1 < pattern.Length)
            {
                if (pattern[pos + 1] == 'G')
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Searches <paramref name="subject"/> from <paramref name="start"/> to its end (<c>_searchOnigRegExp</c>).
    /// </summary>
    /// <returns>The captures (group 0 first), or <see langword="null"/> when nothing matches.</returns>
    /// <remarks>
    /// A search error (for example, Oniguruma's retry limit) is "no match", as in vscode-oniguruma; it is written to
    /// <see cref="Debug"/>.
    /// </remarks>
    public OnigCaptureIndex[]? Search(string subject, int start, OnigSearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, subject.Length);
        ObjectDisposedException.ThrowIf(_regex.IsClosed, this);

        int status;
        bool regexAdded = false;
        bool regionAdded = false;
        try
        {
            _regex.DangerousAddRef(ref regexAdded);
            _region.DangerousAddRef(ref regionAdded);
            fixed (char* chars = subject)
            {
                byte* data = (byte*)chars;
                byte* end = data + (subject.Length * 2);
                status = OnigNative.Search(_regex.DangerousGetHandle(), data, end, data + (start * 2), end,
                    _region.DangerousGetHandle(), (uint)options);
            }

            if (status == OnigNative.Mismatch)
            {
                return null;
            }

            if (status < 0)
            {
                Debug.WriteLine($"Oniguruma search failed ({status}) for /{Pattern}/: {ErrorMessage(status, null)}");
                return null;
            }

            return EncodeRegion((OnigNative.OnigRegion*)_region.DangerousGetHandle());
        }
        finally
        {
            if (regionAdded)
            {
                _region.DangerousRelease();
            }

            if (regexAdded)
            {
                _regex.DangerousRelease();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _region.Dispose();
        _regex.Dispose();
    }

    // encodeOnigRegion: byte offsets / 2 = UTF-16 offsets; -1 (unmatched group) stays -1.
    private static OnigCaptureIndex[] EncodeRegion(OnigNative.OnigRegion* region)
    {
        var captures = new OnigCaptureIndex[region->NumRegs];
        for (int i = 0; i < captures.Length; i++)
        {
            int begin = region->Beg[i];
            int end = region->End[i];
            captures[i] = new OnigCaptureIndex(begin < 0 ? -1 : begin / 2, end < 0 ? -1 : end / 2);
        }

        return captures;
    }

    // getLastOnigError.
    private static string ErrorMessage(int status, OnigNative.OnigErrorInfo* errorInfo)
    {
        byte* buffer = stackalloc byte[OnigNative.MaxErrorMessageLength];
        int length = OnigNative.ErrorCodeToString(buffer, status, errorInfo);
        return Encoding.UTF8.GetString(buffer, Math.Clamp(length, 0, OnigNative.MaxErrorMessageLength));
    }
}
