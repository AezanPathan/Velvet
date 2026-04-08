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
    RendererManager,
    TextureManager
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
 * Set a uniform sampler (texture unit) on a program.
 */
export function setUniform1i(programId: number, name: string, value: number): void {
    if (!context) throw new Error("Velvet not initialized");

    const program = ProgramManager.get(programId) as any;
    const location = program.getUniformLocation(name);

    if (location) {
        program.use();
        context.gl.uniform1i(location, value);
    }
}

/**
 * Set a uniform boolean on a program.
 */
export function setUniform1b(programId: number, name: string, value: boolean): void {
    if (!context) throw new Error("Velvet not initialized");

    const program = ProgramManager.get(programId) as any;
    const location = program.getUniformLocation(name);

    if (location) {
        program.use();
        context.gl.uniform1i(location, value ? 1 : 0);
        console.log(`[DEBUG] setUniform1b: ${name} = ${value}`);
        if (name === "uHasTexture") {
            console.warn(`*** CRITICAL: setUniform1b("uHasTexture", ${value}) SET ON GPU ***`);
        }
    } else {
        console.warn(`[DEBUG] setUniform1b: uniform "${name}" not found in program ${programId}`);
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
    indices?: Uint32Array,
    vertexStrideFloats?: number
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
    }

    const mesh = new GLMesh(gl, MeshManager.generateId(), vb, ib) as any;
    
    // Prefer explicit stride provided by .NET geometry layout to avoid ambiguous modulo detection.
    // Fallback to legacy heuristic when stride is not provided.
    const effectiveStride = vertexStrideFloats && vertexStrideFloats > 0 ? vertexStrideFloats : 0;

    if (effectiveStride === 16 || (effectiveStride === 0 && vertexData.length % 16 === 0)) {
        // SKINNED layout: position(3) + normal(3) + uv(2) + joints(4) + weights(4) = 16 floats (64 bytes)
        count = indices ? count : vertexData.length / 16;
        mesh.setAttributes([
            { location: 0, size: 3, type: gl.FLOAT, stride: 64, offset: 0 },              // aPosition
            { location: 1, size: 3, type: gl.FLOAT, stride: 64, offset: 12 },             // aNormal
            { location: 2, size: 2, type: gl.FLOAT, stride: 64, offset: 24 },             // aUV
            { location: 3, size: 4, type: gl.FLOAT, stride: 64, offset: 32 },             // aJoints (4 floats containing byte values)
            { location: 4, size: 4, type: gl.FLOAT, stride: 64, offset: 48 }              // aWeights
        ]);
    } else if (effectiveStride === 11 || (effectiveStride === 0 && vertexData.length % 11 === 0)) {
        // Backward compatibility: position(3) + color(3) + normal(3) + uv(2) -> stride 44 bytes
        count = indices ? count : vertexData.length / 11;
        mesh.setAttributes([
            { location: 0, size: 3, type: gl.FLOAT, stride: 44, offset: 0 },   // aPosition
            { location: 1, size: 3, type: gl.FLOAT, stride: 44, offset: 12 },  // aColor
            { location: 2, size: 3, type: gl.FLOAT, stride: 44, offset: 24 },  // aNormal
            { location: 3, size: 2, type: gl.FLOAT, stride: 44, offset: 36 }   // aUV
        ]);
    } else if (effectiveStride === 9 || (effectiveStride === 0 && vertexData.length % 9 === 0)) {
        // position(3) + color(3) + normal(3) -> stride 36 bytes
        count = indices ? count : vertexData.length / 9;
        mesh.setAttributes([
            { location: 0, size: 3, type: gl.FLOAT, stride: 36, offset: 0 },   // aPosition
            { location: 1, size: 3, type: gl.FLOAT, stride: 36, offset: 12 },  // aColor
            { location: 2, size: 3, type: gl.FLOAT, stride: 36, offset: 24 }   // aNormal
        ]);
    } else if (effectiveStride === 8 || (effectiveStride === 0 && vertexData.length % 8 === 0)) {
        // Standard layout: position(3) + normal(3) + uv(2) = 8 floats (32 bytes)
        count = indices ? count : vertexData.length / 8;
        mesh.setAttributes([
            { location: 0, size: 3, type: gl.FLOAT, stride: 32, offset: 0 },    // aPosition
            { location: 1, size: 3, type: gl.FLOAT, stride: 32, offset: 12 },   // aNormal
            { location: 2, size: 2, type: gl.FLOAT, stride: 32, offset: 24 }    // aUV
        ]);
    } else if (effectiveStride === 6 || (effectiveStride === 0 && vertexData.length % 6 === 0)) {
        // Simple position + color: position(3) + color(3) -> stride 24 bytes
        count = indices ? count : vertexData.length / 6;
        mesh.setAttributes([
            { location: 0, size: 3, type: gl.FLOAT, stride: 24, offset: 0 },  // aPosition (vec3)
            { location: 1, size: 3, type: gl.FLOAT, stride: 24, offset: 12 }  // aColor (vec3)
        ]);
    } else if (effectiveStride === 3 || (effectiveStride === 0 && vertexData.length % 3 === 0)) {
        // Position-only layout (for skybox): position(3) -> stride 12 bytes
        count = indices ? count : vertexData.length / 3;
        mesh.setAttributes([
            { location: 0, size: 3, type: gl.FLOAT, stride: 12, offset: 0 }   // aPosition (vec3)
        ]);
    } else {
        throw new Error(`Unsupported vertex layout: ${vertexData.length} floats (expected stride 3, 6, 8, 9, 11, or 16)`);
    }
    mesh.setCount(count);

    return MeshManager.add(mesh);
}

/**
 * Create a particle mesh for point rendering.
 * Allocates a fixed-size vertex buffer for the given capacity.
 */
export function createParticleMesh(capacity: number): number {
    if (!context) throw new Error("Velvet not initialized");
    if (capacity <= 0) throw new Error("createParticleMesh: capacity must be > 0");

    const gl = context.gl;

    const vertexData = new Float32Array(capacity * 8);
    const vb = new GLBuffer(gl, BufferManager.generateId(), gl.ARRAY_BUFFER);
    vb.setData(vertexData, gl.DYNAMIC_DRAW);
    BufferManager.add(vb);

    const mesh = new GLMesh(gl, MeshManager.generateId(), vb) as any;
    mesh.setAttributes([
        { location: 0, size: 3, type: gl.FLOAT, stride: 32, offset: 0 },   // aPosition
        { location: 1, size: 1, type: gl.FLOAT, stride: 32, offset: 12 },  // aSize
        { location: 2, size: 4, type: gl.FLOAT, stride: 32, offset: 16 }   // aColor
    ]);
    mesh.setPrimitiveType(gl.POINTS);
    mesh.setCount(0);

    return MeshManager.add(mesh);
}

/**
 * Update vertex data for an existing mesh (used by particles).
 */
export function updateMeshVertices(meshId: number, vertices: Float32Array, vertexCount: number): void {
    const mesh = MeshManager.get(meshId) as any;
    const vertexData = ensureFloat32Array(vertices);
    const count = Math.max(0, vertexCount | 0);

    if (count === 0) {
        mesh.setCount(0);
        return;
    }

    const required = count * 8;
    const data = vertexData.length > required ? vertexData.subarray(0, required) : vertexData;
    mesh.updateVertexData(data, count);
}

/**
 * Load a texture from a URL and create a WebGL texture object.
 * Returns a WebGLTexture that can be bound to a texture unit.
 * 
 * @param imageUrl - URL to the image (can be data: URL or relative path)
 * @returns WebGLTexture object (or null if loading fails)
 */
export async function loadTexture(imageUrl: string): Promise<WebGLTexture | null> {
    if (!context) throw new Error("Velvet not initialized");

    try {
        // Fetch the image
        const response = await fetch(imageUrl);
        if (!response.ok) {
            console.error(`Failed to load texture: ${imageUrl} (${response.statusText})`);
            return null;
        }

        // Convert to bitmap
        const blob = await response.blob();
        const imageBitmap = await createImageBitmap(blob);

        // Create WebGL texture
        const gl = context.gl;
        const texture = gl.createTexture();
        if (!texture) {
            console.error("Failed to create WebGL texture");
            return null;
        }

        // Bind and upload
        gl.bindTexture(gl.TEXTURE_2D, texture);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, imageBitmap.width, imageBitmap.height, 0, gl.RGBA, gl.UNSIGNED_BYTE, imageBitmap);

        // Set texture parameters
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.REPEAT);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);

        // Generate mipmaps
        gl.generateMipmap(gl.TEXTURE_2D);

        // Unbind
        gl.bindTexture(gl.TEXTURE_2D, null);

        return texture;
    } catch (error) {
        console.error(`Error loading texture from ${imageUrl}:`, error);
        return null;
    }
}

/**
 * Create a WebGLTexture from a URL using HTMLImageElement.
 * Returns a texture ID managed by TextureManager.
 */
export function     createTextureFromUrl(url: string): Promise<number> {
    return new Promise((resolve, reject) => {
        if (!context) {
            reject(new Error("Velvet not initialized"));
            return;
        }

        const gl = context.gl;
        const texture = gl.createTexture();
        if (!texture) {
            reject(new Error("createTextureFromUrl: gl.createTexture failed"));
            return;
        }

        // Initialize with 1x1 pixel so the texture is valid before the image loads
        gl.bindTexture(gl.TEXTURE_2D, texture);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0, gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([255, 255, 255, 255]));
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.REPEAT);

        const img = new Image();
        img.crossOrigin = "anonymous";
        img.onload = () => {
            try {
                gl.bindTexture(gl.TEXTURE_2D, texture);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
                gl.generateMipmap(gl.TEXTURE_2D);
                gl.bindTexture(gl.TEXTURE_2D, null);
                const textureId = TextureManager.add(texture);
                console.log(`[Velvet] Texture loaded successfully: ${url}, id=${textureId}`);
                resolve(textureId);
            } catch (e) {
                reject(e);
            }
        };
        img.onerror = () => {
            reject(new Error(`createTextureFromUrl: failed to load image ${url}`));
        };
        img.src = url;
    });
}

/**
 * Bind a texture by ID to a sampler uniform.
 */
export function bindTextureById(programId: number, samplerName: string, textureId: number, textureUnit: number): void {
    if (!context) throw new Error("Velvet not initialized");

    const gl = context.gl;
    const texture = TextureManager.get(textureId);
    const program = ProgramManager.get(programId) as any;
    const location = program.getUniformLocation(samplerName);
    if (!location) throw new Error(`bindTextureById: uniform ${samplerName} not found`);

    console.log(`[DEBUG] bindTextureById: programId=${programId}, sampler=${samplerName}, textureId=${textureId}, unit=${textureUnit}`);
    
    program.use();
    gl.activeTexture(gl.TEXTURE0 + textureUnit);
    gl.bindTexture(gl.TEXTURE_2D, texture);
    gl.uniform1i(location, textureUnit);
    
    console.log(`[DEBUG] bindTextureById: COMPLETE - texture bound to unit ${textureUnit}`);
}

/**
 * Create a cubemap texture from 6 face URLs.
 * Face order: +X, -X, +Y, -Y, +Z, -Z
 */
export function createCubemapTexture(faceUrls: string[]): Promise<number> {
    return new Promise((resolve, reject) => {
        if (!context) return reject(new Error("Velvet not initialized"));
        if (faceUrls.length !== 6) {
            return reject(new Error("createCubemapTexture: exactly 6 face URLs required"));
        }

        const gl = context.gl;
        const texture = gl.createTexture();
        if (!texture) {
            return reject(new Error("createCubemapTexture: gl.createTexture failed"));
        }

        gl.bindTexture(gl.TEXTURE_CUBE_MAP, texture);

        // Set texture parameters
        gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_R, gl.CLAMP_TO_EDGE);

        // Face targets in order: +X, -X, +Y, -Y, +Z, -Z
        const faceTargets = [
            gl.TEXTURE_CUBE_MAP_POSITIVE_X,
            gl.TEXTURE_CUBE_MAP_NEGATIVE_X,
            gl.TEXTURE_CUBE_MAP_POSITIVE_Y,
            gl.TEXTURE_CUBE_MAP_NEGATIVE_Y,
            gl.TEXTURE_CUBE_MAP_POSITIVE_Z,
            gl.TEXTURE_CUBE_MAP_NEGATIVE_Z
        ];

        let loadedCount = 0;
        let hasError = false;

        for (let i = 0; i < 6; i++) {
            const img = new Image();
            img.crossOrigin = "anonymous";
            
            img.onload = () => {
                if (hasError) return;

                try {
                    gl.bindTexture(gl.TEXTURE_CUBE_MAP, texture);
                    gl.texImage2D(faceTargets[i], 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
                    
                    loadedCount++;
                    if (loadedCount === 6) {
                        gl.bindTexture(gl.TEXTURE_CUBE_MAP, null);
                        const textureId = TextureManager.add(texture);
                        console.log(`[Velvet] Cubemap texture loaded successfully, id=${textureId}`);
                        resolve(textureId);
                    }
                } catch (e) {
                    hasError = true;
                    reject(e);
                }
            };

            img.onerror = () => {
                if (!hasError) {
                    hasError = true;
                    reject(new Error(`createCubemapTexture: failed to load face ${i}: ${faceUrls[i]}`));
                }
            };

            img.src = faceUrls[i];
        }
    });
}

/**
 * Bind a cubemap texture by ID to a sampler uniform.
 */
export function bindCubemapTextureById(programId: number, samplerName: string, textureId: number, textureUnit: number): void {
    if (!context) throw new Error("Velvet not initialized");

    const gl = context.gl;
    const texture = TextureManager.get(textureId);
    const program = ProgramManager.get(programId) as any;
    const location = program.getUniformLocation(samplerName);
    if (!location) throw new Error(`bindCubemapTextureById: uniform ${samplerName} not found`);

    program.use();
    gl.activeTexture(gl.TEXTURE0 + textureUnit);
    gl.bindTexture(gl.TEXTURE_CUBE_MAP, texture);
    gl.uniform1i(location, textureUnit);
}

/**
 * Bind a texture to a texture unit and set the sampler uniform.
 * 
 * @param texture - WebGLTexture object from loadTexture()
 * @param textureUnit - Texture unit (0-31, typically 0)
 * @param programId - Program ID to set the sampler uniform on
 * @param samplerName - Name of the sampler uniform (e.g., "uBaseColor")
 */
export function bindTexture(texture: WebGLTexture, textureUnit: number, programId: number, samplerName: string): void {
    if (!context) throw new Error("Velvet not initialized");

    const gl = context.gl;

    // Activate texture unit
    gl.activeTexture(gl.TEXTURE0 + textureUnit);

    // Bind texture to the active unit
    gl.bindTexture(gl.TEXTURE_2D, texture);

    // Set sampler uniform
    const program = ProgramManager.get(programId) as any;
    program.use();
    const location = program.getUniformLocation(samplerName);
    if (location) {
        gl.uniform1i(location, textureUnit);
    }
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
 * Set blend mode on a renderer.
 */
export function setBlendMode(rendererId: number, mode: "off" | "alpha" | "additive"): void {
    const renderer = RendererManager.get(rendererId) as any;
    renderer.setBlendMode(mode);
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

/**
 * Enable or disable depth buffer writes.
 * When disabled, fragments are still depth-tested but don't update the depth buffer.
 * Useful for rendering skyboxes and other background elements.
 */
export function setDepthMask(rendererId: number, enabled: boolean): void {
    const renderer = RendererManager.get(rendererId);
    renderer.setDepthMask(enabled);
}
