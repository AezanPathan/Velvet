namespace Velvet.Hosting.Web.Razor.Setup;

public sealed class RazorVelvetStartupOptions
{
    private Func<RazorVelvetStartupContext, Task>? _startup;

    public Func<RazorVelvetStartupContext, Task> Startup
        => _startup ?? throw new InvalidOperationException("Razor Velvet startup callback is not configured.");

    public void Configure(Func<RazorVelvetStartupContext, Task> startup)
    {
        ArgumentNullException.ThrowIfNull(startup);
        _startup = startup;
    }
}
