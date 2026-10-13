// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (randomChar, randomInt, randomStr)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

/// <summary>Monaco's random text helpers, driven by an explicit seeded <see cref="Random"/> so failures can be replayed.</summary>
internal static class RandomText
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ\r\n";

    // Monaco: randomChar
    public static char RandomChar(Random random) => Alphabet[random.Next(Alphabet.Length)];

    // Monaco: randomInt
    public static int RandomInt(Random random, int bound) => random.Next(bound);

    // Monaco: randomStr. Like upstream, the result has len - 1 characters (its loop runs from 1 to len exclusive).
    public static string RandomStr(Random random, int len)
    {
        var chars = new char[Math.Max(len - 1, 0)];
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = RandomChar(random);
        }

        return new string(chars);
    }
}
