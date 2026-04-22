using Microsoft.AspNetCore.Builder;

namespace Velvet.Hosting.Web.MvcRuntime;

public static class MvcVelvetApplicationBuilderExtensions
{
    public static WebApplication UseMvcVelvetRuntime(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        MvcVelvetEntry.Configure(app.Services);
        return app;
    }
}
