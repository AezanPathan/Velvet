using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.JSInterop;

namespace Velvet.Hosting.Web.Razor.Runtime;

internal sealed class RazorVelvetCircuitHandler : CircuitHandler
{
    private readonly RazorVelvetRuntimeAccessor _runtimeAccessor;
    private readonly IJSRuntime _jsRuntime;

    public RazorVelvetCircuitHandler(RazorVelvetRuntimeAccessor runtimeAccessor, IJSRuntime jsRuntime)
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
