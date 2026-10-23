using LoomEditor.View.Accessibility;
using static LoomEditor.View.Tests.Accessibility.TestHelpers.AutomationTestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class AutomationUnitStartsTests
{
    [Fact]
    public void Create_Format_IsTheWordUnit() => WithDocument("foo.bar  baz\n\n  x", d =>
    {
        var format = AutomationUnitStarts.Create(d, AutomationTextUnit.Format);
        var word = AutomationUnitStarts.Create(d, AutomationTextUnit.Word);
        Assert.IsType<WordUnitStarts>(format);
        for (int line = 1; line <= 3; line++)
        {
            for (int column = 1; column <= d.Model.GetLineMaxColumn(line); column++)
            {
                var p = Pos(line, column);
                Assert.Equal(word.StartAtOrBefore(p), format.StartAtOrBefore(p));
                Assert.Equal(word.PreviousStart(p), format.PreviousStart(p));
                Assert.Equal(word.NextStartStrict(p), format.NextStartStrict(p));
            }
        }
    });

    [Theory]
    [InlineData(AutomationTextUnit.Character)]
    [InlineData(AutomationTextUnit.Format)]
    [InlineData(AutomationTextUnit.Word)]
    [InlineData(AutomationTextUnit.Line)]
    [InlineData(AutomationTextUnit.Paragraph)]
    [InlineData(AutomationTextUnit.Page)]
    [InlineData(AutomationTextUnit.Document)]
    public void Create_EveryUnit_ReturnsStarts(object unit) =>
        WithDocument("ab\ncd", d => Assert.Equal(d.DocumentStart, AutomationUnitStarts.Create(d, (AutomationTextUnit)unit).StartAtOrBefore(d.DocumentStart)));

    [Fact]
    public void Create_UndefinedUnit_Throws() =>
        WithDocument("a", d => Assert.Throws<ArgumentOutOfRangeException>(() => AutomationUnitStarts.Create(d, (AutomationTextUnit)99)));
}
