using Microsoft.Win32.SafeHandles;

namespace LoomEditor.TextMate.Oniguruma;

/// <summary>A compiled Oniguruma regex (<c>regex_t*</c>), freed with <c>onig_free</c>.</summary>
/// <remarks>Threading: used by the thread that created it (ADR 0008, decision 5).</remarks>
internal sealed class OnigRegexHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>Takes ownership of <paramref name="regex"/>.</summary>
    public OnigRegexHandle(nint regex)
        : base(ownsHandle: true)
    {
        SetHandle(regex);
    }

    /// <inheritdoc />
    protected override bool ReleaseHandle()
    {
        OnigNative.Free(handle);
        return true;
    }
}
