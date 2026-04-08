# Velvet Current Architecture

## Scope

This document describes the architecture as it exists today in this workspace (8 April 2026), including:
- project boundaries
- dependency direction
- runtime flow in Blazor/WebAssembly
- rendering pipeline responsibilities

## Solution and Projects

The solution file currently includes three engine projects:
- `src/Velvet.Core`
- `src/Velvet.Graphics.WebGL`
- `src/Velvet.Hosting.Web`

The demo app `Velvet-Site` exists in the repository and references all three projects, but is not listed in `Velvet.sln`.

## Layered Architecture

### 1) Core Layer (`Velvet.Core`)

Purpose:
- engine domain model and render abstractions
- scene graph, math, geometry, animation, particles, rendering data
- no direct JavaScript runtime dependency

Key contracts:
- `Velvet.Core.Engine.IGraphicsDevice`
- `Velvet.Core.Engine.IRenderable`
- `Velvet.Core.Rendering.Shaders.IShader`

Key engine types:
- `Scene`, `SceneNode`, `MeshInstance`
- `RenderBatcher`, `RenderBatch`
- `Camera`, light data types
- animation and particle domain objects

### 2) Backend Layer (`Velvet.WebGL`)

Purpose:
- WebGL implementation details and .NET-to-JS bridge
- shader/program wrappers and GPU upload implementations

Key types:
- `IWebGLBridge` (transport surface to JS API)
- `BlazorWebGLBridge`, `StaticWebGLBridge`
- `WebGLGraphicsDevice`, `WebGLDevice`
- `WebGLMeshUploader`
- `ShaderProgram`, `WebGLShader`
- `JsBridge` global bridge registry

Frontend asset pipeline:
- TypeScript sources in `src/Velvet.WebGL/ts`
- bundled by webpack to `src/Velvet.WebGL/wwwroot/velvet.js`

### 3) Host Integration Layer (`Velvet.Blazor`)

Purpose:
- Blazor-first application runtime that orchestrates render loop, input, resize, and frame updates

Key types:
- `Velvet.Blazor.VelvetApp` (high-level app runtime used by pages)
- `VelvetBlazorExtensions` (simple setup helper)

This layer connects browser events and JS interop to engine state, then drives per-frame rendering.

### 4) Application Layer (`Velvet-Site`)

Purpose:
- pages/scenes that configure camera, lights, assets, and scene content
- no backend implementation details

Example:
- `Velvet-Site/Pages/Scene06.razor.cs` creates a `Velvet.Blazor.VelvetApp`, configures camera/lights/particles, and starts the frame loop.

## Dependency Direction

Current project references enforce this direction:

- `Velvet.Core` -> (no project references)
- `Velvet.WebGL` -> `Velvet.Core`
- `Velvet.Blazor` -> `Velvet.Core`, `Velvet.WebGL`
- `Velvet-Site` -> `Velvet.Core`, `Velvet.WebGL`, `Velvet.Blazor`

Design intent:
- inner engine abstractions live in Core
- WebGL backend depends on Core contracts
- Blazor host composes backend + engine behavior
- app/pages depend on host APIs

## Runtime Flows

## A) Minimal engine flow (`Velvet.Core.Engine.VelvetApp`)

1. Host configures graphics device via `UseGraphics(IGraphicsDevice)`.
2. Host registers `IRenderable` instances.
3. `RunAsync()` initializes device and invokes each renderable.

This is a lightweight baseline app loop.

## B) Blazor scene flow (`Velvet.Blazor.VelvetApp`)

1. `CreateAsync(canvas, js, programFactory)` creates `BlazorWebGLBridge`, initializes renderer, binds resize/input callbacks.
2. Host sets camera/lights/controller and adds scenes or particle systems.
3. `StartAsync(...)` builds render batches and starts a timed async loop.
4. Each frame:
- apply pending resize (through `ResizeController`)
- update input/controller and particle systems
- invoke user frame callback
- clear framebuffer
- render skybox
- set view/projection/light uniforms
- render mesh batches (with optional per-mesh callback)
- render particle systems
5. `StopAsync()` cancels loop and unbinds JS callback registrations.

## Rendering Responsibilities

Core decides:
- scene representation
- mesh instances and transforms
- batching strategy
- animation/particle domain behavior

WebGL decides:
- GPU resource creation and draw calls
- shader compilation/linking
- texture and uniform interop to JS WebGL runtime

Blazor host decides:
- frame loop timing and event plumbing
- when to apply resize
- orchestration of camera, lights, skybox, mesh draw sequence, particles

## Resize and Input Lifecycle

Current resize pattern:
- JS side reports size changes to `Velvet.Blazor.VelvetApp`.
- App queues resize through `ResizeController.RequestResize(...)`.
- Resize is applied at frame boundary in render loop.

Current input pattern:
- Orbit mouse/wheel events are bound via `CanvasHelpers.bindOrbitInput`.
- JS-invokable callbacks feed `OrbitInputBinder`.
- Binder updates camera each frame.

## Material Architecture Status

Material system is currently transitional:
- legacy standard material path exists
- shader-driven material path exists (`Velvet.Core.Rendering.Materials.Material` + `IShader`)

See `refactor/material.md` for migration details and rationale.

## Architectural Strengths

- Clear layering from domain to backend to host.
- Core remains mostly backend-agnostic.
- Blazor integration isolates browser-specific concerns.
- Frame-bound resize and async render loop avoid blocking patterns in WASM.

## Current Constraints and Tradeoffs

- Two app entry styles exist (`Velvet.Core.Engine.VelvetApp` and `Velvet.Blazor.VelvetApp`), which can create API overlap.
- Material migration is not yet fully unified.
- `JsBridge` global state simplifies setup but introduces shared mutable configuration.
- `Velvet-Site` not being in `Velvet.sln` can hide integration breakages unless built explicitly.

## Suggested Next Refactor Targets

1. Decide long-term ownership between core app runner and Blazor app runner (or formalize both roles).
2. Complete material unification and deprecate duplicate pathways.
3. Reduce global bridge state by preferring explicit bridge injection where practical.
4. Add architecture tests/checks around dependency direction and host/backend contracts.
5. Optionally include `Velvet-Site` in solution for full workspace build validation.
