import { TextureManager } from "../core/resource/Managers";
import { getContext, getRequiredUniformLocation, withOptionalUniformLocation } from "./runtime";

export async function loadTexture(imageUrl: string): Promise<WebGLTexture | null> {
  const context = getContext();

  try {
    const response = await fetch(imageUrl);
    if (!response.ok) {
      console.error(`loadTexture: HTTP ${response.status} when loading '${imageUrl}'`);
      return null;
    }

    const blob = await response.blob();
    const imageBitmap = await createImageBitmap(blob);

    const gl = context.gl;
    const texture = gl.createTexture();
    if (!texture) {
      console.error("loadTexture: gl.createTexture failed");
      return null;
    }

    gl.bindTexture(gl.TEXTURE_2D, texture);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, imageBitmap.width, imageBitmap.height, 0, gl.RGBA, gl.UNSIGNED_BYTE, imageBitmap);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.REPEAT);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
    gl.generateMipmap(gl.TEXTURE_2D);
    gl.bindTexture(gl.TEXTURE_2D, null);
    return texture;
  } catch (error) {
    console.error(`loadTexture: failed to load '${imageUrl}'`, error);
    return null;
  }
}

export function createTextureFromUrl(url: string): Promise<number> {
  return new Promise((resolve, reject) => {
    const context = getContext();
    const gl = context.gl;
    const texture = gl.createTexture();
    if (!texture) {
      reject(new Error("createTextureFromUrl: gl.createTexture failed"));
      return;
    }

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
        resolve(TextureManager.add(texture));
      } catch (e) {
        reject(e);
      }
    };
    img.onerror = () => reject(new Error(`createTextureFromUrl: failed to load image ${url}`));
    img.src = url;
  });
}

export function bindTextureById(programId: number, samplerName: string, textureId: number, textureUnit: number): void {
  const { gl, location, program } = getRequiredUniformLocation(programId, samplerName, "bindTextureById");
  const texture = TextureManager.get(textureId);

  program.use();
  gl.activeTexture(gl.TEXTURE0 + textureUnit);
  gl.bindTexture(gl.TEXTURE_2D, texture);
  gl.uniform1i(location, textureUnit);
}

export function createCubemapTexture(faceUrls: string[]): Promise<number> {
  return new Promise((resolve, reject) => {
    const context = getContext();
    if (faceUrls.length !== 6) {
      reject(new Error("createCubemapTexture: exactly 6 face URLs required"));
      return;
    }

    const gl = context.gl;
    const texture = gl.createTexture();
    if (!texture) {
      reject(new Error("createCubemapTexture: gl.createTexture failed"));
      return;
    }

    gl.bindTexture(gl.TEXTURE_CUBE_MAP, texture);
    gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_R, gl.CLAMP_TO_EDGE);

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
            resolve(TextureManager.add(texture));
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

export function bindCubemapTextureById(programId: number, samplerName: string, textureId: number, textureUnit: number): void {
  const { gl, location, program } = getRequiredUniformLocation(programId, samplerName, "bindCubemapTextureById");
  const texture = TextureManager.get(textureId);

  program.use();
  gl.activeTexture(gl.TEXTURE0 + textureUnit);
  gl.bindTexture(gl.TEXTURE_CUBE_MAP, texture);
  gl.uniform1i(location, textureUnit);
}

export function bindTexture(texture: WebGLTexture, textureUnit: number, programId: number, samplerName: string): void {
  withOptionalUniformLocation(programId, samplerName, (gl, location) => {
    gl.activeTexture(gl.TEXTURE0 + textureUnit);
    gl.bindTexture(gl.TEXTURE_2D, texture);
    gl.uniform1i(location, textureUnit);
  });
}
