using System.Runtime.InteropServices;

namespace LoomEditor.TextMate.Oniguruma;

/// <summary>
/// Process-wide Oniguruma setup: <c>onig_initialize</c> with the UTF-16 LE encoding, called once, and the addresses
/// of <c>ONIG_ENCODING_UTF16_LE</c> and <c>ONIG_SYNTAX_DEFAULT</c>.
/// </summary>
/// <remarks>Threading: thread-safe. The setup runs once, through a <c>static readonly</c> lazy initializer.</remarks>
internal static unsafe class OnigLibrary
{
    private static readonly Lazy<Setup> Instance = new(Initialize, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary><c>ONIG_ENCODING_UTF16_LE</c>.</summary>
    public static nint EncodingUtf16Le => Instance.Value.EncodingUtf16Le;

    /// <summary><c>ONIG_SYNTAX_DEFAULT</c>.</summary>
    public static nint DefaultSyntax => Instance.Value.DefaultSyntax;

    /// <summary><c>onig_version()</c>.</summary>
    public static string Version => Marshal.PtrToStringAnsi((nint)OnigNative.Version()) ?? string.Empty;

    /// <summary>Runs the one-time setup if it has not run yet.</summary>
    public static void EnsureInitialized() => _ = Instance.Value;

    private static Setup Initialize()
    {
        nint library = NativeLibrary.Load(OnigNative.Library, typeof(OnigLibrary).Assembly, null);
        nint encoding = NativeLibrary.GetExport(library, OnigNative.EncodingUtf16LeExport);
        nint syntax = *(nint*)NativeLibrary.GetExport(library, OnigNative.DefaultSyntaxExport);

        int status = OnigNative.Initialize(&encoding, 1);
        if (status != OnigNative.Normal)
        {
            throw new OnigException($"onig_initialize failed ({status}).");
        }

        return new Setup(encoding, syntax);
    }

    private readonly record struct Setup(nint EncodingUtf16Le, nint DefaultSyntax);
}
