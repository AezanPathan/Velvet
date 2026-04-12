# Velvet.Graphics.WebGL Migration Notes (Wave 1)

## Summary

Wave 1 refactored WebGL backend internals across C# and TypeScript while keeping the public integration surface stable.

## C# refactor changes

1. **Bridge duplication removed**
   - Added `JsRuntimeWebGLBridgeBase` for shared `IJSRuntime` forwarding logic.
   - `BlazorWebGLBridge` and `StaticWebGLBridge` now only implement host-specific initialization behavior.
   - `IWebGLBridge` signatures are unchanged.

2. **ShaderProgram responsibility split**
   - Added `ShaderProgramMaterialBinder` to isolate material + texture binding/cache behavior.
   - `ShaderProgram` remains the public façade used by existing callers.

## TypeScript refactor changes

1. **API decomposition**
   - `ts/api/VelvetAPI.ts` is now an orchestrator.
   - Extracted modules:
     - `ts/api/runtime.ts`
     - `ts/api/uniforms.ts`
     - `ts/api/meshes.ts`
     - `ts/api/textures.ts`
     - `ts/api/rendererState.ts`
   - `ts/index.ts` global `Velvet.*` API remains stable.

2. **Lifecycle hardening**
   - Added `clearAllManagers()` in resource managers.
   - `runtime.ts` now resets managers on context loss/restoration callbacks.
   - `WebGLContext` now exposes `onContextLost` / `onContextRestored` handler registration.

3. **Diagnostics cleanup**
   - Removed noisy console/debug output from API hot paths.
   - Removed large commented-out legacy block from `GLRenderer.ts`.

## Consumer impact

- Existing host code and `IWebGLBridge` consumers continue to work.
- No required migration for existing runtime entry points (`Velvet.init`, `Velvet.createMesh`, etc.).
