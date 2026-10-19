namespace LoomEditor.TextMate.Tests.Grammar;

// Ported from microsoft/vscode-textmate: src/tests/tokenization.test.ts, run on test-cases/first-mate/tests.json.
public class FirstMateTests
{
    public static IEnumerable<TheoryDataRow<string, int>> Cases() => TokenizationSuiteRunner.Rows("first-mate/tests.json");

    // vscode-textmate: test(tst.desc) — one row per case, named by its desc.
    [Theory]
    [MemberData(nameof(Cases))]
    public void Tokenizes(string testsFile, int index) => TokenizationSuiteRunner.Run(testsFile, index);
}
