using System.Collections.Concurrent;

namespace Velvet.Hosting.Web.MvcRuntime;

internal sealed class MvcVelvetHostRegistry
{
    private readonly ConcurrentDictionary<string, MvcVelvetHost> _hosts = new(StringComparer.Ordinal);

    public async Task ReplaceHostAsync(string canvasId, MvcVelvetHost host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        ArgumentNullException.ThrowIfNull(host);

        MvcVelvetHost? previous = null;
        _hosts.AddOrUpdate(
            canvasId,
            _ => host,
            (_, existing) =>
            {
                previous = existing;
                return host;
            });

        if (previous is null || ReferenceEquals(previous, host))
        {
            return;
        }

        await previous.StopAsync().ConfigureAwait(false);
    }
}
