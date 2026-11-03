#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#endif

/// <summary>Tolerance comparison for layout values (DIPs, scaling, zoom), where exact floating-point equality is fragile.</summary>
/// <remarks>Threading: stateless; any thread.</remarks>
internal static class DoubleComparison
{
    private const double Epsilon = 1e-9;

    internal static bool AreClose(double a, double b) => Math.Abs(a - b) <= Epsilon;
}
