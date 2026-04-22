using Microsoft.JSInterop;

namespace Velvet.Hosting.Web.MvcRuntime;

internal sealed class MvcVelvetRuntimeAccessor
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
