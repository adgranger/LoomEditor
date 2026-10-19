using System.Runtime.InteropServices;

namespace LoomEditor.TextMate.Oniguruma;

/// <summary>
/// <c>[LibraryImport]</c> declarations of the Oniguruma 6.9.10 functions used by LoomEditor (<c>oniguruma.h</c>), and
/// the constants they need.
/// </summary>
/// <remarks>
/// <para>Threading: the functions are thread-safe for distinct regex and region objects (Oniguruma's contract).</para>
/// <para>
/// Oniguruma stops a match after <c>DEFAULT_RETRY_LIMIT_IN_MATCH</c> = 10 000 000 retries (6.9.10, <c>regint.h</c>; the
/// search retry limit is 0 = unlimited) and returns <see cref="ErrorRetryLimitInMatchOver"/>. LoomEditor keeps that
/// compiled-in default (ADR 0007, decision 4).
/// </para>
/// </remarks>
internal static unsafe partial class OnigNative
{
    /// <summary>The library name: the default probing finds <c>onig.dll</c> next to the app.</summary>
    public const string Library = "onig";

    /// <summary>Exported data symbol of the UTF-16 LE encoding (<c>ONIG_ENCODING_UTF16_LE</c> is its address).</summary>
    public const string EncodingUtf16LeExport = "OnigEncodingUTF16_LE";

    /// <summary>Exported pointer variable whose value is <c>ONIG_SYNTAX_DEFAULT</c>.</summary>
    public const string DefaultSyntaxExport = "OnigDefaultSyntax";

    /// <summary><c>ONIG_MAX_ERROR_MESSAGE_LEN</c>.</summary>
    public const int MaxErrorMessageLength = 90;

    /// <summary><c>ONIG_NORMAL</c>.</summary>
    public const int Normal = 0;

    /// <summary><c>ONIG_MISMATCH</c>.</summary>
    public const int Mismatch = -1;

    /// <summary><c>ONIGERR_RETRY_LIMIT_IN_MATCH_OVER</c>.</summary>
    public const int ErrorRetryLimitInMatchOver = -17;

    /// <summary><c>ONIGERR_RETRY_LIMIT_IN_SEARCH_OVER</c>.</summary>
    public const int ErrorRetryLimitInSearchOver = -18;

    /// <summary><c>ONIG_OPTION_NONE</c>.</summary>
    public const uint OptionNone = 0;

    /// <summary><c>ONIG_OPTION_CAPTURE_GROUP</c> (<c>1 &lt;&lt; 8</c>).</summary>
    public const uint OptionCaptureGroup = 1U << 8;

    /// <summary><c>ONIG_OPTION_NOT_BEGIN_STRING</c> (<c>1 &lt;&lt; 22</c>).</summary>
    public const uint OptionNotBeginString = 1U << 22;

    /// <summary><c>ONIG_OPTION_NOT_END_STRING</c> (<c>1 &lt;&lt; 23</c>).</summary>
    public const uint OptionNotEndString = 1U << 23;

    /// <summary><c>ONIG_OPTION_NOT_BEGIN_POSITION</c> (<c>1 &lt;&lt; 24</c>).</summary>
    public const uint OptionNotBeginPosition = 1U << 24;

    /// <summary><c>int onig_initialize(OnigEncoding encodings[], int number_of_encodings)</c>.</summary>
    [LibraryImport(Library, EntryPoint = "onig_initialize")]
    public static partial int Initialize(nint* encodings, int numberOfEncodings);

    /// <summary><c>int onig_new(OnigRegex*, pattern, pattern_end, option, enc, syntax, OnigErrorInfo*)</c>.</summary>
    [LibraryImport(Library, EntryPoint = "onig_new")]
    public static partial int New(nint* regex, byte* pattern, byte* patternEnd, uint option, nint encoding, nint syntax, OnigErrorInfo* errorInfo);

    /// <summary><c>void onig_free(OnigRegex)</c>.</summary>
    [LibraryImport(Library, EntryPoint = "onig_free")]
    public static partial void Free(nint regex);

    /// <summary><c>int onig_search(OnigRegex, str, end, start, range, OnigRegion*, option)</c>.</summary>
    [LibraryImport(Library, EntryPoint = "onig_search")]
    public static partial int Search(nint regex, byte* str, byte* end, byte* start, byte* range, nint region, uint option);

    /// <summary><c>OnigRegion* onig_region_new(void)</c>.</summary>
    [LibraryImport(Library, EntryPoint = "onig_region_new")]
    public static partial nint RegionNew();

    /// <summary><c>void onig_region_free(OnigRegion* region, int free_self)</c>.</summary>
    [LibraryImport(Library, EntryPoint = "onig_region_free")]
    public static partial void RegionFree(nint region, int freeSelf);

    /// <summary><c>const char* onig_version(void)</c>.</summary>
    [LibraryImport(Library, EntryPoint = "onig_version")]
    public static partial byte* Version();

    /// <summary><c>int onig_error_code_to_str(OnigUChar* s, int err_code, ...)</c>.</summary>
    /// <remarks>
    /// Variadic in C. It is declared with its one optional argument (<c>OnigErrorInfo*</c>): the Windows x64 and ARM64
    /// ABIs pass a pointer given to a variadic function in the same integer register as a fixed argument.
    /// </remarks>
    [LibraryImport(Library, EntryPoint = "onig_error_code_to_str")]
    public static partial int ErrorCodeToString(byte* buffer, int errorCode, OnigErrorInfo* errorInfo);

    /// <summary><c>OnigErrorInfo</c>.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct OnigErrorInfo
    {
        /// <summary><c>OnigEncoding enc</c>.</summary>
        public nint Encoding;

        /// <summary><c>OnigUChar* par</c>.</summary>
        public nint Par;

        /// <summary><c>OnigUChar* par_end</c>.</summary>
        public nint ParEnd;
    }

    /// <summary><c>OnigRegion</c> (<c>struct re_registers</c>).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct OnigRegion
    {
        /// <summary><c>int allocated</c>.</summary>
        public int Allocated;

        /// <summary><c>int num_regs</c>.</summary>
        public int NumRegs;

        /// <summary><c>int* beg</c>: start offsets in bytes, <c>-1</c> for an unmatched group.</summary>
        public int* Beg;

        /// <summary><c>int* end</c>: end offsets in bytes, <c>-1</c> for an unmatched group.</summary>
        public int* End;

        /// <summary><c>OnigCaptureTreeNode* history_root</c>.</summary>
        public nint HistoryRoot;
    }
}
