using LoomEditor.View.Accessibility;
using static LoomEditor.View.Tests.Accessibility.TestHelpers.AutomationTestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class AutomationUnitStartsTests
{
    // Removed by P7-07
    [Fact]
    public void Create_Word_NotSupportedYet() =>
        WithDocument("a", d => Assert.Throws<NotSupportedException>(() => AutomationUnitStarts.Create(d, AutomationTextUnit.Word)));

    [Fact]
    public void Create_UndefinedUnit_Throws() =>
        WithDocument("a", d => Assert.Throws<ArgumentOutOfRangeException>(() => AutomationUnitStarts.Create(d, (AutomationTextUnit)99)));
}
