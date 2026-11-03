#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

/// <summary>A line of one side of a <see cref="LoomDiffEditor"/>.</summary>
/// <param name="Side">The side of the line.</param>
/// <param name="LineNumber">The 1-based model line number in that side's text.</param>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
public readonly record struct DiffLine(DiffSide Side, int LineNumber);
