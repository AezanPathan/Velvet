# Velvet Graphics Engine

A minimal cross-platform graphics engine inspired by Three.js/Babylon.js. The repository now contains:

- `Velvet.Core` – pure C# engine primitives (no JS dependencies)
- `Velvet.Graphics.WebGL` – WebGL backend, bridge abstractions, and the shared `velvet.js`
- `Velvet.Hosting.Web` – helper glue for Blazor apps to configure the WebGL backend
- `Velvet-Site` – Blazor WebAssembly sample app using the projects above

## Usage

The current hosting API is Blazor-first via `Velvet.Hosting.Web.VelvetHost`.

```csharp
var app = await VelvetHost.CreateAsync(canvasRef, JS, ShaderProgram.CreateDefaultAsync);
app.Add(scene);
app.Camera = camera;
await app.StartAsync();
```

When your component/page is disposed:

```csharp
await app.StopAsync();
```

## Running demos

### Velvet-Site (Blazor WebAssembly)

```powershell
cd Velvet
dotnet run --project Velvet-Site/Velvet-Site.csproj
```

Browse to the logged `localhost` URL to open the sample scenes.
