using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LoomEditor.Core.Diagnostics;

/// <summary>
/// Small argument-checking helpers shared by the Core project.
/// </summary>
/// <remarks>
/// Monaco uses 1-based line numbers and columns everywhere. <see cref="IsOneBased"/> exists so that
/// this convention is checked the same way, with the same message, in every constructor.
/// </remarks>
internal static class Guard
{
    /// <summary>Returns <paramref name="value"/>, or throws if it is lower than 1.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is lower than 1.</exception>
    public static int IsOneBased(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Line numbers and columns are 1-based and must be >= 1.");
        }

        return value;
    }

    /// <summary>
    /// Checks an internal invariant, in Debug builds only. Use it for "this can never happen" conditions,
    /// never to validate arguments coming from callers.
    /// </summary>
    [Conditional("DEBUG")]
    public static void Assert([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression(nameof(condition))] string? message = null)
    {
        Debug.Assert(condition, message);
    }
}
