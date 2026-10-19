namespace LoomEditor.TextMate.Tests.Grammar;

// Ported from microsoft/vscode-textmate: src/tests/tokenization.test.ts, run on test-cases/suite1/tests.json.
public class Suite1Tests
{
    public static IEnumerable<TheoryDataRow<string, int>> Cases() => TokenizationSuiteRunner.Rows("suite1/tests.json");

    // vscode-textmate: test(tst.desc) — one row per case, named by its desc.
    [Theory]
    [MemberData(nameof(Cases))]
    public void Tokenizes(string testsFile, int index) => TokenizationSuiteRunner.Run(testsFile, index);
}
