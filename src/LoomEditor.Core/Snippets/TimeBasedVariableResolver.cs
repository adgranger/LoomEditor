// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetVariables.ts (TimeBasedVariableResolver)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;

namespace LoomEditor.Core.Snippets;

/// <summary>Resolves the <c>CURRENT_*</c> variables from the time the resolver was created (Monaco: <c>TimeBasedVariableResolver</c>).</summary>
/// <remarks>
/// Behavior difference: day and month names are English (Monaco localizes them). The clock is a <see cref="TimeProvider"/>, read once.
/// Threading: UI thread only; resolvers are created per insertion.
/// </remarks>
internal sealed class TimeBasedVariableResolver(TimeProvider timeProvider) : IVariableResolver
{
    private static readonly DateTimeFormatInfo English = CultureInfo.InvariantCulture.DateTimeFormat;

    private readonly DateTimeOffset _date = timeProvider.GetLocalNow();

    /// <inheritdoc />
    public string? Resolve(Variable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        CultureInfo culture = CultureInfo.InvariantCulture;
        return variable.Name switch
        {
            "CURRENT_YEAR" => _date.Year.ToString(culture),
            "CURRENT_YEAR_SHORT" => (_date.Year % 100).ToString("D2", culture),
            "CURRENT_MONTH" => _date.Month.ToString("D2", culture),
            "CURRENT_DATE" => _date.Day.ToString("D2", culture),
            "CURRENT_HOUR" => _date.Hour.ToString("D2", culture),
            "CURRENT_MINUTE" => _date.Minute.ToString("D2", culture),
            "CURRENT_SECOND" => _date.Second.ToString("D2", culture),
            "CURRENT_MILLISECOND" => _date.Millisecond.ToString("D3", culture),
            "CURRENT_DAY_NAME" => English.DayNames[(int)_date.DayOfWeek],
            "CURRENT_DAY_NAME_SHORT" => English.AbbreviatedDayNames[(int)_date.DayOfWeek],
            "CURRENT_MONTH_NAME" => English.MonthNames[_date.Month - 1],
            "CURRENT_MONTH_NAME_SHORT" => English.AbbreviatedMonthNames[_date.Month - 1],
            "CURRENT_SECONDS_UNIX" => _date.ToUnixTimeSeconds().ToString(culture),
            "CURRENT_MILLISECONDS_UNIX" => _date.ToUnixTimeMilliseconds().ToString(culture),
            "CURRENT_TIMEZONE_OFFSET" => FormatOffset(_date.Offset),
            "CURRENT_TIMEZONE_NAME" => timeProvider.LocalTimeZone.Id,
            _ => null,
        };
    }

    // Monaco: sign + hh + ':' + mm (lines 312-319).
    private static string FormatOffset(TimeSpan offset)
    {
        int minutes = (int)Math.Abs(offset.TotalMinutes);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{(offset < TimeSpan.Zero ? '-' : '+')}{minutes / 60:D2}:{minutes % 60:D2}");
    }
}
