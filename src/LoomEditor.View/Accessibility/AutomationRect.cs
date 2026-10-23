namespace LoomEditor.View.Accessibility;

/// <summary>A rectangle in editor-relative DIPs (the control's coordinates).</summary>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct AutomationRect(double X, double Y, double Width, double Height);
