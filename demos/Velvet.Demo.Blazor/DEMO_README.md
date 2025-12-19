# Velvet WebGL Cube Demo - Complete Implementation Guide

## 🎯 Overview

This is a **complete Blazor demo** that renders a rotating 3D cube using the **NEW VelvetAPI**. 

### ⚠️ Important: OLD API is REMOVED

This demo does **NOT** use:
- ❌ `Velvet.ensureCanvas()`
- ❌ `Velvet.drawTriangle()`
- ❌ `Velvet.drawCube()`
- ❌ `Velvet.init("canvasId")` (string ID)

### ✅ NEW VelvetAPI Used

This demo uses **ONLY** these modern API methods:

```javascript
Velvet.init(canvasElement)                          // Initialize with DOM element
Velvet.createShader(source, type)                   // Compile GLSL shader
Velvet.createProgram()                              // Create GPU program
Velvet.attachShader(programId, shaderId)            // Attach shader to program
Velvet.linkProgram(programId)                       // Link program
Velvet.createMesh(vertices)                         // Upload mesh data
Velvet.setUniformMatrix4fv(programId, name, matrix) // Set matrix uniforms
Velvet.clear(r, g, b, a)                            // Clear screen
Velvet.drawMesh(meshId, programId, rendererId)      // Render mesh
```

## 📁 File Structure

```
demos/Velvet.Demo.Blazor/
├── Pages/
│   ├── Index.razor              # Landing page with link to demo
│   ├── Index.razor.cs            # (Minimal - old VelvetApp deprecated)
│   ├── CubeDemo.razor            # Main demo UI with canvas
│   └── CubeDemo.razor.cs         # C# logic using BlazorWebGLBridge
├── wwwroot/
│   ├── index.html                # Script loading order
│   ├── velvet.js                 # Velvet WebGL engine bundle
│   ├── cube-demo.js              # Animation loop + matrix math
│   └── shaders/
│       ├── simple.vert           # GLSL 300 es vertex shader
│       └── simple.frag           # GLSL 300 es fragment shader
```

## 🔧 Implementation Details

### 1. Blazor Component (CubeDemo.razor.cs)

**Key Points:**
- Uses `ElementReference canvasRef` for canvas
- Creates `BlazorWebGLBridge` instance
- Calls `bridge.InitWithElementAsync(canvasRef)` to get `rendererId`
- Uses direct `IJSRuntime` calls to VelvetAPI for all operations

**Flow:**
```csharp
1. OnAfterRenderAsync(firstRender) → Create BlazorWebGLBridge
2. InitializeDemo() →
   a. rendererId = await bridge.InitWithElementAsync(canvasRef)
   b. Load shader sources via loadShaderSource()
   c. vertexShaderId = await JS.InvokeAsync("Velvet.createShader", ...)
   d. fragmentShaderId = await JS.InvokeAsync("Velvet.createShader", ...)
   e. programId = await JS.InvokeAsync("Velvet.createProgram")
   f. await JS.InvokeVoidAsync("Velvet.attachShader", ...)
   g. await JS.InvokeVoidAsync("Velvet.linkProgram", programId)
   h. meshId = await JS.InvokeAsync("Velvet.createMesh", vertices)
3. StartAnimation() →
   await JS.InvokeVoidAsync("startCubeAnimation", rendererId, programId, meshId, ...)
```

### 2. JavaScript Animation (cube-demo.js)

**Responsibilities:**
- Matrix math utilities (Mat4.perspective, lookAt, rotateX/Y/Z, multiply)
- Shader loading from URLs
- Animation loop with `requestAnimationFrame`
- Calls VelvetAPI methods: `setUniformMatrix4fv`, `clear`, `drawMesh`

**Animation Loop:**
```javascript
function animate() {
    // Update rotations
    rotationX += 0.01; rotationY += 0.02; rotationZ += 0.005;
    
    // Build model matrix
    modelMatrix = Mat4.multiply(rotateX, rotateY, rotateZ);
    
    // Set uniform
    window.Velvet.setUniformMatrix4fv(programId, 'uModelMatrix', modelMatrix);
    
    // Render
    window.Velvet.clear(0.1, 0.1, 0.1, 1.0);
    window.Velvet.drawMesh(meshId, programId, rendererId);
    
    requestAnimationFrame(animate);
}
```

### 3. GLSL Shaders (GLSL 300 es)

**Vertex Shader (simple.vert):**
```glsl
#version 300 es
layout(location = 0) in vec3 aPosition;  // Explicit location required
layout(location = 1) in vec3 aColor;

uniform mat4 uModelMatrix;
uniform mat4 uViewMatrix;
uniform mat4 uProjectionMatrix;

out vec3 vColor;

void main() {
    vColor = aColor;
    gl_Position = uProjectionMatrix * uViewMatrix * uModelMatrix * vec4(aPosition, 1.0);
}
```

**Fragment Shader (simple.frag):**
```glsl
#version 300 es
precision highp float;

in vec3 vColor;
out vec4 fragColor;  // Required in GLSL 300 es

void main() {
    fragColor = vec4(vColor, 1.0);
}
```

### 4. Cube Geometry

**Format:** Interleaved vertices `[x, y, z, r, g, b, ...]`
- 6 faces × 2 triangles × 3 vertices = 36 vertices
- 36 vertices × 6 floats = 216 floats total

**Face Colors:**
- Front: Red (1, 0, 0)
- Back: Green (0, 1, 0)
- Top: Blue (0, 0, 1)
- Bottom: Yellow (1, 1, 0)
- Right: Magenta (1, 0, 1)
- Left: Cyan (0, 1, 1)

## 🚀 Running the Demo

### Step 1: Build
```bash
cd C:\Users\DELL\Velvet
dotnet build
```

### Step 2: Run
```bash
cd demos
dotnet run --project Velvet.Demo.Blazor
```

### Step 3: Open Browser
Navigate to: **https://localhost:49892/cubedemo**

### Step 4: Use Demo
1. Click **"Initialize Demo"** button
2. Wait for shaders to compile and mesh to upload
3. Click **"Start Animation"** to see rotating cube
4. Click **"Stop Animation"** to pause

## 🔍 Debugging

### Console Output
The demo logs detailed progress:
```
✅ Renderer initialized: ID = 0
✅ Shaders loaded: 450 chars total
✅ Shaders compiled: Vertex=0, Fragment=1
✅ Program created: ID = 0
✅ Shaders attached to program
✅ Program linked successfully
✅ Mesh created: ID = 0, vertices = 36 (216 floats)
🎉 Demo initialization successful!
🎬 Animation started!
```

### Common Issues

**Issue:** velvet.js not found (404)
**Fix:** Copy velvet.js to Blazor wwwroot:
```powershell
Copy-Item "src\Velvet.WebGL\wwwroot\velvet.js" -Destination "demos\Velvet.Demo.Blazor\wwwroot\velvet.js"
```

**Issue:** Shaders not found
**Fix:** Ensure shaders exist in `demos\Velvet.Demo.Blazor\wwwroot\shaders\`

**Issue:** Black screen / no cube
**Fix:** Check browser console for WebGL errors. Ensure:
- Canvas element is properly referenced
- Shaders compile without errors
- Matrices are set before draw call

## 📊 Resource IDs

The VelvetAPI uses integer IDs to track resources:

| Resource       | ID  | Created By              |
|----------------|-----|-------------------------|
| Renderer       | 0   | `Velvet.init()`         |
| Vertex Shader  | 0   | `Velvet.createShader()` |
| Fragment Shader| 1   | `Velvet.createShader()` |
| Program        | 0   | `Velvet.createProgram()`|
| Mesh           | 0   | `Velvet.createMesh()`   |

## 🎨 Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  Blazor C# (CubeDemo.razor.cs)                              │
│  - ElementReference canvasRef                               │
│  - BlazorWebGLBridge (IJSRuntime wrapper)                   │
│  - Calls: InitWithElementAsync(canvasRef)                   │
└────────────────┬────────────────────────────────────────────┘
                 │ IJSRuntime.InvokeAsync
                 ↓
┌─────────────────────────────────────────────────────────────┐
│  JavaScript (cube-demo.js + velvet.js)                      │
│  - window.Velvet.init(canvasElement) → rendererId           │
│  - window.Velvet.createShader/Program/Mesh                  │
│  - window.Velvet.setUniformMatrix4fv                        │
│  - window.Velvet.drawMesh                                   │
└────────────────┬────────────────────────────────────────────┘
                 │ WebGL2 API calls
                 ↓
┌─────────────────────────────────────────────────────────────┐
│  WebGL2 Context                                             │
│  - Compiled shaders (GLSL 300 es)                           │
│  - Linked programs                                          │
│  - Vertex buffers with interleaved data                     │
│  - Matrix uniforms (Model, View, Projection)                │
└─────────────────────────────────────────────────────────────┘
```

## ✨ Features Demonstrated

- ✅ **Blazor + WebGL2 Integration** via ElementReference
- ✅ **Modern Shader Pipeline** with GLSL 300 es
- ✅ **Resource Management** with ID-based tracking
- ✅ **Matrix Math** (Perspective projection, View matrix, Model rotations)
- ✅ **Animation Loop** using requestAnimationFrame
- ✅ **Interleaved Vertex Data** [position, color]
- ✅ **Multiple Uniform Updates** per frame
- ✅ **Error Handling** with try-catch and user feedback

## 🎓 Learning Points

1. **No string IDs:** Canvas is passed as DOM ElementReference, not "canvasId"
2. **Explicit locations:** GLSL 300 es requires `layout(location = N)` for attributes
3. **Resource IDs:** All GPU resources tracked by integer IDs returned from create methods
4. **Async workflow:** Blazor → JS interop is async/await
5. **Frame loop in JS:** Animation runs in JavaScript, not C# (performance)

---

**Made with ❤️ using Velvet Graphics Engine**
