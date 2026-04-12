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
- `Velvet.Core.Graphics.IGraphicsDevice`
- `Velvet.Core.Rendering.IRenderable`
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

### 3) Host Integration Layer (`Velvet.Hosting.Web`)

Purpose:
- Blazor-first application runtime that orchestrates render loop, input, resize, and frame updates

Key types:
- `Velvet.Hosting.Web.VelvetHost` (high-level app runtime used by pages)
- `ServiceExtensions` (simple setup helper)

This layer connects browser events and JS interop to engine state, then drives per-frame rendering.

### 4) Application Layer (`Velvet-Site`)

Purpose:
- pages/scenes that configure camera, lights, assets, and scene content
- no backend implementation details

Example:
- `Velvet-Site/Pages/Scene06.razor.cs` creates a `Velvet.Hosting.Web.VelvetHost`, configures camera/lights/particles, and starts the frame loop.

## Dependency Direction

Current project references enforce this direction:

- `Velvet.Core` -> (no project references)
- `Velvet.WebGL` -> `Velvet.Core`
- `Velvet.Hosting.Web` -> `Velvet.Core`, `Velvet.WebGL`
- `Velvet-Site` -> `Velvet.Core`, `Velvet.WebGL`, `Velvet.Hosting.Web`

Design intent:
- inner engine abstractions live in Core
- WebGL backend depends on Core contracts
- Blazor host composes backend + engine behavior
- app/pages depend on host APIs

## Runtime Flows

## A) Minimal engine flow (`Velvet.Core.VelvetHost`)

1. Host configures graphics device via `UseGraphics(IGraphicsDevice)`.
2. Host registers `IRenderable` instances.
3. `RunAsync()` initializes device and invokes each renderable.

This is a lightweight baseline app loop.

## B) Blazor scene flow (`Velvet.Hosting.Web.VelvetHost`)

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
- JS side reports size changes to `Velvet.Hosting.Web.VelvetHost`.
- App queues resize through `ResizeController.RequestResize(...)`.
- Resize is applied at frame boundary in render loop.

Current input pattern:
- Orbit mouse/wheel events are bound via `CanvasHelpers.bindOrbitInput`.
- JS-invokable callbacks feed `OrbitInputBinder`.
- Binder updates camera each frame.

## Material Architecture Status

Material system is currently transitional:
- legacy standard material path exists
- shader-driven material path exists (`Velvet.Core.Rendering.Materials.ShaderMaterial` + `IShader`, with obsolete alias for `Rendering.Materials.Material`)

See `refactor/material.md` for migration details and rationale.

## Architectural Strengths

- Clear layering from domain to backend to host.
- Core remains mostly backend-agnostic.
- Blazor integration isolates browser-specific concerns.
- Frame-bound resize and async render loop avoid blocking patterns in WASM.

## Current Constraints and Tradeoffs

- Two app entry styles exist (`Velvet.Core.VelvetHost` and `Velvet.Hosting.Web.VelvetHost`), which can create API overlap.
- Material migration is not yet fully unified.
- `JsBridge` global state simplifies setup but introduces shared mutable configuration.
- `Velvet-Site` not being in `Velvet.sln` can hide integration breakages unless built explicitly.

## Suggested Next Refactor Targets

1. Decide long-term ownership between core app runner and Blazor app runner (or formalize both roles).
2. Complete material unification and deprecate duplicate pathways.
3. Reduce global bridge state by preferring explicit bridge injection where practical.
4. Add architecture tests/checks around dependency direction and host/backend contracts.
5. Optionally include `Velvet-Site` in solution for full workspace build validation.

## Folder Hierarchy (Short)

```text
Velvet/
|- README.md                       # Project overview
|- Velvet.sln                      # Solution file (engine projects)
|- refactor/
|  |- architecture.md             # Architecture notes
|  |- material.md                 # Material-system migration notes
|- src/
|  |- Velvet.Core/                # Engine core
|  |  |- Velvet.Core.csproj       # Core project definition
|  |  |- Animation/               # Clips, samplers, animator
|  |  |  |- AnimationClip.cs
|  |  |  |- Animator.cs
|  |  |- Engine/                  # App loop + core contracts
|  |  |  |- VelvetHost.cs
|  |  |  |- IGraphicsDevice.cs
|  |  |  |- IRenderable.cs
|  |  |  |- Scene.cs
|  |  |- Geometry/                # Built-in geometry and layouts
|  |  |  |- CubeGeometry.cs
|  |  |  |- SphereGeometry.cs
|  |  |- Math/                    # Matrix/vector/quaternion types
|  |  |  |- Matrix4.cs
|  |  |  |- Vector3.cs
|  |  |- Particles/               # Particle system domain
|  |  |  |- ParticleSystem.cs
|  |  |- Rendering/               # Camera, mesh, batching, materials
|  |     |- Camera.cs
|  |     |- Mesh.cs
|  |     |- RenderBatcher.cs
|  |     |- Materials/
|  |     |- Shaders/
|  |
|  |- Velvet.Graphics.WebGL/      # WebGL backend + JS bridge
|  |  |- Velvet.Graphics.WebGL.csproj
|  |  |- package.json             # TS/Webpack toolchain
|  |  |- tsconfig.json
|  |  |- webpack.config.js
|  |  |- bridges/                 # C# to JS bridge + WebGL device impl
|  |  |  |- IWebGLBridge.cs
|  |  |  |- BlazorWebGLBridge.cs
|  |  |  |- WebGLGraphicsDevice.cs
|  |  |- Shaders/
|  |  |  |- WebGLShader.cs
|  |  |- ts/                      # TypeScript runtime source
|  |  |  |- index.ts
|  |  |  |- api/
|  |  |  |- core/
|  |  |  |- webgl/
|  |  |- wwwroot/                 # Built browser assets
|  |     |- velvet.js
|  |     |- velvet-debug-ui.js
|  |
|  |- Velvet.Hosting.Web/         # Hosting integration layer
|     |- Velvet.Hosting.Web.csproj
|     |- VelvetHost.cs
|     |- ServiceExtensions.cs
|     |- Assets/
|        |- Gltf/
|- Velvet-Site/                   # Blazor demo app
|  |- Velvet-Site.csproj
|  |- Program.cs
|  |- App.razor
|  |- _Imports.razor
|  |- Layout/
|  |  |- MainLayout.razor
|  |  |- NavMenu.razor
|  |- Pages/                      # Demo pages and scene code-behind
|  |  |- Home.razor
|  |  |- MaterialDemo.razor
|  |  |- MaterialDemo.razor.cs
|  |  |- Scene04.razor
|  |  |- Scene05.razor
|  |  |- Scene06.razor
|  |- wwwroot/
|     |- index.html
|     |- css/
|     |- js/
|     |- models/
|     |- skybox/
```

### Main Folder Info

- `src/Velvet.Core`: backend-agnostic engine logic and contracts.
- `src/Velvet.Graphics.WebGL`: concrete GPU/WebGL implementation and browser JS interop.
- `src/Velvet.Hosting.Web`: host glue code that wires runtime and rendering for web.
- `Velvet-Site`: app layer with demo pages (`SceneXX.razor`) and scene setup code.
- `refactor`: living design docs for ongoing architecture/material changes.
