# Velvet.Core Refactor Todo (Compatibility-First)

## Goal

Broad cleanup of `src/Velvet.Core` (naming, folders, API redesign) without adding features, while keeping existing consumers working through migration shims and deprecations.

## Current execution status (wave 1)

- Done: API map + migration aliases, material API consolidation, matrix API normalization baseline, typed batching contracts, GLTF material parsing extraction, example relocation, validation build.
- Remaining for later waves: deeper `GltfLoader` decomposition (animation/accessor split), broader namespace pass beyond touched surfaces, dedicated regression test project.

## Todo list

1. Define target API map
- Inventory current public API.
- Mark each symbol as keep/rename/move/deprecate.
- Prepare migration aliases and deprecation messages.

2. Material system consolidation
- Design one canonical core material API.
- Bridge legacy and new material paths behind compatibility adapters.
- Remove ambiguous duplicate naming across namespaces/folders.

3. Matrix/math contract cleanup
- Decide canonical matrix representation and boundary conversions.
- Refactor call sites to use one primary path.
- Keep temporary compatibility helpers for existing code.

4. Rendering contract typing improvements
- Replace `object` shader program usage in batching with typed contracts.
- Clarify backend-agnostic vs backend-specific responsibilities.
- Align `Rendering/*` structure and naming.

5. GLTF loader decomposition
- Split `GltfLoader` into focused internal components.
- Keep current public entry points as wrappers.
- Isolate material, mesh, skin, and animation parsing responsibilities.

6. Remove example code from core runtime assembly
- Move `BatchRenderingExample` out of `Velvet.Core`.
- Keep docs/examples updated in refactor/docs area or sample project.

7. Namespace and folder normalization pass
- Ensure namespace-to-path consistency.
- Remove transitional naming drift.
- Update dependent projects accordingly.

8. Validation + migration docs
- Build and smoke-check dependent projects after each phase.
- Add/refine tests where practical for refactored boundaries.
- Write migration notes for deprecated/renamed APIs.
