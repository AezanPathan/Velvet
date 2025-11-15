(function () {
  const state = {
    gl: null,
    program: null,
    buffer: null,
    initialized: false,
  };

  function createShader(gl, type, source) {
    const shader = gl.createShader(type);
    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
      const info = gl.getShaderInfoLog(shader);
      gl.deleteShader(shader);
      throw new Error(`Velvet shader compile error: ${info}`);
    }
    return shader;
  }

  function createProgram(gl, vertexSource, fragmentSource) {
    const vertexShader = createShader(gl, gl.VERTEX_SHADER, vertexSource);
    const fragmentShader = createShader(gl, gl.FRAGMENT_SHADER, fragmentSource);
    const program = gl.createProgram();
    gl.attachShader(program, vertexShader);
    gl.attachShader(program, fragmentShader);
    gl.linkProgram(program);

    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
      const info = gl.getProgramInfoLog(program);
      gl.deleteProgram(program);
      gl.deleteShader(vertexShader);
      gl.deleteShader(fragmentShader);
      throw new Error(`Velvet program link error: ${info}`);
    }

    gl.deleteShader(vertexShader);
    gl.deleteShader(fragmentShader);
    return program;
  }

  function ensureInitialized() {
    if (!state.initialized) {
      throw new Error("Velvet not initialized. Call Velvet.init(canvasId) first.");
    }
  }

  window.Velvet = {
    ensureCanvas(canvasId) {
      const existing = document.getElementById(canvasId);
      if (existing) return;

      const canvas = document.createElement('canvas');
      canvas.id = canvasId;
      canvas.width = 640;
      canvas.height = 480;
      canvas.style.border = '1px solid #444';
      canvas.style.maxWidth = '100%';
      canvas.style.height = 'auto';
      document.body.appendChild(canvas);
    },

    init(canvasId) {
      if (state.initialized) {
        return;
      }

      const canvas = document.getElementById(canvasId);
      if (!canvas) {
        throw new Error(`Velvet could not find canvas with id '${canvasId}'.`);
      }

      const gl = canvas.getContext("webgl");
      if (!gl) {
        throw new Error("Velvet could not acquire a WebGL context.");
      }

      const vertexSource = `
        attribute vec2 position;
        void main() {
          gl_Position = vec4(position, 0.0, 1.0);
        }
      `;

      const fragmentSource = `
        precision mediump float;
        void main() {
          gl_FragColor = vec4(1.0, 0.3, 0.6, 1.0);
        }
      `;

      const program = createProgram(gl, vertexSource, fragmentSource);
      const buffer = gl.createBuffer();

      state.gl = gl;
      state.program = program;
      state.buffer = buffer;
      state.initialized = true;
    },

    drawTriangle() {
      ensureInitialized();
      const { gl, program, buffer } = state;
      gl.viewport(0, 0, gl.canvas.width, gl.canvas.height);
      gl.clearColor(0.1, 0.1, 0.1, 1.0);
      gl.clear(gl.COLOR_BUFFER_BIT);

      gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
      const vertices = new Float32Array([
        0.0, 0.8,
        -0.8, -0.8,
        0.8, -0.8,
      ]);
      gl.bufferData(gl.ARRAY_BUFFER, vertices, gl.STATIC_DRAW);

      gl.useProgram(program);
      const positionLocation = gl.getAttribLocation(program, "position");
      gl.enableVertexAttribArray(positionLocation);
      gl.vertexAttribPointer(positionLocation, 2, gl.FLOAT, false, 0, 0);

      gl.drawArrays(gl.TRIANGLES, 0, 3);
    },
  };
})();
