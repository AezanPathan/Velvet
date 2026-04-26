using Microsoft.AspNetCore.Builder;
using Velvet.Hosting.Web.Razor.Runtime;

namespace Velvet.Hosting.Web.Razor.Setup;

public static class RazorVelvetApplicationBuilderExtensions
{
    public static WebApplication UseRazorVelvetRuntime(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        RazorVelvetEntry.Configure(app.Services);
        return app;
    }
}
