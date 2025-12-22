// Optional developer-only debug UI for Velvet.
// - Lives fully in JS
// - Uses Tweakpane if present
// - Mutates runtime state only via explicit callbacks/hooks (no engine logic here)
// - Safe to omit: if this file or Tweakpane isn't loaded, Velvet continues to run

(function () {
  "use strict";

  function hasTweakpane() {
    return typeof window !== "undefined" && window.Tweakpane && window.Tweakpane.Pane;
  }

  function clamp01(x) {
    if (typeof x !== "number" || Number.isNaN(x)) return 0;
    return Math.min(1, Math.max(0, x));
  }

  function floatToHexChannel01(v01) {
    var v = Math.round(clamp01(v01) * 255);
    var hex = v.toString(16);
    return hex.length === 1 ? "0" + hex : hex;
  }

  function rgb01ToHex(rgb) {
    return (
      "#" +
      floatToHexChannel01(rgb.r) +
      floatToHexChannel01(rgb.g) +
      floatToHexChannel01(rgb.b)
    );
  }

  function hexToRgb01(hex) {
    if (typeof hex !== "string") return { r: 1, g: 1, b: 1 };
    var s = hex.trim();
    if (s[0] === "#") s = s.slice(1);
    if (s.length === 3) {
      s = s[0] + s[0] + s[1] + s[1] + s[2] + s[2];
    }
    if (s.length !== 6) return { r: 1, g: 1, b: 1 };

    var r = parseInt(s.slice(0, 2), 16);
    var g = parseInt(s.slice(2, 4), 16);
    var b = parseInt(s.slice(4, 6), 16);

    if (Number.isNaN(r) || Number.isNaN(g) || Number.isNaN(b)) return { r: 1, g: 1, b: 1 };

    return { r: r / 255, g: g / 255, b: b / 255 };
  }

  function radToDeg(rad) {
    return (rad * 180) / Math.PI;
  }

  function degToRad(deg) {
    return (deg * Math.PI) / 180;
  }

  async function invoke(dotnet, methodName /*, ...args */) {
    if (!dotnet) throw new Error("Missing dotnet reference");
    if (!methodName) throw new Error("Missing method name");

    var args = Array.prototype.slice.call(arguments, 2);

    // Blazor JS interop DotNetObjectReference exposes invokeMethodAsync.
    if (typeof dotnet.invokeMethodAsync === "function") {
      return await dotnet.invokeMethodAsync(methodName, ...args);
    }

    // Fallback: support a plain JS object with functions (static HTML hosts can do this).
    if (typeof dotnet[methodName] === "function") {
      return await dotnet[methodName](...args);
    }

    throw new Error("dotnet reference does not support method: " + methodName);
  }

  function safeNumber(x, fallback) {
    return typeof x === "number" && !Number.isNaN(x) ? x : fallback;
  }

  function vec3FromDto(dto, fallback) {
    var fb = fallback || { x: 0, y: 0, z: 0 };
    if (!dto) return { x: fb.x, y: fb.y, z: fb.z };
    return {
      x: safeNumber(dto.x, fb.x),
      y: safeNumber(dto.y, fb.y),
      z: safeNumber(dto.z, fb.z),
    };
  }

  function colorFromDto(dto, fallback) {
    var fb = fallback || { r: 1, g: 1, b: 1 };
    if (!dto) return { r: fb.r, g: fb.g, b: fb.b };
    return {
      r: safeNumber(dto.r, fb.r),
      g: safeNumber(dto.g, fb.g),
      b: safeNumber(dto.b, fb.b),
    };
  }

  function ensureContainer(container) {
    if (container && container.appendChild) return container;

    var el = document.createElement("div");
    el.id = "velvet-debug-ui";
    el.style.position = "fixed";
    el.style.top = "12px";
    el.style.right = "12px";
    el.style.zIndex = "99999";
    document.body.appendChild(el);
    return el;
  }

  async function init(config) {
    if (!hasTweakpane()) {
      console.warn("[VelvetDebugUI] Tweakpane not found. Skipping debug UI init.");
      return null;
    }

    if (!config || typeof config !== "object") {
      throw new Error("VelvetDebugUI.init requires a config object");
    }

    var camera = config.camera;
    var directionalLight = config.directionalLight;
    var pointLight = config.pointLight;
    var renderer = config.renderer;

    var container = ensureContainer(config.container);

    // Avoid multiple panes if init() is called twice.
    if (container.__velvetPane && typeof container.__velvetPane.dispose === "function") {
      try {
        container.__velvetPane.dispose();
      } catch {
        // ignore
      }
    }

    var pane = new window.Tweakpane.Pane({
      title: config.title || "Velvet Debug",
      container: container,
    });

    container.__velvetPane = pane;

    var params = {
      camera_position: { x: 0, y: 0, z: 3 },
      camera_target: { x: 0, y: 0, z: 0 },
      camera_forward: { x: 0, y: 0, z: -1 },
      camera_fovDeg: 60,
      camera_near: 0.1,
      camera_far: 100,

      dir_enabled: true,
      dir_direction: { x: 0.5, y: -1.0, z: -0.3 },
      dir_colorHex: "#ffffff",
      dir_intensity: 1.25,

      point_enabled: true,
      point_position: { x: 1.5, y: 1.2, z: 1.5 },
      point_colorHex: "#ffe6cc",
      point_intensity: 2.0,
      point_constant: 1.0,
      point_linear: 0.14,
      point_quadratic: 0.07,
    };

    var refreshInFlight = false;

    async function refreshFromHost() {
      if (!camera || !camera.dotnet || !camera.getState) return;
      if (refreshInFlight) return;
      refreshInFlight = true;
      try {
        var state = await invoke(camera.dotnet, camera.getState);
        if (!state) return;

        if (state.camera) {
          params.camera_position = vec3FromDto(state.camera.position, params.camera_position);
          params.camera_target = vec3FromDto(state.camera.target, params.camera_target);
          params.camera_forward = vec3FromDto(state.camera.forward, params.camera_forward);
          params.camera_fovDeg = safeNumber(radToDeg(state.camera.fovYRadians), params.camera_fovDeg);
          params.camera_near = safeNumber(state.camera.nearPlane, params.camera_near);
          params.camera_far = safeNumber(state.camera.farPlane, params.camera_far);
        }

        if (state.directionalLight) {
          params.dir_enabled = !!state.directionalLight.enabled;
          params.dir_direction = vec3FromDto(state.directionalLight.direction, params.dir_direction);
          var dirRgb = colorFromDto(state.directionalLight.color, hexToRgb01(params.dir_colorHex));
          params.dir_colorHex = rgb01ToHex(dirRgb);
          params.dir_intensity = safeNumber(state.directionalLight.intensity, params.dir_intensity);
        }

        if (state.pointLight) {
          params.point_enabled = !!state.pointLight.enabled;
          params.point_position = vec3FromDto(state.pointLight.position, params.point_position);
          var pRgb = colorFromDto(state.pointLight.color, hexToRgb01(params.point_colorHex));
          params.point_colorHex = rgb01ToHex(pRgb);
          params.point_intensity = safeNumber(state.pointLight.intensity, params.point_intensity);
          params.point_constant = safeNumber(state.pointLight.constant, params.point_constant);
          params.point_linear = safeNumber(state.pointLight.linear, params.point_linear);
          params.point_quadratic = safeNumber(state.pointLight.quadratic, params.point_quadratic);
        }

        pane.refresh();
      } catch (e) {
        console.warn("[VelvetDebugUI] Refresh failed:", e);
      } finally {
        refreshInFlight = false;
      }
    }

    // Camera
    if (camera) {
      var camFolder = pane.addFolder({ title: "Camera" });

      var pos = camFolder.addFolder({ title: "Position" });
      pos.addBinding(params.camera_position, "x", { min: -20, max: 20, step: 0.01 }).on("change", async () => {
        if (!camera.setPosition) return;
        await invoke(camera.dotnet, camera.setPosition, params.camera_position.x, params.camera_position.y, params.camera_position.z);
        if (typeof config.onChange === "function") config.onChange("camera.position");
      });
      pos.addBinding(params.camera_position, "y", { min: -20, max: 20, step: 0.01 }).on("change", async () => {
        if (!camera.setPosition) return;
        await invoke(camera.dotnet, camera.setPosition, params.camera_position.x, params.camera_position.y, params.camera_position.z);
        if (typeof config.onChange === "function") config.onChange("camera.position");
      });
      pos.addBinding(params.camera_position, "z", { min: -20, max: 20, step: 0.01 }).on("change", async () => {
        if (!camera.setPosition) return;
        await invoke(camera.dotnet, camera.setPosition, params.camera_position.x, params.camera_position.y, params.camera_position.z);
        if (typeof config.onChange === "function") config.onChange("camera.position");
      });

      var tgt = camFolder.addFolder({ title: "LookAt" });
      tgt.addBinding(params.camera_target, "x", { min: -20, max: 20, step: 0.01 }).on("change", async () => {
        if (!camera.setTarget) return;
        await invoke(camera.dotnet, camera.setTarget, params.camera_target.x, params.camera_target.y, params.camera_target.z);
        if (typeof config.onChange === "function") config.onChange("camera.target");
      });
      tgt.addBinding(params.camera_target, "y", { min: -20, max: 20, step: 0.01 }).on("change", async () => {
        if (!camera.setTarget) return;
        await invoke(camera.dotnet, camera.setTarget, params.camera_target.x, params.camera_target.y, params.camera_target.z);
        if (typeof config.onChange === "function") config.onChange("camera.target");
      });
      tgt.addBinding(params.camera_target, "z", { min: -20, max: 20, step: 0.01 }).on("change", async () => {
        if (!camera.setTarget) return;
        await invoke(camera.dotnet, camera.setTarget, params.camera_target.x, params.camera_target.y, params.camera_target.z);
        if (typeof config.onChange === "function") config.onChange("camera.target");
      });

      var proj = camFolder.addFolder({ title: "Projection" });
      proj.addBinding(params, "camera_fovDeg", { label: "FOV (deg)", min: 5, max: 175, step: 0.1 }).on("change", async () => {
        if (!camera.setPerspective) return;
        await invoke(camera.dotnet, camera.setPerspective, degToRad(params.camera_fovDeg), params.camera_near, params.camera_far);
        if (typeof config.onChange === "function") config.onChange("camera.perspective");
      });
      proj.addBinding(params, "camera_near", { label: "Near", min: 0.01, max: 10, step: 0.01 }).on("change", async () => {
        if (!camera.setPerspective) return;
        await invoke(camera.dotnet, camera.setPerspective, degToRad(params.camera_fovDeg), params.camera_near, params.camera_far);
        if (typeof config.onChange === "function") config.onChange("camera.perspective");
      });
      proj.addBinding(params, "camera_far", { label: "Far", min: 1, max: 1000, step: 1 }).on("change", async () => {
        if (!camera.setPerspective) return;
        await invoke(camera.dotnet, camera.setPerspective, degToRad(params.camera_fovDeg), params.camera_near, params.camera_far);
        if (typeof config.onChange === "function") config.onChange("camera.perspective");
      });

      var fwd = camFolder.addFolder({ title: "Forward (read-only)" });
      fwd.addBinding(params.camera_forward, "x", { readonly: true });
      fwd.addBinding(params.camera_forward, "y", { readonly: true });
      fwd.addBinding(params.camera_forward, "z", { readonly: true });

      camFolder.addButton({ title: "Refresh" }).on("click", refreshFromHost);

      // Prime UI from host.
      await refreshFromHost();

      // Low-frequency polling keeps the pane honest if other code modifies state.
      var pollMs = typeof config.pollMs === "number" ? config.pollMs : 500;
      if (pollMs > 0) {
        var pollHandle = window.setInterval(refreshFromHost, pollMs);
        // Dispose hook
        var oldDispose = pane.dispose.bind(pane);
        pane.dispose = function () {
          window.clearInterval(pollHandle);
          return oldDispose();
        };
      }
    }

    // Directional light
    if (directionalLight) {
      var dirFolder = pane.addFolder({ title: "Directional Light" });

      dirFolder.addBinding(params, "dir_enabled", { label: "Enabled" }).on("change", async () => {
        if (!directionalLight.setEnabled) return;
        await invoke(directionalLight.dotnet, directionalLight.setEnabled, !!params.dir_enabled);
        if (typeof config.onChange === "function") config.onChange("directional.enabled");
      });

      var dirVec = dirFolder.addFolder({ title: "Direction" });
      dirVec.addBinding(params.dir_direction, "x", { min: -1, max: 1, step: 0.01 }).on("change", async () => {
        if (!directionalLight.setDirection) return;
        await invoke(directionalLight.dotnet, directionalLight.setDirection, params.dir_direction.x, params.dir_direction.y, params.dir_direction.z);
        if (typeof config.onChange === "function") config.onChange("directional.direction");
      });
      dirVec.addBinding(params.dir_direction, "y", { min: -1, max: 1, step: 0.01 }).on("change", async () => {
        if (!directionalLight.setDirection) return;
        await invoke(directionalLight.dotnet, directionalLight.setDirection, params.dir_direction.x, params.dir_direction.y, params.dir_direction.z);
        if (typeof config.onChange === "function") config.onChange("directional.direction");
      });
      dirVec.addBinding(params.dir_direction, "z", { min: -1, max: 1, step: 0.01 }).on("change", async () => {
        if (!directionalLight.setDirection) return;
        await invoke(directionalLight.dotnet, directionalLight.setDirection, params.dir_direction.x, params.dir_direction.y, params.dir_direction.z);
        if (typeof config.onChange === "function") config.onChange("directional.direction");
      });

      dirFolder.addBinding(params, "dir_colorHex", { label: "Color" }).on("change", async () => {
        if (!directionalLight.setColor) return;
        var rgb = hexToRgb01(params.dir_colorHex);
        await invoke(directionalLight.dotnet, directionalLight.setColor, rgb.r, rgb.g, rgb.b);
        if (typeof config.onChange === "function") config.onChange("directional.color");
      });

      dirFolder.addBinding(params, "dir_intensity", { label: "Intensity", min: 0, max: 10, step: 0.01 }).on("change", async () => {
        if (!directionalLight.setIntensity) return;
        await invoke(directionalLight.dotnet, directionalLight.setIntensity, params.dir_intensity);
        if (typeof config.onChange === "function") config.onChange("directional.intensity");
      });
    }

    // Point light
    if (pointLight) {
      var pointFolder = pane.addFolder({ title: "Point Light" });

      pointFolder.addBinding(params, "point_enabled", { label: "Enabled" }).on("change", async () => {
        if (!pointLight.setEnabled) return;
        await invoke(pointLight.dotnet, pointLight.setEnabled, !!params.point_enabled);
        if (typeof config.onChange === "function") config.onChange("point.enabled");
      });

      var pPos = pointFolder.addFolder({ title: "Position" });
      pPos.addBinding(params.point_position, "x", { min: -20, max: 20, step: 0.01 }).on("change", async () => {
        if (!pointLight.setPosition) return;
        await invoke(pointLight.dotnet, pointLight.setPosition, params.point_position.x, params.point_position.y, params.point_position.z);
        if (typeof config.onChange === "function") config.onChange("point.position");
      });
      pPos.addBinding(params.point_position, "y", { min: -20, max: 20, step: 0.01 }).on("change", async () => {
        if (!pointLight.setPosition) return;
        await invoke(pointLight.dotnet, pointLight.setPosition, params.point_position.x, params.point_position.y, params.point_position.z);
        if (typeof config.onChange === "function") config.onChange("point.position");
      });
      pPos.addBinding(params.point_position, "z", { min: -20, max: 20, step: 0.01 }).on("change", async () => {
        if (!pointLight.setPosition) return;
        await invoke(pointLight.dotnet, pointLight.setPosition, params.point_position.x, params.point_position.y, params.point_position.z);
        if (typeof config.onChange === "function") config.onChange("point.position");
      });

      pointFolder.addBinding(params, "point_colorHex", { label: "Color" }).on("change", async () => {
        if (!pointLight.setColor) return;
        var rgb = hexToRgb01(params.point_colorHex);
        await invoke(pointLight.dotnet, pointLight.setColor, rgb.r, rgb.g, rgb.b);
        if (typeof config.onChange === "function") config.onChange("point.color");
      });

      pointFolder.addBinding(params, "point_intensity", { label: "Intensity", min: 0, max: 25, step: 0.01 }).on("change", async () => {
        if (!pointLight.setIntensity) return;
        await invoke(pointLight.dotnet, pointLight.setIntensity, params.point_intensity);
        if (typeof config.onChange === "function") config.onChange("point.intensity");
      });

      var atten = pointFolder.addFolder({ title: "Attenuation" });
      atten.addBinding(params, "point_constant", { label: "Constant", min: 0.01, max: 5, step: 0.01 }).on("change", async () => {
        if (!pointLight.setAttenuation) return;
        await invoke(pointLight.dotnet, pointLight.setAttenuation, params.point_constant, params.point_linear, params.point_quadratic);
        if (typeof config.onChange === "function") config.onChange("point.attenuation");
      });
      atten.addBinding(params, "point_linear", { label: "Linear", min: 0, max: 2, step: 0.001 }).on("change", async () => {
        if (!pointLight.setAttenuation) return;
        await invoke(pointLight.dotnet, pointLight.setAttenuation, params.point_constant, params.point_linear, params.point_quadratic);
        if (typeof config.onChange === "function") config.onChange("point.attenuation");
      });
      atten.addBinding(params, "point_quadratic", { label: "Quadratic", min: 0, max: 2, step: 0.001 }).on("change", async () => {
        if (!pointLight.setAttenuation) return;
        await invoke(pointLight.dotnet, pointLight.setAttenuation, params.point_constant, params.point_linear, params.point_quadratic);
        if (typeof config.onChange === "function") config.onChange("point.attenuation");
      });
    }

    // Renderer (optional)
    if (renderer) {
      var rFolder = pane.addFolder({ title: "Renderer" });

      if (renderer.pause) {
        rFolder.addButton({ title: "Pause" }).on("click", async () => {
          await invoke(renderer.dotnet, renderer.pause);
          if (typeof config.onChange === "function") config.onChange("renderer.pause");
        });
      }

      if (renderer.resume) {
        rFolder.addButton({ title: "Resume" }).on("click", async () => {
          await invoke(renderer.dotnet, renderer.resume);
          if (typeof config.onChange === "function") config.onChange("renderer.resume");
        });
      }
    }

    // Convenience global for devtools.
    window.__VelvetDebugUIPane = pane;

    return pane;
  }

  window.VelvetDebugUI = {
    init: init,
  };
})();
