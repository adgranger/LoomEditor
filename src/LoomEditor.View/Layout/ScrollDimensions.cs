// Ported from microsoft/vscode: src/vs/base/common/scrollable.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>Size of the viewport and of the scrollable content. Monaco: <c>IScrollDimensions</c>.</summary>
/// <remarks>Threading: read-only view, thread-safety is that of the implementation.</remarks>
internal interface IScrollDimensions
{
    double Width { get; }

    double ScrollWidth { get; }

    double Height { get; }

    double ScrollHeight { get; }
}

/// <summary>A scroll position. Monaco: <c>IScrollPosition</c>.</summary>
/// <remarks>Threading: read-only view, thread-safety is that of the implementation.</remarks>
internal interface IScrollPosition
{
    double ScrollLeft { get; }

    double ScrollTop { get; }
}

/// <summary>A scroll position plus the viewport size. Monaco: <c>ISmoothScrollPosition</c>.</summary>
/// <remarks>Threading: read-only view, thread-safety is that of the implementation.</remarks>
internal interface ISmoothScrollPosition
{
    double ScrollLeft { get; }

    double ScrollTop { get; }

    double Width { get; }

    double Height { get; }
}

/// <summary>Partial update of the dimensions; <c>null</c> keeps the current value. Monaco: <c>INewScrollDimensions</c>.</summary>
/// <remarks>Threading: immutable value, safe from any thread.</remarks>
internal readonly record struct NewScrollDimensions(
    double? Width = null,
    double? ScrollWidth = null,
    double? Height = null,
    double? ScrollHeight = null);

/// <summary>A scroll position as an immutable value.</summary>
/// <remarks>Threading: immutable value, safe from any thread.</remarks>
internal readonly record struct ScrollPosition(double ScrollLeft, double ScrollTop) : IScrollPosition;

/// <summary>Partial update of the position; <c>null</c> keeps the current value. Monaco: <c>INewScrollPosition</c>.</summary>
/// <remarks>Threading: immutable value, safe from any thread.</remarks>
internal readonly record struct NewScrollPosition(double? ScrollLeft = null, double? ScrollTop = null);

/// <summary>Immutable <see cref="ISmoothScrollPosition"/>.</summary>
/// <remarks>Threading: immutable value, safe from any thread.</remarks>
internal readonly record struct SmoothScrollPosition(double ScrollLeft, double ScrollTop, double Width, double Height)
    : ISmoothScrollPosition;
