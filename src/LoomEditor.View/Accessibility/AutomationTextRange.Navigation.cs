using LoomEditor.Core.Text;

namespace LoomEditor.View.Accessibility;

internal sealed partial class AutomationTextRange
{
    /// <summary>Makes the range exactly the unit that contains its start.</summary>
    public void ExpandToEnclosingUnit(AutomationTextUnit unit)
    {
        AutomationUnitStarts starts = AutomationUnitStarts.Create(Document, unit);
        Position start = starts.StartAtOrBefore(Range.Start);
        Set(new Range(start, starts.NextStart(start)));
    }

    /// <summary>Expands to the unit, then moves by <paramref name="count"/> units; returns the signed number of units really moved.</summary>
    public int Move(AutomationTextUnit unit, int count)
    {
        AutomationUnitStarts starts = AutomationUnitStarts.Create(Document, unit);
        Position cur = starts.StartAtOrBefore(Range.Start);
        int moved = 0;
        for (; moved < count; moved++)
        {
            if (starts.NextStartStrict(cur) is not { } next)
            {
                break;
            }

            cur = next;
        }

        for (; moved > count; moved--)
        {
            if (starts.PreviousStart(cur) is not { } previous)
            {
                break;
            }

            cur = previous;
        }

        Set(new Range(cur, starts.NextStart(cur)));
        return moved;
    }

    /// <summary>Moves one endpoint by <paramref name="count"/> units (a crossed range collapses to the moved endpoint); returns the signed count really moved.</summary>
    public int MoveEndpointByUnit(AutomationTextEndpoint endpoint, AutomationTextUnit unit, int count)
    {
        ValidateEndpoint(endpoint);
        AutomationUnitStarts starts = AutomationUnitStarts.Create(Document, unit);
        if (count == 0)
        {
            return 0;
        }

        Position e = Get(endpoint);
        int moved = 0;
        if (count > 0)
        {
            while (moved < count)
            {
                if (starts.NextStartStrict(e) is { } next)
                {
                    e = next;
                    moved++;
                }
                else
                {
                    if (e < Document.DocumentEnd)
                    {
                        e = Document.DocumentEnd;
                        moved++;
                    }

                    break;
                }
            }
        }
        else
        {
            while (moved > count && starts.PreviousStart(e) is { } previous)
            {
                e = previous;
                moved--;
            }
        }

        SetEndpoint(endpoint, e);
        return moved;
    }

    /// <summary>Moves one endpoint to an endpoint of <paramref name="other"/> (a crossed range collapses to the moved endpoint).</summary>
    public void MoveEndpointByRange(AutomationTextEndpoint endpoint, AutomationTextRange other, AutomationTextEndpoint otherEndpoint)
    {
        ArgumentNullException.ThrowIfNull(other);
        ValidateEndpoint(endpoint);
        ValidateEndpoint(otherEndpoint);
        if (!ReferenceEquals(Document, other.Document))
        {
            throw new ArgumentException("The ranges belong to different documents.", nameof(other));
        }

        SetEndpoint(endpoint, other.Get(otherEndpoint));
    }

    private void SetEndpoint(AutomationTextEndpoint endpoint, Position value)
    {
        Range r = Range;
        Set(endpoint == AutomationTextEndpoint.Start
            ? new Range(value, r.End < value ? value : r.End)
            : new Range(r.Start > value ? value : r.Start, value));
    }

    private static void ValidateEndpoint(AutomationTextEndpoint endpoint)
    {
        if (endpoint is not (AutomationTextEndpoint.Start or AutomationTextEndpoint.End))
        {
            throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, null);
        }
    }
}
