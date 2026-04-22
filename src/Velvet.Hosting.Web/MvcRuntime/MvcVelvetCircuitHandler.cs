using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.JSInterop;

namespace Velvet.Hosting.Web.MvcRuntime;

internal sealed class MvcVelvetCircuitHandler : CircuitHandler
{
    private readonly MvcVelvetRuntimeAccessor _runtimeAccessor;
    private readonly IJSRuntime _jsRuntime;

    public MvcVelvetCircuitHandler(MvcVelvetRuntimeAccessor runtimeAccessor, IJSRuntime jsRuntime)
    {
        _runtimeAccessor = runtimeAccessor;
        _jsRuntime = jsRuntime;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _runtimeAccessor.Set(_jsRuntime);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _runtimeAccessor.Clear(_jsRuntime);
        return Task.CompletedTask;
    }
}
