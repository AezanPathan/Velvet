# Velvet Core Refactor TODO

## Goals
- Make `Velvet.Core` boundaries cleaner and more stable.
- Remove transitional APIs and reduce duplication.
- Improve maintainability and release confidence for `v0.1+`.

## 1) Core API Cleanup (High)
- [ ] Audit public APIs in `Velvet.Core` and mark unstable/internal-only types.
- [ ] Minimize public surface where possible (prefer internal/private for implementation details).
- [ ] Standardize naming across rendering and scene APIs (consistent nouns/verbs).
- [ ] Add XML docs on key public contracts (`IGraphicsDevice`, `IRenderable`, shader/material interfaces).

## 2) Material Migration Completion (High)
- [ ] Remove or isolate obsolete compatibility material aliases in `Velvet.Core.Rendering.Materials`.
- [ ] Ensure all demos/examples use the new material/shader path.
- [ ] Add a clear migration note for users upgrading from the old material API.

## 3) Host Responsibility Boundaries (High)
- [ ] Decide and document responsibility split between core host loop and Blazor host loop.
- [ ] Keep `Velvet.Core` free from browser/JS assumptions.
- [ ] Extract any orchestration logic from core that belongs in hosting/backend layers.

## 4) Rendering Pipeline Structure (Medium)
- [ ] Review `RenderBatcher` and related render data flow for clearer staging and ownership.
- [ ] Reduce coupling between scene objects and draw submission details.
- [ ] Ensure lighting data contracts are backend-agnostic and easy to extend.

## 5) Math and Data Types (Medium)
- [ ] Review `Matrix`, `Matrix4`, `Vector3`, `Vector4`, `Quaternion` for overlap and consistency.
- [ ] Normalize coordinate/handedness and transform conventions in docs/comments.
- [ ] Add guard checks for invalid values where silent failures are possible.

## 6) Animation and Particles (Medium)
- [ ] Separate runtime evaluation logic from storage/data classes where mixed.
- [ ] Verify interpolation behavior and edge cases (empty clips, single keyframe, out-of-range time).
- [ ] Keep particle update contracts independent from renderer-specific details.

## 7) Error Handling and Diagnostics (Medium)
- [ ] Replace ambiguous exceptions with precise messages in core subsystems.
- [ ] Add lightweight debug hooks/events for pipeline visibility (without backend dependencies).
- [ ] Document expected invalid states and lifecycle assumptions.

## 8) Validation and Safety Net (High)
- [ ] Add smoke tests for core scene setup, batching, animation stepping, and math correctness.
- [ ] Add architecture checks to enforce dependency direction (`Core` must not depend on backend/host concerns).
- [ ] Include `Velvet-Site` build in regular validation to catch integration regressions.

## 9) Refactor Execution Plan
- [ ] Create small PR-sized batches (API cleanup, materials, batching, tests) instead of one large rewrite.
- [ ] Track breaking changes in a `CHANGELOG` section for `v0.1` -> next version.
- [ ] After each batch: build solution + run tests + validate at least one sample scene.

## Definition of Done
- [ ] `Velvet.Core` has clear boundaries and no transitional API confusion.
- [ ] Material API path is unified and examples match current guidance.
- [ ] Core has baseline tests and architecture checks.
- [ ] Docs reflect the refactored API and expected usage.
