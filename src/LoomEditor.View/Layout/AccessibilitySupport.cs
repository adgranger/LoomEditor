// Ported from microsoft/vscode: src/vs/platform/accessibility/common/accessibility.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>Whether a screen reader is attached (Monaco: <c>AccessibilitySupport</c>).</summary>
internal enum AccessibilitySupport
{
    /// <summary>Not known (Monaco: <c>'auto'</c> for the option, "not detected yet" for the environment).</summary>
    Unknown = 0,

    /// <summary>No screen reader.</summary>
    Disabled = 1,

    /// <summary>A screen reader is attached.</summary>
    Enabled = 2,
}
