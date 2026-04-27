using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using Velvet.Graphics.WebGL;

namespace Velvet.Hosting.Web;

public static class ServiceExtensions
{
    /// <summary>
    /// Registers Velvet web-host defaults for dependency injection.
    /// </summary>
    public static IServiceCollection AddVelvetHost(
        this IServiceCollection services,
        Func<IWebGLBridge, Task<ShaderProgram>>? defaultProgramFactory = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Replace(ServiceDescriptor.Singleton(new VelvetHostServices(
            defaultProgramFactory ?? ShaderProgram.CreateDefaultAsync)));

        return services;
    }

    /// <summary>
    /// Creates a <see cref="BlazorVelvetHost"/> from DI services for a canvas.
    /// </summary>
    public static Task<BlazorVelvetHost> CreateVelvetHostAsync(
        this IServiceProvider services,
        ElementReference canvas,
        Func<IWebGLBridge, Task<ShaderProgram>>? programFactory = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var js = services.GetRequiredService<IJSRuntime>();
        var defaults = services.GetService<VelvetHostServices>();
        var selectedFactory = programFactory ?? defaults?.ProgramFactory;

        return BlazorVelvetHost.CreateAsync(canvas, js, selectedFactory);
    }

    private sealed class VelvetHostServices
    {
        public VelvetHostServices(Func<IWebGLBridge, Task<ShaderProgram>> programFactory)
        {
            ProgramFactory = programFactory;
        }

        public Func<IWebGLBridge, Task<ShaderProgram>> ProgramFactory { get; }
    }
}
