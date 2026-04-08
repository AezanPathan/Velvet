# Material System Refactor Strategy

## Current State (Dual System)

Velvet currently maintains **two material systems** side-by-side to support legacy rendering while enabling a new shader-driven architecture. Both are intentionally preserved during this transition phase.

---

## System 1: Standard Material (Legacy)



```csharp
{
    public float AmbientStrength { get; set; }
- **Applies To**: `MaterialDemo.razor` (three Suzanne models), GLTF loader default materials
 **Stable**: No pending changes; locked for backward compatibility
 **Use Case**: Standard lit rendering with diffuse + ambient fallback
 **Applies To**: GLTF loader (during migration), backward compatibility layer
 **Status**: ⏳ Being phased out; `MaterialDemo` migrated to new system as of Phase 2

- Cannot express custom shader uniforms
- Difficult to extend for effects (PBR, parallax mapping, etc.)

**Namespace:** `Velvet.Core.Rendering.Materials`
    private readonly IShader _shader;
 **Extensible**: Accepts any uniform type (float, Vector3, Matrix4)
 **Use Case**: Custom effects, material prototyping, dynamic uniforms
 **Applies To**: `CustomMaterialDemo.razor` (✅ live), `MaterialDemo.razor` (✅ migrated in Phase 2)
```

### Characteristics
---
- **Shader-aware**: Takes an `IShader` and stores arbitrary properties
## Phase 2: Material Demo Migrated ✅
- **Active**: Applies itself by calling `shader.Use()` and setting uniforms
### Key Changes
Applied new Material system to [Velvet-Site/Pages/MaterialDemo.razor.cs](Velvet-Site/Pages/MaterialDemo.razor.cs):

1. **Created three new Materials**: matte red, standard cyan, bright yellow
   ```csharp
   shader = new WebGLShader(app.Program);
   matteMaterial = new Material(shader);
   matteMaterial.Set("uBaseColor", new Vector3(1.0f, 0.42f, 0.42f));
   matteMaterial.Set("uAmbientStrength", 0.03f);
   ```

2. **Mapped meshes to materials** via Dictionary<Mesh, Material>:
   ```csharp
   foreach (var mesh in GetAllMeshes(suzanneNode))
   {
     meshMaterialMap[mesh] = newMaterials[i];
   }
   ```

3. **Applied materials in beforeDrawMesh callback**:
   ```csharp
   await app.StartAsync(
     onFrame: OnFrameAsync,
     beforeDrawMesh: BeforeDrawMesh);
   
   private async Task BeforeDrawMesh(Mesh mesh)
   {
     if (meshMaterialMap.TryGetValue(mesh, out var material))
     {
       material.Apply();
       await shader.FlushAsync();
     }
   }
   ```

### Result
- ✅ Three material variations render correctly with new system
- ✅ Color and ambient strength controlled via uniforms
- ✅ Async uniform writes handled safely via WebGLShader.FlushAsync()
- ✅ Demo layer contains zero rendering backend logic

### Lessons Learned
- **Mesh-to-Material mapping** works well for per-instance material variety
- **beforeDrawMesh callback** is the right integration point for custom material application
- **Aliasing technique** (`using NewMaterial = ...`) helps avoid type ambiguity during transition

---
- **Extensible**: Accepts any uniform type (float, Vector3, Matrix4)
## GLTF Loader Migration Strategy (Phase 3+)
- **Use Case**: Custom effects, material prototyping, dynamic uniforms
### Current State
- GLTF loader creates old Materials in [src/Velvet.Core/Assets/Gltf/GltfLoader.cs](src/Velvet.Core/Assets/Gltf/GltfLoader.cs#L967)
- Problem: GltfLoader is in Core; WebGL shaders are in WebGL layer
- Cannot directly instantiate WebGLShader from Core without circular dependency

### Planned Approach
1. **Option A** (Recommended for Phase 3):
   - Keep GltfLoader creating old Materials
   - Add optional shader factory parameter: `GltfLoader.LoadScene(bytes, shaderFactory)`
   - If shader available, wrap old Material in new Material
   - Engine layer handles conversion

2. **Option B** (Alternative):
   - Create Material adapter in Velvet.Graphics.WebGL layer
   - `public static Material CreateFromOldMaterial(ShaderProgram program, Velvet.Core.Rendering.Material oldMaterial)`
   - VelvetApp converts old Materials to new on-the-fly during rendering

3. **Option C** (Future):
   - Promote shader abstraction to Core.Rendering.Shaders
   - Allow GltfLoader to take IShader parameter
   - Full end-to-end integration in Core layer

### Why Not Now
- Adding shader factory parameter to LoadScene is a breaking API change
- GLTF models currently render fine with old Material → SetMaterialAsync → uniform mapping
- Focus on stabilizing custom material pipeline first
- Phase backward compatibility: old Materials still work while new system matures

---
- **Applies To**: `CustomMaterialDemo.razor`

### Advantages
- Backend-agnostic: Depends only on `IShader` interface

### Limitations
- No built-in texture support (design choice—kept minimal)
- ✅ Materials Demo migrated to new Material system
- ✅ Multiple per-instance materials working correctly
- ⏳ Prepare for GLTF loader migration
- No material inheritance or composition patterns
- Requires manual property setting upstream

### Phase 3 (Proposed)
---
- Migrate GLTF loader to create new Materials with optional shader factory
- Auto-conversion layer for old → new Materials

### Phase 3 (Proposed)

## Backend Implementation: WebGLShader

**Location:** `src/Velvet.Graphics.WebGL/Shaders/WebGLShader.cs`

**Namespace:** `Velvet.Graphics.WebGL.Shaders`

### Why It Exists
- Implements the `IShader` interface for the new Material system
- Converts Material API calls into WebGL uniform operations
- Wraps async ShaderProgram calls with a **queuing pattern** to avoid WASM blocking

### Key Pattern: Async Queueing
```csharp
// Synchronous API (IShader contract)
public void SetFloat(string name, float value)
    => _pendingUniformWrites.Add(_program.SetUniform1fAsync(name, value));

// Asynchronous flush (called from render loop)
public async Task FlushAsync()
    => await Task.WhenAll(_pendingUniformWrites.ToArray());
```

**Why This Works:**
1. Material.Apply() calls SetFloat/SetVector3/SetMatrix4 (synchronous)
2. WebGLShader enqueues async uniform operations
3. Render loop calls WebGLShader.FlushAsync() via `beforeDrawMesh` callback
4. All uniforms are written asynchronously without blocking

This pattern **solves the WASM "Cannot wait on monitors" error** by moving async work into the event loop instead of blocking on it.

---

## Mesh Rendering Choice

**In `VelvetApp.RunLoopAsync()` render pipeline:**

Currently, meshes are rendered with the **new Material system** only. The render loop invokes:
```csharp
beforeDrawMesh = mesh =>
{
    customMaterial.Apply();
    return webglShader.FlushAsync();
};
```

**For legacy materials**, the renderer reads properties directly (no Material.Apply call).

---

## Migration Path (Future)

### Phase 1 (Current)
- ✅ New Material system implemented and validated in `CustomMaterialDemo`
- ✅ WebGLShader uses safe async queueing pattern
- ✅ Standard Material preserved for backward compatibility
- ⏳ No breaking changes to existing scenes

### Phase 2 (Planned)
- Convert `MaterialDemo` to optionally use new Material system (wrapped in Standard properties)
- Add Material factory helpers to reduce boilerplate
- Document uniform naming conventions

### Phase 3 (Proposed)
- **Merge both systems** into a unified Material class that wraps a shader

### Deprecation

---

## Design Constraints (Intentional)

### What NOT to Add
- ❌ **Texture support** in Material: Keep shader responsibility
- ❌ **Material inheritance/composition**: Use shader composition instead
- ❌ **Shader graphs**: Future separate system
- ❌ **Async Apply()**: Breaks synchronous interface contract
- ❌ **Auto-caching**: WebGLShader handles queuing

### Why These Constraints?
- **Minimalism**: Material is a property bag + uniform applier
- **Single responsibility**: Shaders handle effects; Material hands uniforms
- **Interface stability**: IShader stays synchronous for all implementations
- **Async at the boundary**: Queueing happens at WebGL integration layer

---

## Demo Usage Reference

### Standard Material (MaterialDemo.razor)
```csharp
var material = new Velvet.Core.Rendering.Material(
    albedoColor: new Vector3(1, 0, 0),
    ambientStrength: 0.05f,
    diffuseStrength: 1.0f
);
mesh.Material = material;
```

### New Material (CustomMaterialDemo.razor)
```csharp
var shader = new WebGLShader(app.Program);
var material = new Velvet.Core.Rendering.Materials.Material(shader);
material.Set("uBaseColor", new Vector3(0.6f, 0.3f, 0.9f));
material.Set("uAmbientStrength", 0.25f);

// In render loop
material.Apply();
await shader.FlushAsync();
```

---

## File Structure After Refactor

```
src/
  Velvet.Core/
    Rendering/
      Material.cs                     ← Standard (legacy)
      Shaders/
        IShader.cs
      Materials/
        Material.cs                   ← New (shader-driven)
  Velvet.Graphics.WebGL/
    Shaders/
      WebGLShader.cs                  ← IShader implementation
Velvet-Site/
  Pages/
    MaterialDemo.razor                ← Uses standard Material
    CustomMaterialDemo.razor          ← Uses new Material + WebGLShader
refactor/
  material.md                         ← This document
```

---

## Validation Checklist

- ✅ WebGLShader uses safe async queueing (no GetResult blocking)
- ✅ Demo layer contains no backend logic
- ✅ IShader interface remains synchronous
- ✅ Material system is minimal (no advanced features)
- ✅ Both Material classes coexist without conflict
- ✅ WASM runtime accepts async at render loop boundary only
- ✅ Build succeeds with no regressions

---

## Questions & Decisions

**Q: Why keep Standard Material after switching to new Material?**
A: Provides clear upgrade path. Existing code (GLTF loader, legacy scenes) continues working while new scenes adopt new system.

**Q: Why not make Material.Apply() async?**
A: Breaks the synchronous contract of IShader. Async belongs at the call site (render loop), not in Material.

**Q: Can I use both materials on the same mesh?**
A: No. A mesh references one material. Choose the system that fits your use case.

**Q: What about performance?**
A: New Material has marginal overhead (foreach over properties dictionary). For performance-critical paths, consider batching or shader variants.
