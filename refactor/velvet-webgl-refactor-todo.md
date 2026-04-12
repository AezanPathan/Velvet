# Velvet.Graphics.WebGL Refactor Todo (Compatibility-First)

## Goal

Refactor `src/Velvet.Graphics.WebGL` for cleaner architecture and maintainability across C# bridge code and TypeScript runtime, without adding new features and without breaking existing host integrations.

## Current execution status (wave 1)

- Done: API map, bridge consolidation, shader/material responsibility split, TS API decomposition, lifecycle hardening, diagnostics cleanup, folder/module normalization, migration docs.
- Remaining for later waves: deeper typed-shader bridge cleanup (`GLProgram.attachShader` raw-handle casting), optional resource disposal API expansion for explicit host-driven teardown.

## Todo list

1. Define public API map
- Inventory C# and TS public surfaces.
- Mark keep/rename/move/deprecate decisions.
- Add migration aliases where needed.

2. Consolidate bridge implementations
- Extract shared `IJSRuntime` bridge operations from Blazor/Static bridges.
- Keep host-specific initialization paths separate.
- Preserve `IWebGLBridge` compatibility.

3. Split ShaderProgram responsibilities
- Isolate texture/material binding from core program operations.
- Keep existing `ShaderProgram` call sites working through wrappers.
- Preserve skinned/particle behavior.

4. Decompose TypeScript API surface
- Split `ts/api/VelvetAPI.ts` into focused modules.
- Keep stable exports in `ts/index.ts`.
- Reduce untyped casts and tighten contracts.

5. Improve resource lifecycle management
- Define clear ownership/cleanup for renderer/program/mesh/texture resources.
- Add explicit cleanup hooks where missing.
- Add context-loss recovery scaffolding.

6. Clean up diagnostics
- Gate debug logging behind explicit debug flags.
- Remove noisy hot-path console logs.
- Keep actionable error reporting.

7. Normalize folder/namespace structure
- Align C# bridge foldering to responsibilities.
- Align TS module layout to API boundaries.

8. Validate and document migration
- Build .NET and TS outputs after each phase.
- Publish migration notes for deprecated surfaces.
- Update refactor docs with final status.
