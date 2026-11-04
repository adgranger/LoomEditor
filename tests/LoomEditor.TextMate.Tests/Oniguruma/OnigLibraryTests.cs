using System.Runtime.InteropServices;

namespace LoomEditor.TextMate.Tests.Oniguruma;

public class OnigLibraryTests
{
    private static readonly string LibraryFile =
        OperatingSystem.IsWindows() ? "onig.dll" : OperatingSystem.IsMacOS() ? "libonig.dylib" : "libonig.so";

    [Fact]
    public void OnigLibrary_IsNextToTheTestAssembly()
    {
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, LibraryFile)));
    }

    [Fact]
    public void OnigLibrary_LoadsAndExportsVersion()
    {
        Assert.True(NativeLibrary.TryLoad(LibraryFile, typeof(OnigLibraryTests).Assembly, null, out nint handle));
        try
        {
            Assert.True(NativeLibrary.TryGetExport(handle, "onig_version", out nint export));
            Assert.NotEqual(0, export);
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }
}
