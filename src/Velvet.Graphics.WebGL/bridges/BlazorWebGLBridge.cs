using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Velvet.Graphics.WebGL;

/// <summary>
/// Blazor bridge implementation that talks to the Velvet JavaScript API via IJSRuntime.
/// This implementation intentionally avoids a compile-time dependency on Blazor types
/// (e.g. ElementReference) so the Velvet.Graphics.WebGL project remains host-agnostic.
/// 
/// At runtime, Blazor should pass an ElementReference object; IJSRuntime will marshal it.
/// </summary>
public sealed class BlazorWebGLBridge : JsRuntimeWebGLBridgeBase
{
    #region Constructor

    public BlazorWebGLBridge(IJSRuntime js)
        : base(js)
    {
    }

    #endregion

    #region Initialization

    /// <inheritdoc />
    /// <remarks>
    /// The parameter is declared as <see cref="object"/> to avoid a compile-time
    /// dependency on Microsoft.AspNetCore.Components. When calling from a Razor
    /// component, pass the ElementReference (e.g. `await Bridge.InitWithElementAsync(canvasRef)`).
    /// </remarks>
    public override Task<int> InitWithElementAsync(object canvasElement)
    {
        if (canvasElement is null) throw new ArgumentNullException(nameof(canvasElement));

        // We intentionally do not type-check for ElementReference at compile-time.
        // If you want compile-time checks, add a reference to Microsoft.AspNetCore.Components and
        // change the parameter type to ElementReference.
        return Js.InvokeAsync<int>("Velvet.init", canvasElement).AsTask();
    }

    /// <inheritdoc />
    public override Task<int> InitWithIdAsync(string canvasId)
    {
        // Blazor bridge intentionally does not support string id init.
        throw new NotSupportedException("BlazorWebGLBridge does not support InitWithIdAsync. Use InitWithElementAsync with an ElementReference.");
    }

    #endregion

}
