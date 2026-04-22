using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Velvet.Graphics.WebGL;

namespace Velvet.Hosting.Web.MvcRuntime;

public static class MvcVelvetEntry
{
    private static IServiceProvider? _services;
    private static int _startupState; // 0 = not started, 1 = starting, 2 = started

    internal static void Configure(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    [JSInvokable]
    public static async Task Start(string canvasId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);

        if (Volatile.Read(ref _startupState) == 2)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _startupState, 1, 0) != 0)
        {
            while (Volatile.Read(ref _startupState) == 1)
            {
                await Task.Delay(10).ConfigureAwait(false);
            }

            return;
        }

        try
        {
            var services = _services ?? throw new InvalidOperationException(
                "MVC Velvet runtime is not configured. Call app.UseMvcVelvetRuntime() at startup.");

            var options = services.GetRequiredService<MvcVelvetStartupOptions>();
            var runtimeAccessor = services.GetRequiredService<MvcVelvetRuntimeAccessor>();

            IJSRuntime? js = null;
            for (var i = 0; i < 100; i++)
            {
                js = runtimeAccessor.Current;
                if (js is not null)
                {
                    break;
                }

                await Task.Delay(50).ConfigureAwait(false);
            }

            if (js is null)
            {
                throw new InvalidOperationException(
                    "No active Blazor circuit runtime yet. Retry start after connection is established.");
            }

            var bridge = services.GetRequiredService<IWebGLBridge>();
            var context = MvcVelvetStartupContext.Create(canvasId, services, js, bridge);
            await options.Startup(context).ConfigureAwait(false);
            Volatile.Write(ref _startupState, 2);
        }
        catch
        {
            Volatile.Write(ref _startupState, 0);
            throw;
        }
    }
}
