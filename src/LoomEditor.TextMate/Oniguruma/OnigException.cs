namespace LoomEditor.TextMate.Oniguruma;

/// <summary>An Oniguruma error, with Oniguruma's message (for example, a pattern that does not compile).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class OnigException : Exception
{
    /// <summary>Creates the exception.</summary>
    public OnigException()
    {
    }

    /// <summary>Creates the exception with Oniguruma's message.</summary>
    public OnigException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and its cause.</summary>
    public OnigException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
