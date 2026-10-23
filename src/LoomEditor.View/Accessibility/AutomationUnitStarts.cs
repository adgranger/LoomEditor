using LoomEditor.Core.Text;

namespace LoomEditor.View.Accessibility;

/// <summary>The "starts" of one text unit: the positions where a unit begins.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal abstract class AutomationUnitStarts
{
    protected AutomationUnitStarts(AutomationTextDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Document = document;
    }

    /// <summary>The document the starts are computed on.</summary>
    public AutomationTextDocument Document { get; }

    /// <summary>The greatest start that is &lt;= <paramref name="position"/> (always exists: the document start is a start).</summary>
    public abstract Position StartAtOrBefore(Position position);

    /// <summary>The greatest start that is &lt; <paramref name="position"/>, or null when there is none.</summary>
    public abstract Position? PreviousStart(Position position);

    /// <summary>The smallest start that is &gt; <paramref name="position"/>, or null when there is none.</summary>
    public abstract Position? NextStartStrict(Position position);

    /// <summary>Like <see cref="NextStartStrict"/>, but the document end when there is no next start.</summary>
    public Position NextStart(Position position) => NextStartStrict(position) ?? Document.DocumentEnd;

    /// <summary>Creates the starts of <paramref name="unit"/>.</summary>
    public static AutomationUnitStarts Create(AutomationTextDocument document, AutomationTextUnit unit) => unit switch
    {
        AutomationTextUnit.Character => new CharacterUnitStarts(document),
        AutomationTextUnit.Paragraph => new ParagraphUnitStarts(document),
        AutomationTextUnit.Document => new DocumentUnitStarts(document),
        AutomationTextUnit.Format or AutomationTextUnit.Word or AutomationTextUnit.Line or AutomationTextUnit.Page =>
            throw new NotSupportedException($"The {unit} unit is added by a later card."),
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
    };
}
