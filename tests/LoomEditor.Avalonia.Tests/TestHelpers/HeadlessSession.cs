using Avalonia.Headless;

namespace LoomEditor.Avalonia.Tests.TestHelpers;

/// <summary>One headless Avalonia session for the whole test assembly; tests run their UI code on its thread.</summary>
internal static class HeadlessSession
{
    private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.StartNew(typeof(TestApp));

    public static Task RunAsync(Action action) => Session.Dispatch(action, CancellationToken.None);

    public static Task<T> RunAsync<T>(Func<T> function) => Session.Dispatch(function, CancellationToken.None);
}
