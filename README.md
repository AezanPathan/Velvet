# Velvet Graphics Engine

A minimal cross-platform graphics engine inspired by Three.js/Babylon.js.

## Release status

Current aligned version: **0.1.0** (`Velvet.Core`, `Velvet.Graphics.WebGL`, `Velvet.Hosting.Web`, `Velvet-Site`, and WebGL package metadata).

Recent release-readiness updates:

- Material migration noise is contained: docs/samples use non-obsolete material APIs (`ShaderMaterial` and `Velvet.Core.Rendering.Material`), while the legacy alias remains compatibility-only.
- DI host setup is implemented via `Velvet.Hosting.Web.ServiceExtensions`.

## Projects

- `Velvet.Core` – pure C# engine primitives
- `Velvet.Graphics.WebGL` – WebGL backend and shared `velvet.js`
- `Velvet.Hosting.Web` – Blazor hosting/orchestration API
- `Velvet-Site` – Blazor WebAssembly sample app

## Usage

Direct host creation:

```csharp
var app = await VelvetHost.CreateAsync(canvasRef, JS, ShaderProgram.CreateDefaultAsync);
app.Add(scene);
app.Camera = camera;
await app.StartAsync();
```

DI-based setup with `ServiceExtensions`:

```csharp
// Program.cs
using Velvet.Hosting.Web;

builder.Services.AddVelvetHost(); // or AddVelvetHost(customProgramFactory)

// Component (.razor.cs)
[Inject] private IServiceProvider Services { get; set; } = default!;

app = await Services.CreateVelvetHostAsync(canvasRef); // optional per-host programFactory override
```

Shutdown:

```csharp
await app.StopAsync();
```

## Running demos

```powershell
cd Velvet
dotnet run --project Velvet-Site/Velvet-Site.csproj
```

⚠️ Razor (SSR) Host Status

The Razor-based hosting mode is currently experimental.

Due to the stateless nature of SSR and lifecycle limitations,
full runtime persistence (like continuous rendering loops) is not yet stable.

For production or demos, Blazor hosting is recommended.
