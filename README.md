# Velvet Graphics Engine

A minimal cross-platform graphics engine inspired by Three.js/Babylon.js. The repository now contains:

- `Velvet.Core` – pure C# engine primitives (no JS dependencies)
- `Velvet.WebGL` – WebGL backend, bridge abstractions, and the shared `velvet.js`
- `Velvet.Blazor` – helper glue for Blazor apps to configure the WebGL backend
- `Velvet.Demo.Blazor` – Blazor WebAssembly demo rendering a triangle
- `Velvet.Demo.Static` – plain .NET WebAssembly demo that boots via a simple HTML page (no Blazor)

## Usage

```csharp
var app = new VelvetApp();
app.UseGraphics(new WebGLDevice());
app.Add(new DrawTriangle());
await app.RunAsync();
```

On Blazor, configure the bridge before running:

```csharp
JsBridge.Configure(new BlazorWebGLBridge(JSRuntime));
var app = new VelvetApp();
app.UseGraphics(new WebGLDevice());
app.Add(new DrawTriangle());
await app.RunAsync();
```

On static WebAssembly (no Blazor):

```csharp
JsBridge.Configure(new StaticWebGLBridge());
var app = new VelvetApp();
app.UseGraphics(new WebGLDevice());
app.Add(new DrawTriangle());
await app.RunAsync();
```

## Running demos

### Blazor demo

```powershell
cd Velvet/src
dotnet run --project Velvet.Demo.Blazor
dotnet watch --project Velvet.Demo.Blazor run (for debug)
```

Browse to the logged `localhost` port to see the triangle.

### Static demo

```powershell
cd Velvet
dotnet publish Velvet.Demo.Static -c Release
```

Serve the contents of `Velvet.Demo.Static\bin\Release\net9.0\browser-wasm\publish\wwwroot` (e.g., via `npx serve`).
