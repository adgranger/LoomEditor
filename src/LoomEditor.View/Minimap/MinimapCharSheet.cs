// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimapCharSheet.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Minimap;

/// <summary>
/// The constants and character indexing of the minimap character sheet. Monaco: <c>Constants</c>, <c>allCharCodes</c>,
/// <c>getCharIndex</c>.
/// </summary>
/// <remarks>Threading: stateless, safe from any thread.</remarks>
internal static class MinimapCharSheet
{
    public const int StartChCode = 32; // Space
    public const int EndChCode = 126; // Tilde (~)
    public const int UnknownCode = 65533; // UTF placeholder code
    public const int CharCount = EndChCode - StartChCode + 2;

    public const int SampledCharHeight = 16;
    public const int SampledCharWidth = 10;

    public const int BaseCharHeight = 2;
    public const int BaseCharWidth = 1;

    public const int RgbaChannelsCnt = 4;
    public const int RgbaSampledRowWidth = RgbaChannelsCnt * CharCount * SampledCharWidth;

    /// <summary>The characters of the sheet: ' ' to '~', then the unknown-symbol placeholder.</summary>
    public static readonly int[] AllCharCodes = [.. Enumerable.Range(StartChCode, EndChCode - StartChCode + 1), UnknownCode];

    /// <summary>The index of a character in the sheet. Monaco: <c>getCharIndex</c>.</summary>
    public static int GetCharIndex(int chCode, int fontScale)
    {
        chCode -= StartChCode;
        if (chCode < 0 || chCode > CharCount)
        {
            if (fontScale <= 2)
            {
                // For smaller scales, we can get away with using any ASCII character...
                // (JavaScript's % keeps the sign of the dividend, like C#'s.)
                return (chCode + CharCount) % CharCount;
            }

            return CharCount - 1; // unknown symbol
        }

        return chCode;
    }

    /// <summary>
    /// Stores a computed value the way a JavaScript <c>Uint8ClampedArray</c> assignment does: round half to even, then
    /// clamp to 0–255.
    /// </summary>
    public static byte ClampToByte(double value) => (byte)Math.Clamp(Math.Round(value, MidpointRounding.ToEven), 0, 255);

    /// <summary>Monaco's <c>toUint8</c> (base/common/uint.ts): clamp to 0–255 and truncate.</summary>
    public static int ToUint8(double value) => value < 0 ? 0 : value > 255 ? 255 : (int)value;
}
