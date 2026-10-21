// Ported from microsoft/vscode: src/vs/editor/contrib/comment/test/browser/lineCommentCommand.test.ts (suite 'Editor Contrib - Line Comment in mixed modes')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Comment;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using LoomEditor.View.Tests.Features.Comment.TestHelpers;

namespace LoomEditor.View.Tests.Features.Comment;

public partial class LineCommentCommandTests
{
    private const string OuterLanguageId = "outerMode";
    private const string InnerLanguageId = "innerMode";

    // Monaco: testLineCommentCommand of the suite 'Editor Contrib - Line Comment in mixed modes'
    private static void TestMixedModes(string[] lines, Selection selection, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(
            lines,
            selection,
            (services, sel) => new LineCommentCommand(services.LanguageConfigurationRegistry, sel, 4, LineCommentType.Toggle, true, true),
            expectedLines,
            expectedSelection,
            languageId: OuterLanguageId,
            prepare: services =>
            {
                services.RegisterLanguage(InnerLanguageId);
                services.Add(services.LanguageConfigurationRegistry.Register(InnerLanguageId, new LanguageConfiguration
                {
                    Comments = new CommentRule(null, new CharacterPair("{/*", "*/}")),
                }));

                services.RegisterLanguage(OuterLanguageId);
                services.Add(services.LanguageConfigurationRegistry.Register(OuterLanguageId, new LanguageConfiguration
                {
                    Comments = new CommentRule(new LineCommentConfig("//"), new CharacterPair("/*", "*/")),
                }));
                services.Add(services.TokenizationRegistry.Register(
                    OuterLanguageId,
                    new LanguagePerLineTokenizationSupport(
                        services.LanguageIdCodec.EncodeLanguageId(OuterLanguageId),
                        services.LanguageIdCodec.EncodeLanguageId(InnerLanguageId))));
            },
            forceTokenization: true);

    // Monaco: 'issue #24047 (part 1): Commenting code in JSX files'
    [Fact]
    public void Issue24047_Part1_CommentingCodeInJsxFiles() =>
        TestMixedModes(
            [
                "import React from 'react';",
                "const Loader = () => (",
                "  <div>",
                "    Loading...",
                "  </div>",
                ");",
                "export default Loader;",
            ],
            new Selection(1, 1, 7, 22),
            [
                "// import React from 'react';",
                "// const Loader = () => (",
                "//   <div>",
                "//     Loading...",
                "//   </div>",
                "// );",
                "// export default Loader;",
            ],
            new Selection(1, 4, 7, 25));

    // Monaco: 'issue #24047 (part 2): Commenting code in JSX files'
    [Fact]
    public void Issue24047_Part2_CommentingCodeInJsxFiles() =>
        TestMixedModes(
            [
                "import React from 'react';",
                "const Loader = () => (",
                "  <div>",
                "    Loading...",
                "  </div>",
                ");",
                "export default Loader;",
            ],
            new Selection(3, 4, 3, 4),
            [
                "import React from 'react';",
                "const Loader = () => (",
                "  {/* <div> */}",
                "    Loading...",
                "  </div>",
                ");",
                "export default Loader;",
            ],
            new Selection(3, 8, 3, 8));

    // Monaco: 'issue #36173: Commenting code in JSX tag body'
    [Fact]
    public void Issue36173_CommentingCodeInJsxTagBody() =>
        TestMixedModes(
            ["<div>", "  {123}", "</div>"],
            new Selection(2, 4, 2, 4),
            ["<div>", "  {/* {123} */}", "</div>"],
            new Selection(2, 8, 2, 8));
}
