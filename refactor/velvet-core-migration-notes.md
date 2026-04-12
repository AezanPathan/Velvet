# Velvet.Core Migration Notes (Wave 1)

## Summary

Wave 1 introduced compatibility-first refactors in `Velvet.Core` with deprecation shims to avoid breaking consumers.

## API migrations

1. Shader-driven material type
   - Old: `Velvet.Core.Rendering.Materials.Material`
   - New: `Velvet.Core.Rendering.Materials.ShaderMaterial`
   - Compatibility: old type remains as `[Obsolete]` alias.

2. Batching render program contract
   - Old: `RenderBatcher.BuildBatches(..., object shaderProgram)`
   - New: `RenderBatcher.BuildBatches(..., IRenderProgram renderProgram)`
   - Compatibility: object-based overloads remain as `[Obsolete]`.

3. Batch key program access
   - Old: `BatchKey.ShaderProgram`
   - New: `BatchKey.RenderProgram`
   - Compatibility: old property remains as `[Obsolete]` alias.

4. Matrix API direction
   - Direction: prefer `Matrix4` wrappers (`Multiply`, `Trs`, `LookAt`, `Perspective`) for API-facing usage.
   - Compatibility: existing `Matrix` utilities remain available.

## Structural changes

1. `GltfLoader` material extraction moved to `GltfMaterialReader`.
2. `BatchRenderingExample` removed from runtime assembly and relocated to `refactor/examples/BatchRenderingExample.cs`.

## Consumer impact

- Existing code continues to compile.
- Consumers using deprecated APIs receive warnings and should migrate to canonical APIs listed above.
