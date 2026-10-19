using Microsoft.Win32.SafeHandles;

namespace LoomEditor.TextMate.Oniguruma;

/// <summary>An Oniguruma match region (<c>OnigRegion*</c>), freed with <c>onig_region_free(region, 1)</c>.</summary>
/// <remarks>Threading: used by the thread that created it (ADR 0008, decision 5).</remarks>
internal sealed class OnigRegionHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>Allocates a new region (<c>onig_region_new</c>).</summary>
    public OnigRegionHandle()
        : base(ownsHandle: true)
    {
        SetHandle(OnigNative.RegionNew());
        if (IsInvalid)
        {
            throw new OnigException("onig_region_new returned NULL (out of memory).");
        }
    }

    /// <inheritdoc />
    protected override bool ReleaseHandle()
    {
        OnigNative.RegionFree(handle, 1);
        return true;
    }
}
