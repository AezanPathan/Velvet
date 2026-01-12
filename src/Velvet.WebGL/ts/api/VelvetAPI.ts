import { WebGLContext } from "../webgl/WebGLContext";
import { GLRenderer } from "../webgl/GLRenderer";
import { GLShader } from "../webgl/GLShader";
import { GLProgram } from "../webgl/GLProgram";
import { GLBuffer } from "../webgl/GLBuffer";
import { GLMesh } from "../webgl/GLMesh";
import {
  ShaderManager,
  ProgramManager,
  BufferManager,
  MeshManager,
  RendererManager
} from "../core/resource/Managers";

/**
 * VelvetAPI
 * ----------
 * This file exposes the public-facing API of the Velvet Engine.
 *
 * Goals:
 *  - Provide a clean bridge between Blazor / JS / HTML and the engine backend.
 *  - Allow creation of shaders, programs, buffers, meshes through stable IDs.
 *  - Hide WebGL backend complexity from the user.
 *  - Enable future multi-backend support (WebGPU, Canvas2D).
 *
 * VelvetAPI does NOT render demos or scenes.
 * It only exposes engine primitives.
 */

let context: WebGLContext | null = null;

function ensureFloat32Array(data: unknown): Float32Array {
    if (data instanceof Float32Array) return data;
    // Blazor JS interop commonly materializes .NET float[] as a plain JS number[].
    if (Array.isArray(data)) return new Float32Array(data);
    // Last resort: try to treat it as array-like.
    return new Float32Array(data as any);
}

function ensureUint32Array(data: unknown): Uint32Array {
    if (data instanceof Uint32Array) return data;
    if (Array.isArray(data)) return new Uint32Array(data);
    return new Uint32Array(data as any);
}

/**
 * Initialize the Velvet engine with a canvas.
 * 
 * @param canvas - Either a canvas element ID (string) or an HTMLCanvasElement
 * @returns Renderer ID for use with draw calls
 * 
 * @example
 * // Using element ID
 * const rendererId = Velvet.init("myCanvas");
 * 
 * @example
 * // Using element reference (Blazor, React, etc.)
 * const canvas = document.getElementById("myCanvas");
 * const rendererId = Velvet.init(canvas);
 */
export function init(canvas: string | HTMLCanvasElement): number {
    let canvasElement: HTMLCanvasElement;

    // Resolve canvas element
    if (typeof canvas === "string") {
        // Canvas is an ID string - resolve it
        const element = document.getElementById(canvas);
        
        if (!element) {
            throw new Error(`Velvet.init: canvas '${canvas}' not found`);
        }
        
        if (!(element instanceof HTMLCanvasElement)) {
            throw new Error(`Velvet.init: element '${canvas}' is not a canvas (found ${element.tagName})`);
        }
        
        canvasElement = element;
    } else {
        // Canvas is already an HTMLCanvasElement
        if (!(canvas instanceof HTMLCanvasElement)) {
            throw new Error("Velvet.init: provided element is not an HTMLCanvasElement");
        }
        
        canvasElement = canvas;
    }

    // Initialize WebGL context with the resolved canvas element
    context = new WebGLContext(canvasElement);

    const renderer = new GLRenderer(context.gl, RendererManager.generateId());
    return RendererManager.add(renderer);
}

/**
 * Creates and compiles a shader.
 */
export function createShader(source: string, type: "vertex" | "fragment"): number {
    if (!context) throw new Error("Velvet not initialized");

    const gl = context.gl;
    const shader = new GLShader(gl, ShaderManager.generateId());
    shader.compile(source, type);

    return ShaderManager.add(shader);
}

/**
 * Creates a GPU program (shaders must be attached by Velvet internally).
 */
export function createProgram(): number {
    if (!context) throw new Error("Velvet not initialized");

    const gl = context.gl;
    const program = new GLProgram(gl, ProgramManager.generateId());

    return ProgramManager.add(program);
}

/**
 * Attach a shader to a program.
 */
export function attachShader(programId: number, shaderId: number): void {
    const program = ProgramManager.get(programId);
    const shader = ShaderManager.get(shaderId);
    program.attachShader(shader);
}

/**
 * Link an existing program by ID.
 */
export function linkProgram(programId: number): void {
    const program = ProgramManager.get(programId);
    program.link();
}

/**
 * Set a 4x4 matrix uniform on a program.
 */
export function setUniformMatrix4fv(programId: number, name: string, matrix: Float32Array): void {
    if (!context) throw new Error("Velvet not initialized");
    
    const program = ProgramManager.get(programId) as any;
    const location = program.getUniformLocation(name);
    
    if (location) {
        program.use();
        context.gl.uniformMatrix4fv(location, false, ensureFloat32Array(matrix));
    }
}

export function setUniformMatrix3fv(programId: number, name: string, matrix: Float32Array): void {
    if (!context) throw new Error("Velvet not initialized");

    const program = ProgramManager.get(programId) as any;
    const location = program.getUniformLocation(name);

    if (location) {
        program.use();
        context.gl.uniformMatrix3fv(location, false, ensureFloat32Array(matrix));
    }
}

export function setUniform3f(programId: number, name: string, x: number, y: number, z: number): void {
    if (!context) throw new Error("Velvet not initialized");

    const program = ProgramManager.get(programId) as any;
    const location = program.getUniformLocation(name);

    if (location) {
        program.use();
        context.gl.uniform3f(location, x, y, z);
    }
}

export function setUniform1f(programId: number, name: string, value: number): void {
    if (!context) throw new Error("Velvet not initialized");

    const program = ProgramManager.get(programId) as any;
    const location = program.getUniformLocation(name);

    if (location) {
        program.use();
        context.gl.uniform1f(location, value);
    }
}

/**
 * Create a mesh from raw vertex/index data.
 * 
 * For non-indexed geometry, pass only vertices.
 * The mesh's attribute layout and count must be set before drawing.
 */
export function createMesh(
    vertices: Float32Array,
    indices?: Uint32Array
): number {
    if (!context) throw new Error("Velvet not initialized");

    const gl = context.gl;

    const vertexData = ensureFloat32Array(vertices);
    const vb = new GLBuffer(gl, BufferManager.generateId(), gl.ARRAY_BUFFER);
    vb.setData(vertexData);
    BufferManager.add(vb);

    let ib: GLBuffer | undefined;
    let count = 0;
    
    if (indices && indices.length > 0) {
        const indexData = ensureUint32Array(indices);
        ib = new GLBuffer(gl, BufferManager.generateId(), gl.ELEMENT_ARRAY_BUFFER);
        ib.setData(indexData);
        BufferManager.add(ib);
        count = indexData.length;
    } else {
        // Infer stride: either position(3)+color(3) = 6 floats per vertex, or
        // position+color+normal = 9 floats per vertex. Prefer 9 if divisible.
        if (vertexData.length % 9 === 0) {
            count = vertexData.length / 9;
        } else {
            count = vertexData.length / 6;
        }
    }

    const mesh = new GLMesh(gl, MeshManager.generateId(), vb, ib) as any;
    
    // Set default attributes for position (location 0) and color (location 1)
    // Configure attributes depending on inferred stride
    if (vertexData.length % 9 === 0) {
        // position(3) + color(3) + normal(3) -> stride 36 bytes
        mesh.setAttributes([
            { location: 0, size: 3, type: gl.FLOAT, stride: 36, offset: 0 },   // aPosition
            { location: 1, size: 3, type: gl.FLOAT, stride: 36, offset: 12 },  // aColor
            { location: 2, size: 3, type: gl.FLOAT, stride: 36, offset: 24 }   // aNormal
        ]);
    } else {
        mesh.setAttributes([
            { location: 0, size: 3, type: gl.FLOAT, stride: 24, offset: 0 },  // aPosition (vec3)
            { location: 1, size: 3, type: gl.FLOAT, stride: 24, offset: 12 }  // aColor (vec3)
        ]);
    }
    mesh.setCount(count);

    return MeshManager.add(mesh);
}

/**
 * Draw a mesh using a specific program and renderer.
 */
export function drawMesh(meshId: number, programId: number, rendererId: number): void {
    const mesh = MeshManager.get(meshId);
    const program = ProgramManager.get(programId);
    const renderer = RendererManager.get(rendererId);

    renderer.drawMesh(mesh, program);
}

/**
 * Clear screen
 */
export function clear(rendererId: number, r: number, g: number, b: number, a: number): void {
    const renderer = RendererManager.get(rendererId);
    renderer.clear(r, g, b, a);
}

/**
 * Resize viewport
 */
export function resize(width: number, height: number): void {
    if (!context) throw new Error("Velvet not initialized");
    context.resize(width, height);
}
