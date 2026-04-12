# Velvet.Graphics.WebGL API Map (Refactor Wave 1)

## Scope

This map covers both:
- C# backend surface in `src/Velvet.Graphics.WebGL/*.cs`
- TypeScript bridge/runtime surface in `src/Velvet.Graphics.WebGL/ts/*`

## Canonical APIs (target)

1. **Bridge contract**
   - `IWebGLBridge` remains the canonical C# host-interop contract.
   - Host-specific init differences (`InitWithElementAsync`, `InitWithIdAsync`) stay explicit.

2. **Program abstraction**
   - `ShaderProgram` remains the public program façade used by host/runtime code.
   - Internals should be decomposed (uniforms/material/texture operations) without changing call sites.

3. **Shader uniform adapter**
   - `WebGLShader` remains canonical `IShader` implementation for shader-driven material pipeline.

4. **TS public entry points**
   - Public API exported through `ts/index.ts` remains stable (`Velvet.*` runtime contract).
   - Internals move out of monolithic `VelvetAPI.ts` into focused modules.

## Compatibility surface to preserve

1. **`JsBridge` global registry**
   - Keep available for existing setup flows.
   - Treat as compatibility convenience, not required architecture path.

2. **`WebGLDevice` / `WebGLGraphicsDevice` constructors**
   - Keep current constructor signatures and behavior.

3. **Current JS API function names**
   - Keep exported names (`init`, `createShader`, `createProgram`, `drawMesh`, etc.) stable.

## Refactor decisions for this wave

1. Keep public signatures stable and refactor behind wrappers/adapters.
2. Reduce bridge duplication first, then split `ShaderProgram` internals.
3. Decompose TS API by concern while preserving `Velvet.*` compatibility.
4. Gate diagnostics/log spam and keep only actionable error reporting.
