# Velvet.Core API Map (Refactor Wave 1)

This map records the compatibility-first API direction applied in the first refactor wave.

## Canonical APIs (use going forward)

1. `Velvet.Core.Rendering.Materials.ShaderMaterial`
   - Canonical shader-driven material abstraction.
   - Replaces direct use of `Velvet.Core.Rendering.Materials.Material`.

2. `Velvet.Core.Rendering.IRenderProgram`
   - Canonical backend program marker used by batching contracts.
   - Implemented by `Velvet.Graphics.WebGL.ShaderProgram`.

3. `Velvet.Core.Math.Matrix4`
   - Canonical matrix wrapper for API-facing matrix operations.
   - Added factory/utility methods: `Identity`, `Multiply`, `Trs`, `LookAt`, `Perspective`, `NormalMatrix`.

4. `Velvet.Core.Assets.Gltf.GltfLoader` (public entry points unchanged)
   - Internally delegated material extraction to `GltfMaterialReader` to reduce loader responsibility.

## Compatibility APIs retained (deprecated or transitional)

1. `Velvet.Core.Rendering.Materials.Material`
   - **Status:** `[Obsolete]` compatibility alias.
   - **Migration:** replace with `ShaderMaterial`.

2. `RenderBatcher.BuildBatches(..., object shaderProgram)`
   - **Status:** `[Obsolete]` compatibility overload.
   - **Migration:** use `BuildBatches(..., IRenderProgram renderProgram)`.

3. `BatchKey(object shaderProgram, Material material, VertexLayout vertexLayout)`
   - **Status:** `[Obsolete]` compatibility constructor.
   - **Migration:** use `BatchKey(IRenderProgram renderProgram, Material material, VertexLayout vertexLayout)`.

4. `BatchKey.ShaderProgram`
   - **Status:** `[Obsolete]` property alias.
   - **Migration:** use `BatchKey.RenderProgram`.

## Structural cleanup decisions in this wave

1. Removed runtime-shipped documentation sample:
   - `src/Velvet.Core/Rendering/BatchRenderingExample.cs` deleted.
   - Moved reference sample to `refactor/examples/BatchRenderingExample.cs`.

2. Kept public `GltfLoader` signatures stable:
   - `LoadScene`, `LoadSceneWithAnimations`, `LoadMeshes` unchanged.

3. Preserved legacy `Velvet.Core.Rendering.Material` data model:
   - Existing mesh material path remains stable in this wave.
