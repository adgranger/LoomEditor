using LoomEditor.View.Commands;

namespace LoomEditor.View.Tests.Commands;

public class CommandRegistryTests
{
    [Fact]
    public void Execute_RegisteredCommand_RunsIt()
    {
        var registry = new CommandRegistry();
        int calls = 0;
        registry.Register(new EditorCommand("test.run", () => calls++));

        Assert.True(registry.Execute("test.run"));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Execute_UnknownCommand_ReturnsFalse()
    {
        Assert.False(new CommandRegistry().Execute("does.not.exist"));
    }

    [Fact]
    public void Execute_CannotExecute_DoesNotRun()
    {
        var registry = new CommandRegistry();
        int calls = 0;
        registry.Register(new EditorCommand("test.run", () => calls++, canExecute: () => false));

        Assert.False(registry.Execute("test.run"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Register_DuplicateId_Throws()
    {
        var registry = new CommandRegistry();
        registry.Register(new EditorCommand("test.run", () => { }));

        Assert.Throws<InvalidOperationException>(() => registry.Register(new EditorCommand("test.run", () => { })));
    }

    [Fact]
    public void DisposingRegistration_RemovesCommand_AndIsIdempotent()
    {
        var registry = new CommandRegistry();
        IDisposable registration = registry.Register(new EditorCommand("test.run", () => { }));

        registration.Dispose();
        registry.Register(new EditorCommand("test.run", () => { }));
        registration.Dispose(); // must not remove the new command

        Assert.True(registry.Contains("test.run"));
    }
}
