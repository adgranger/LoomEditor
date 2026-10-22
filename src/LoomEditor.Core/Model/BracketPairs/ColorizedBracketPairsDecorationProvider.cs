// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/colorizedBracketPairsDecorationProvider.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.BracketPairs;

/// <summary>
/// The decorations that color the brackets by nesting level: one inline decoration per colorized bracket.
/// </summary>
/// <remarks>
/// The class names are drawn by the <c>DecorationStyles</c> rows of the View (Monaco's theming participant). The 31
/// options are created once. Threading: UI thread only.
/// </remarks>
internal sealed class ColorizedBracketPairsDecorationProvider : IDecorationProvider
{
    /// <summary>The class name of an unexpected closing bracket (Monaco: <c>unexpectedClosingBracketClassName</c>).</summary>
    public const string UnexpectedClosingBracketClassName = "unexpected-closing-bracket";

    // Monaco: `level % 30`, a number that is a lcm of all numbers from 1 to 6, to support up to 6 colors.
    private const int LevelCount = 30;

    private static readonly ModelDecorationOptions UnexpectedOptions = CreateOptions(UnexpectedClosingBracketClassName);

    private static readonly ModelDecorationOptions[] LevelOptions =
        [.. Enumerable.Range(0, LevelCount).Select(level => CreateOptions(ClassNameOfLevel(level)))];

    private readonly TextModel _textModel;
    private BracketPairColorizationOptions _colorizationOptions;

    public ColorizedBracketPairsDecorationProvider(TextModel textModel)
    {
        _textModel = textModel;
        _colorizationOptions = textModel.BracketPairColorizationOptions;
        textModel.BracketPairs.Changed += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised when the colors of the brackets may have changed (Monaco: <c>onDidChange</c>).</summary>
    public event EventHandler? Changed;

    /// <summary>The class name of the decoration of the brackets of a nesting level (Monaco: <c>getInlineClassNameOfLevel</c>).</summary>
    public static string ClassNameOfLevel(int level) => $"bracket-highlighting-{level % LevelCount}";

    /// <summary>Monaco: <c>handleDidChangeOptions</c>.</summary>
    public void HandleDidChangeOptions() => _colorizationOptions = _textModel.BracketPairColorizationOptions;

    /// <inheritdoc />
    public IReadOnlyList<ModelDecoration> GetDecorationsInRange(Range range, int ownerId, bool filterOutValidation, bool onlyMinimapDecorations = false)
    {
        // Bracket pair colorization decorations are not rendered in the minimap.
        if (onlyMinimapDecorations)
        {
            return [];
        }

        // Monaco returns nothing when `ownerId === undefined`: the model always passes one.
        if (!_colorizationOptions.Enabled)
        {
            return [];
        }

        List<ModelDecoration> result = [];
        _textModel.BracketPairs.GetBracketsInRange(range, onlyColorizedBrackets: true).ForEach(bracket =>
        {
            int level = bracket.IsInvalid
                ? -1
                : _colorizationOptions.IndependentColorPoolPerBracketType ? bracket.NestingLevelOfEqualBracketType : bracket.NestingLevel;
            result.Add(new ModelDecoration(
                $"bracket{bracket.Range}-{bracket.NestingLevel}",
                0,
                bracket.Range,
                level < 0 ? UnexpectedOptions : LevelOptions[level % LevelCount]));
        });
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelDecoration> GetAllDecorations(int ownerId, bool filterOutValidation) =>
        _colorizationOptions.Enabled
            ? GetDecorationsInRange(new Range(1, 1, _textModel.LineCount, 1), ownerId, filterOutValidation)
            : [];

    private static ModelDecorationOptions CreateOptions(string inlineClassName) =>
        ModelDecorationOptions.Register(new ModelDecorationOptionsData { Description = "BracketPairColorization", InlineClassName = inlineClassName });
}
