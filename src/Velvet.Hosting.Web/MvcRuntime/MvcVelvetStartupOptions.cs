namespace Velvet.Hosting.Web.MvcRuntime;

public sealed class MvcVelvetStartupOptions
{
    private Func<MvcVelvetStartupContext, Task>? _startup;

    public Func<MvcVelvetStartupContext, Task> Startup
        => _startup ?? throw new InvalidOperationException("MVC Velvet startup callback is not configured.");

    public void Configure(Func<MvcVelvetStartupContext, Task> startup)
    {
        ArgumentNullException.ThrowIfNull(startup);
        _startup = startup;
    }
}
