// Ported from microsoft/vscode: src/vs/base/common/strings.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>Unicode grapheme break property of a code point (Monaco: <c>GraphemeBreakType</c>). The table stores these numbers.</summary>
internal enum GraphemeBreakType
{
    Other = 0,
    Prepend = 1,
    CR = 2,
    LF = 3,
    Control = 4,
    Extend = 5,
    RegionalIndicator = 6,
    SpacingMark = 7,
    L = 8,
    V = 9,
    T = 10,
    LV = 11,
    LVT = 12,
    ZWJ = 13,
    ExtendedPictographic = 14,
}
