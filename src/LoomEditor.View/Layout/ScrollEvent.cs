// Ported from microsoft/vscode: src/vs/base/common/scrollable.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>Describes a change of the <see cref="ScrollState"/>. Monaco: <c>ScrollEvent</c>.</summary>
/// <remarks>Threading: immutable value, safe from any thread.</remarks>
internal readonly record struct ScrollEvent(
    bool InSmoothScrolling,
    double OldWidth,
    double OldScrollWidth,
    double OldScrollLeft,
    double Width,
    double ScrollWidth,
    double ScrollLeft,
    double OldHeight,
    double OldScrollHeight,
    double OldScrollTop,
    double Height,
    double ScrollHeight,
    double ScrollTop,
    bool WidthChanged,
    bool ScrollWidthChanged,
    bool ScrollLeftChanged,
    bool HeightChanged,
    bool ScrollHeightChanged,
    bool ScrollTopChanged);
