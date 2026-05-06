using Microsoft.JSInterop;

namespace Velvet.Hosting.Web.Razor.Runtime;

internal sealed class RazorVelvetRuntimeAccessor
{
    private readonly object _sync = new();
    private IJSRuntime? _current;

    public IJSRuntime? Current
    {
        get
        {
            lock (_sync)
            {
                return _current;
            }
        }
    }

    public void Set(IJSRuntime jsRuntime)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        lock (_sync)
        {
            _current = jsRuntime;
        }
    }

    public void Clear(IJSRuntime jsRuntime)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_current, jsRuntime))
            {
                _current = null;
            }
        }
    }
}
