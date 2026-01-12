# Velvet Blazor Cube Demo

This demo shows how to use the **Velvet Graphics Engine** from a Blazor WebAssembly application using JavaScript interop.

## Overview

The demo renders a rotating 3D cube using WebGL through the Velvet engine API. It demonstrates:

- ✅ Canvas initialization from Blazor
- ✅ Loading GLSL shaders from external files
- ✅ Creating and compiling shaders via JS interop
- ✅ Creating and linking GPU programs
- ✅ Uploading mesh geometry
- ✅ Setting uniform matrices (model/view/projection)
- ✅ Rendering in a JavaScript animation loop

## Architecture

```
Blazor C# (CubeDemo.razor)
    ↓ IJSRuntime
JavaScript (cube-demo.js)
    ↓ window.Velvet API
Velvet Engine (velvet.js)
    ↓
WebGL2
```

## File Structure

```
demos/Velvet.Demo.Blazor/
├── Pages/
│   └── CubeDemo.razor          # Blazor component (C# + Razor markup)
├── wwwroot/
│   ├── cube-demo.js             # JS animation loop & shader loading
│   └── shaders/
│       ├── simple.vert          # Vertex shader (GLSL 300 es)
│       └── simple.frag          # Fragment shader (GLSL 300 es)
└── README.md                    # This file
```

## API Usage from Blazor

### 1. Initialize the Engine

```csharp
@inject IJSRuntime JS

private ElementReference canvasElement;
private int rendererId;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // Pass canvas element to JS and get renderer ID back
        rendererId = await JS.InvokeAsync<int>("Velvet.init", canvasElement);
    }
}
```

### 2. Create Shaders

```csharp
// Load shader source (via HTTP or embedded resource)
string vertexSource = await LoadShaderAsync("shaders/simple.vert");
string fragmentSource = await LoadShaderAsync("shaders/simple.frag");

// Create and compile shaders
int vsId = await JS.InvokeAsync<int>("Velvet.createShader", vertexSource, "vertex");
int fsId = await JS.InvokeAsync<int>("Velvet.createShader", fragmentSource, "fragment");
```

### 3. Create and Link Program

```csharp
int programId = await JS.InvokeAsync<int>("Velvet.createProgram");
await JS.InvokeVoidAsync("Velvet.attachShader", programId, vsId);
await JS.InvokeVoidAsync("Velvet.attachShader", programId, fsId);
await JS.InvokeVoidAsync("Velvet.linkProgram", programId);
```

### 4. Create Mesh

```csharp
// Define cube vertices (36 floats per vertex: x,y,z,r,g,b)
float[] vertices = new float[] { /* cube data */ };

// Upload to GPU
int meshId = await JS.InvokeAsync<int>("Velvet.createMesh", vertices);
```

### 5. Start Animation Loop (in JavaScript)

```javascript
// cube-demo.js handles the animation loop
window.startCubeAnimation = function(meshId, programId, rendererId) {
    function animate() {
        // Update matrices
        window.Velvet.setUniformMatrix4fv(programId, "uModelMatrix", modelMatrix);
        
        // Clear and draw
        window.Velvet.clear(0.1, 0.1, 0.1, 1.0);
        window.Velvet.drawMesh(meshId, programId, rendererId);
        
        requestAnimationFrame(animate);
    }
    animate();
};
```

## Shader Interface

### Vertex Shader (`simple.vert`)

**Inputs:**
- `in vec3 aPosition` - Vertex position (location 0)
- `in vec3 aColor` - Vertex color (location 1)

**Uniforms:**
- `uniform mat4 uModelMatrix` - Model transformation
- `uniform mat4 uViewMatrix` - Camera view
- `uniform mat4 uProjectionMatrix` - Perspective projection

**Outputs:**
- `out vec3 vColor` - Color passed to fragment shader

### Fragment Shader (`simple.frag`)

**Inputs:**
- `in vec3 vColor` - Interpolated color from vertex shader

**Outputs:**
- `out vec4 fragColor` - Final pixel color

## Running the Demo

### Prerequisites
- .NET 8.0 or later
- Node.js (for building Velvet.WebGL)

### Build Steps

1. **Build the Velvet engine:**
   ```bash
   cd ../../src/Velvet.WebGL
   npm install
   npm run build
   ```

2. **Run the Blazor app:**
   ```bash
   cd ../../demos/Velvet.Demo.Blazor
   dotnet run
   ```

3. **Open browser:**
   ```
   https://localhost:5001
   ```

4. **Navigate to `/cubedemo`**

## Key Velvet API Methods

| Method | Parameters | Returns | Description |
|--------|-----------|---------|-------------|
| `Velvet.init(canvas)` | HTMLCanvasElement | int | Initialize renderer, returns rendererId |
| `Velvet.createShader(source, type)` | string, "vertex"\|"fragment" | int | Compile shader, returns shaderId |
| `Velvet.createProgram()` | - | int | Create GPU program, returns programId |
| `Velvet.attachShader(progId, shaderId)` | int, int | void | Attach shader to program |
| `Velvet.linkProgram(progId)` | int | void | Link program |
| `Velvet.createMesh(vertices, indices?)` | Float32Array, Uint32Array? | int | Upload mesh, returns meshId |
| `Velvet.setUniformMatrix4fv(progId, name, matrix)` | int, string, Float32Array | void | Set 4x4 matrix uniform |
| `Velvet.clear(r, g, b, a)` | number×4 | void | Clear framebuffer |
| `Velvet.drawMesh(meshId, progId, rendId)` | int×3 | void | Render mesh |

## Blazor-Specific Considerations

### Canvas Reference
Always use `@ref` to get the canvas element reference and pass it to JavaScript after the component has rendered (`OnAfterRenderAsync`).

### JS Interop Performance
For high-frequency calls (like setting uniforms every frame), keep the logic in JavaScript. Only use `IJSRuntime` for setup.

### Error Handling
Wrap JS interop calls in try-catch blocks:

```csharp
try
{
    await JS.InvokeVoidAsync("Velvet.linkProgram", programId);
}
catch (JSException ex)
{
    Console.Error.WriteLine($"Shader linking failed: {ex.Message}");
}
```

## Differences from Static HTML Demo

| Aspect | Blazor Demo | Static HTML Demo |
|--------|-------------|------------------|
| Setup | C# component + JS interop | Pure JavaScript |
| Shader Loading | HTTP from C# or JS | `fetch()` in JS |
| Canvas | `@ref` ElementReference | `getElementById()` |
| Animation Loop | Must stay in JS | Can be in JS |
| Deployment | Requires .NET runtime (WASM) | Static files only |

## Troubleshooting

### Canvas not rendering
- Ensure `velvet.js` is loaded in `wwwroot/index.html`
- Check that `OnAfterRenderAsync` has `firstRender == true`
- Verify canvas element has a valid size

### Shader compilation errors
- Check browser console for detailed WebGL errors
- Ensure GLSL version is `#version 300 es`
- Verify attribute locations match mesh layout

### JS interop errors
- Ensure `window.Velvet` is defined (velvet.js loaded)
- Check parameter types match (int, string, arrays)
- Use browser DevTools to inspect JS objects

## Next Steps

- Add camera controls (orbit, zoom)
- Implement lighting and textures
- Load 3D models from files
- Add multiple meshes and scene graph
- Implement framebuffer effects (shadows, post-processing)
