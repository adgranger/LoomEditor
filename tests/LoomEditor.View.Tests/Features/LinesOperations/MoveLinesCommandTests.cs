using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.LinesOperations;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Features.LinesOperations;

// Monaco: suites 'Editor Contrib - Move Lines Command', '... honors Indentation Rules' and '... honors onEnter Rules'
public class MoveLinesCommandTests
{
    private static readonly string[] Lines = ["first", "second line", "third line", "fourth line", "fifth"];

    private static void TestMoveLinesDownCommand(string[] lines, Selection selection, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(
            lines,
            selection,
            (services, sel) => new MoveLinesCommand(sel, true, EditorAutoIndentStrategy.Advanced, services.LanguageConfigurationRegistry),
            expectedLines,
            expectedSelection);

    private static void TestMoveLinesUpCommand(string[] lines, Selection selection, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(
            lines,
            selection,
            (services, sel) => new MoveLinesCommand(sel, false, EditorAutoIndentStrategy.Advanced, services.LanguageConfigurationRegistry),
            expectedLines,
            expectedSelection);

    private static void TestMoveLinesWithIndentCommand(
        bool down,
        string? languageId,
        string[] lines,
        Selection selection,
        string[] expectedLines,
        Selection expectedSelection,
        LanguageConfiguration? configuration = null) =>
        CommandTester.TestCommand(
            lines,
            selection,
            (services, sel) => new MoveLinesCommand(sel, down, EditorAutoIndentStrategy.Full, services.LanguageConfigurationRegistry),
            expectedLines,
            expectedSelection,
            languageId: languageId,
            prepare: services =>
            {
                if (languageId is not null)
                {
                    services.RegisterLanguage(languageId);
                    services.Add(services.LanguageConfigurationRegistry.Register(languageId, configuration!));
                }
            });

    // Monaco: 'move first up / last down disabled'
    [Fact]
    public void MoveFirstUpLastDownDisabled()
    {
        TestMoveLinesUpCommand(Lines, new Selection(1, 1, 1, 1), Lines, new Selection(1, 1, 1, 1));
        TestMoveLinesDownCommand(Lines, new Selection(5, 1, 5, 1), Lines, new Selection(5, 1, 5, 1));
    }

    // Monaco: 'move first line down'
    [Fact]
    public void MoveFirstLineDown() =>
        TestMoveLinesDownCommand(
            Lines,
            new Selection(1, 4, 1, 1),
            ["second line", "first", "third line", "fourth line", "fifth"],
            new Selection(2, 4, 2, 1));

    // Monaco: 'move 2nd line up'
    [Fact]
    public void Move2ndLineUp() =>
        TestMoveLinesUpCommand(
            Lines,
            new Selection(2, 1, 2, 1),
            ["second line", "first", "third line", "fourth line", "fifth"],
            new Selection(1, 1, 1, 1));

    // Monaco: 'issue #1322a: move 2nd line up'
    [Fact]
    public void Issue1322a_Move2ndLineUp() =>
        TestMoveLinesUpCommand(
            Lines,
            new Selection(2, 12, 2, 12),
            ["second line", "first", "third line", "fourth line", "fifth"],
            new Selection(1, 12, 1, 12));

    // Monaco: 'issue #1322b: move last line up'
    [Fact]
    public void Issue1322b_MoveLastLineUp() =>
        TestMoveLinesUpCommand(
            Lines,
            new Selection(5, 6, 5, 6),
            ["first", "second line", "third line", "fifth", "fourth line"],
            new Selection(4, 6, 4, 6));

    // Monaco: 'issue #1322c: move last line selected up'
    [Fact]
    public void Issue1322c_MoveLastLineSelectedUp() =>
        TestMoveLinesUpCommand(
            Lines,
            new Selection(5, 6, 5, 1),
            ["first", "second line", "third line", "fifth", "fourth line"],
            new Selection(4, 6, 4, 1));

    // Monaco: 'move last line up'
    [Fact]
    public void MoveLastLineUp() =>
        TestMoveLinesUpCommand(
            Lines,
            new Selection(5, 1, 5, 1),
            ["first", "second line", "third line", "fifth", "fourth line"],
            new Selection(4, 1, 4, 1));

    // Monaco: 'move 4th line down'
    [Fact]
    public void Move4thLineDown() =>
        TestMoveLinesDownCommand(
            Lines,
            new Selection(4, 1, 4, 1),
            ["first", "second line", "third line", "fifth", "fourth line"],
            new Selection(5, 1, 5, 1));

    // Monaco: 'move multiple lines down'
    [Fact]
    public void MoveMultipleLinesDown() =>
        TestMoveLinesDownCommand(
            Lines,
            new Selection(4, 4, 2, 2),
            ["first", "fifth", "second line", "third line", "fourth line"],
            new Selection(5, 4, 3, 2));

    // Monaco: 'invisible selection is ignored'
    [Fact]
    public void InvisibleSelectionIsIgnored() =>
        TestMoveLinesDownCommand(
            Lines,
            new Selection(2, 1, 1, 1),
            ["second line", "first", "third line", "fourth line", "fifth"],
            new Selection(3, 1, 2, 1));

    private const string IndentModeId = "moveLinesIndentMode";

    private static readonly LanguageConfiguration IndentMode = new()
    {
        IndentationRules = new IndentationRule(
            DecreaseIndentPattern: new Regex(@"^\s*((?!\S.*\/[*]).*[*]\/\s*)?[})\]]|^\s*(case\b.*|default):\s*(\/\/.*|\/[*].*[*]\/\s*)?$", RegexOptions.CultureInvariant),
            IncreaseIndentPattern: new Regex(@"(\{[^}""'`]*|\([^)""']*|\[[^\]""']*|^\s*(\{\}|\(\)|\[\]|(case\b.*|default):))\s*(\/\/.*|\/[*].*[*]\/\s*)?$", RegexOptions.CultureInvariant),
            IndentNextLinePattern: new Regex(@"^\s*(for|while|if|else)\b(?!.*[;{}]\s*(\/\/.*|\/[*].*[*]\/\s*)?$)", RegexOptions.CultureInvariant),
            UnIndentedLinePattern: new Regex(@"^(?!.*([;{}]|\S:)\s*(\/\/.*|\/[*].*[*]\/\s*)?$)(?!.*(\{[^}""']*|\([^)""']*|\[[^\]""']*|^\s*(\{\}|\(\)|\[\]|(case\b.*|default):))\s*(\/\/.*|\/[*].*[*]\/\s*)?$)(?!^\s*((?!\S.*\/[*]).*[*]\/\s*)?[})\]]|^\s*(case\b.*|default):\s*(\/\/.*|\/[*].*[*]\/\s*)?$)(?!^\s*(for|while|if|else)\b(?!.*[;{}]\s*(\/\/.*|\/[*].*[*]\/\s*)?$))", RegexOptions.CultureInvariant)),
    };

    // Monaco: 'first line indentation adjust to 0'
    [Fact]
    public void FirstLineIndentationAdjustTo0() =>
        TestMoveLinesWithIndentCommand(
            false,
            IndentModeId,
            ["class X {", "\tz = 2", "}"],
            new Selection(2, 1, 2, 1),
            ["z = 2", "class X {", "}"],
            new Selection(1, 1, 1, 1),
            IndentMode);

    // Monaco: 'move lines across block'
    [Fact]
    public void MoveLinesAcrossBlock() =>
        TestMoveLinesWithIndentCommand(
            true,
            IndentModeId,
            ["const value = 2;", "const standardLanguageDescriptions = [", "    {", "        diagnosticSource: 'js',", "    }", "];"],
            new Selection(1, 1, 1, 1),
            ["const standardLanguageDescriptions = [", "    const value = 2;", "    {", "        diagnosticSource: 'js',", "    }", "];"],
            new Selection(2, 5, 2, 5),
            IndentMode);

    // Monaco: 'move line should still work as before if there is no indentation rules'
    [Fact]
    public void MoveLineShouldStillWorkAsBeforeIfThereIsNoIndentationRules() =>
        TestMoveLinesWithIndentCommand(
            false,
            null,
            ["if (true) {", "    var task = new Task(() => {", "        var work = 1234;", "    });", "}"],
            new Selection(3, 1, 3, 1),
            ["if (true) {", "        var work = 1234;", "    var task = new Task(() => {", "    });", "}"],
            new Selection(2, 1, 2, 1));

    // Monaco: 'issue #54829. move block across block'
    [Fact]
    public void Issue54829_MoveBlockAcrossBlock() =>
        TestMoveLinesWithIndentCommand(
            true,
            "moveLinesEnterMode",
            [
                "if (true) {",
                "    if (false) {",
                "        if (1) {",
                "            console.log('b');",
                "        }",
                "        console.log('a');",
                "    }",
                "}",
            ],
            new Selection(3, 9, 5, 10),
            [
                "if (true) {",
                "    if (false) {",
                "        console.log('a');",
                "        if (1) {",
                "            console.log('b');",
                "        }",
                "    }",
                "}",
            ],
            new Selection(4, 9, 6, 10),
            new LanguageConfiguration
            {
                IndentationRules = new IndentationRule(
                    DecreaseIndentPattern: new Regex(@"^\s*\[$", RegexOptions.CultureInvariant),
                    IncreaseIndentPattern: new Regex(@"^\s*\]$", RegexOptions.CultureInvariant)),
                Brackets = [new CharacterPair("{", "}")],
            });
}
