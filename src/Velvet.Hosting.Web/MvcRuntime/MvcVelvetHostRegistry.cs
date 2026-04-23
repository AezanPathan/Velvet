using System.Collections.Concurrent;

namespace Velvet.Hosting.Web.MvcRuntime;

internal sealed class MvcVelvetHostRegistry
{
    private readonly ConcurrentDictionary<string, MvcVelvetHost> _hosts = new(StringComparer.Ordinal);

    public async Task ReplaceHostAsync(string canvasId, MvcVelvetHost host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        ArgumentNullException.ThrowIfNull(host);

        var previous = _hosts.AddOrUpdate(canvasId, host, (_, _) => host);
        if (ReferenceEquals(previous, host))
        {
            return;
        }

        await previous.StopAsync().ConfigureAwait(false);
    }
}
