using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Velvet.Graphics.WebGL;
using Microsoft.Extensions.DependencyInjection;
namespace Velvet.Hosting.Web.MvcRuntime;

public static class MvcVelvetServiceCollectionExtensions
{
    public static IServiceCollection AddMvcVelvetHost(
        this IServiceCollection services,
        Action<MvcVelvetStartupOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddServerSideBlazor();

        services.TryAddSingleton<MvcVelvetRuntimeAccessor>();
        services.TryAddSingleton<MvcVelvetHostRegistry>();
        services.TryAddSingleton<MvcVelvetSceneRegistry>();
        services.TryAddSingleton<MvcVelvetSceneRuntime>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, MvcVelvetCircuitHandler>());

        var options = new MvcVelvetStartupOptions();
        configure(options);
        services.Replace(ServiceDescriptor.Singleton(options));

        return services;
    }
}
