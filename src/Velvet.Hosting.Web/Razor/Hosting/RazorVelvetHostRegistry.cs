using System.Collections.Concurrent;

namespace Velvet.Hosting.Web.Razor.Hosting;

internal sealed class RazorVelvetHostRegistry
{
    private readonly ConcurrentDictionary<string, RazorVelvetHost> _hosts = new(StringComparer.Ordinal);

    public async Task ReplaceHostAsync(string canvasId, RazorVelvetHost host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        ArgumentNullException.ThrowIfNull(host);

        RazorVelvetHost? previous = null;
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
