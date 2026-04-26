using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Velvet.Graphics.WebGL;
using Microsoft.Extensions.DependencyInjection;
using Velvet.Hosting.Web.Razor.Hosting;
using Velvet.Hosting.Web.Razor.Runtime;
using Velvet.Hosting.Web.Razor.Scene;

namespace Velvet.Hosting.Web.Razor.Setup;

public static class RazorVelvetServiceCollectionExtensions
{
    public static IServiceCollection AddRazorVelvetHost(
        this IServiceCollection services,
        Action<RazorVelvetStartupOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddServerSideBlazor();

        services.TryAddSingleton<RazorVelvetRuntimeAccessor>();
        services.TryAddSingleton<RazorVelvetHostRegistry>();
        services.TryAddSingleton<RazorVelvetSceneRegistry>();
        services.TryAddSingleton<RazorVelvetSceneRuntime>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, RazorVelvetCircuitHandler>());

        var options = new RazorVelvetStartupOptions();
        configure(options);
        services.Replace(ServiceDescriptor.Singleton(options));

        return services;
    }
}
