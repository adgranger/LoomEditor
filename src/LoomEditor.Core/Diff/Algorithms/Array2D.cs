// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/utils.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Diff.Algorithms;

/// <summary>A 2D array over one flat array, indexed <c>x + y * width</c> (Monaco: <c>Array2D</c>).</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class Array2D<T>(int width, int height)
{
    private readonly T[] _array = new T[width * height];

    /// <summary>The number of columns.</summary>
    public int Width { get; } = width;

    /// <summary>The number of rows.</summary>
    public int Height { get; } = height;

    /// <summary><c>get</c>.</summary>
    public T Get(int x, int y) => _array[x + y * Width];

    /// <summary><c>set</c>.</summary>
    public void Set(int x, int y, T value) => _array[x + y * Width] = value;
}
