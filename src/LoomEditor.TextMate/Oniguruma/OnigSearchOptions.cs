namespace LoomEditor.TextMate.Oniguruma;

/// <summary>Search-time options of Oniguruma 6.9.10 used by vscode-oniguruma (values of <c>oniguruma.h</c>).</summary>
/// <remarks>Threading: a value type.</remarks>
[Flags]
internal enum OnigSearchOptions : uint
{
    /// <summary><c>ONIG_OPTION_NONE</c>.</summary>
    None = OnigNative.OptionNone,

    /// <summary><c>ONIG_OPTION_NOT_BEGIN_STRING</c>: <c>\A</c> does not match at the start of the subject.</summary>
    NotBeginString = OnigNative.OptionNotBeginString,

    /// <summary><c>ONIG_OPTION_NOT_END_STRING</c>: <c>\z</c> and <c>\Z</c> do not match at the end of the subject.</summary>
    NotEndString = OnigNative.OptionNotEndString,

    /// <summary><c>ONIG_OPTION_NOT_BEGIN_POSITION</c>: <c>\G</c> does not match at the start position.</summary>
    NotBeginPosition = OnigNative.OptionNotBeginPosition,
}
