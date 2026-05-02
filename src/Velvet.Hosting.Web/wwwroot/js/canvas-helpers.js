const canvasResizeBindings = new Map();
const canvasOrbitBindings = new Map();
const canvasAnimationLoops = new Map();
const canvasDebugOverlays = new Map();
let nextResizeBindingId = 1;
let nextOrbitBindingId = 1;
let nextAnimationLoopId = 1;

window.CanvasHelpers = {
    bindResizeTracking: function (canvas, dotNetRef, methodName) {
        if (!(canvas instanceof HTMLCanvasElement)) {
            throw new Error("CanvasHelpers.bindResizeTracking: provided element is not a canvas");
        }

        if (!dotNetRef || typeof dotNetRef.invokeMethodAsync !== "function") {
            throw new Error("CanvasHelpers.bindResizeTracking: dotNetRef is invalid");
        }

        const invokeResize = () => {
            const rect = canvas.getBoundingClientRect();
            const width = Math.max(1, Math.round(rect.width));
            const height = Math.max(1, Math.round(rect.height));
            const dpr = window.devicePixelRatio || 1.0;
            return dotNetRef.invokeMethodAsync(methodName, width, height, dpr);
        };

        const onWindowResize = () => {
            invokeResize().catch(() => { });
        };

        window.addEventListener("resize", onWindowResize);
        const bindingId = `resize-${nextResizeBindingId++}`;
        canvasResizeBindings.set(bindingId, onWindowResize);
        invokeResize().catch(() => { });
        return bindingId;
    },

    bindResizeTrackingById: function (canvasId, dotNetRef, methodName) {
        const canvas = document.getElementById(canvasId);
        if (!(canvas instanceof HTMLCanvasElement)) {
            throw new Error("CanvasHelpers.bindResizeTrackingById: canvas not found or not a canvas: " + canvasId);
        }

        return this.bindResizeTracking(canvas, dotNetRef, methodName);
    },

    unbindResizeTracking: function (bindingId) {
        const onWindowResize = canvasResizeBindings.get(bindingId);
        if (!onWindowResize) {
            return;
        }

        window.removeEventListener("resize", onWindowResize);
        canvasResizeBindings.delete(bindingId);
    },

    bindOrbitInput: function (canvas, dotNetRef, mouseDownMethod, mouseMoveMethod, mouseUpMethod, wheelMethod, touchRotateMethod, touchZoomMethod) {
        if (!(canvas instanceof HTMLCanvasElement)) {
            throw new Error("CanvasHelpers.bindOrbitInput: provided element is not a canvas");
        }

        if (!dotNetRef || typeof dotNetRef.invokeMethodAsync !== "function") {
            throw new Error("CanvasHelpers.bindOrbitInput: dotNetRef is invalid");
        }

        const onMouseDown = (event) => {
            dotNetRef.invokeMethodAsync(mouseDownMethod, Math.round(event.clientX), Math.round(event.clientY)).catch(() => { });
        };
        const onMouseMove = (event) => {
            dotNetRef.invokeMethodAsync(mouseMoveMethod, Math.round(event.clientX), Math.round(event.clientY)).catch(() => { });
        };
        const onMouseUp = () => {
            dotNetRef.invokeMethodAsync(mouseUpMethod).catch(() => { });
        };
        const onMouseLeave = () => {
            dotNetRef.invokeMethodAsync(mouseUpMethod).catch(() => { });
        };
        const onWheel = (event) => {
            event.preventDefault();
            dotNetRef.invokeMethodAsync(wheelMethod, event.deltaY).catch(() => { });
        };

        const getTouchDistance = (touchA, touchB) => {
            const dx = touchA.clientX - touchB.clientX;
            const dy = touchA.clientY - touchB.clientY;
            return Math.hypot(dx, dy);
        };

        let lastTouchX = 0;
        let lastTouchY = 0;
        let isDraggingTouch = false;
        let lastPinchDistance = 0;
        let isPinching = false;

        const onTouchStart = (event) => {
            if (event.touches.length === 1) {
                const touch = event.touches[0];
                isDraggingTouch = true;
                isPinching = false;
                lastTouchX = touch.clientX;
                lastTouchY = touch.clientY;
                return;
            }

            if (event.touches.length === 2) {
                isDraggingTouch = false;
                isPinching = true;
                lastPinchDistance = getTouchDistance(event.touches[0], event.touches[1]);
            }
        };

        const onTouchMove = (event) => {
            if (event.touches.length === 1 && typeof touchRotateMethod === "string" && touchRotateMethod.length > 0 && isDraggingTouch) {
                event.preventDefault();
                const touch = event.touches[0];
                const dx = touch.clientX - lastTouchX;
                const dy = touch.clientY - lastTouchY;
                lastTouchX = touch.clientX;
                lastTouchY = touch.clientY;

                dotNetRef.invokeMethodAsync(touchRotateMethod, dx, dy).catch(() => { });
                return;
            }

            if (event.touches.length === 2 && typeof touchZoomMethod === "string" && touchZoomMethod.length > 0) {
                event.preventDefault();
                const distance = getTouchDistance(event.touches[0], event.touches[1]);
                if (isPinching) {
                    const delta = lastPinchDistance - distance;
                    dotNetRef.invokeMethodAsync(touchZoomMethod, delta).catch(() => { });
                }

                lastPinchDistance = distance;
                isPinching = true;
            }
        };

        const onTouchEnd = (event) => {
            if (event.touches.length === 1) {
                const touch = event.touches[0];
                isDraggingTouch = true;
                isPinching = false;
                lastTouchX = touch.clientX;
                lastTouchY = touch.clientY;
                return;
            }

            if (event.touches.length === 2) {
                isDraggingTouch = false;
                isPinching = true;
                lastPinchDistance = getTouchDistance(event.touches[0], event.touches[1]);
                return;
            }

            isDraggingTouch = false;
            isPinching = false;
            lastPinchDistance = 0;
        };

        canvas.addEventListener("mousedown", onMouseDown);
        canvas.addEventListener("mousemove", onMouseMove);
        canvas.addEventListener("mouseup", onMouseUp);
        canvas.addEventListener("mouseleave", onMouseLeave);
        canvas.addEventListener("wheel", onWheel, { passive: false });
        canvas.addEventListener("touchstart", onTouchStart, { passive: true });
        canvas.addEventListener("touchmove", onTouchMove, { passive: false });
        canvas.addEventListener("touchend", onTouchEnd, { passive: true });

        const bindingId = `orbit-${nextOrbitBindingId++}`;
        canvasOrbitBindings.set(bindingId, {
            canvas,
            onMouseDown,
            onMouseMove,
            onMouseUp,
            onMouseLeave,
            onWheel,
            onTouchStart,
            onTouchMove,
            onTouchEnd
        });

        return bindingId;
    },

    bindOrbitInputById: function (canvasId, dotNetRef, mouseDownMethod, mouseMoveMethod, mouseUpMethod, wheelMethod, touchRotateMethod, touchZoomMethod) {
        const canvas = document.getElementById(canvasId);
        if (!(canvas instanceof HTMLCanvasElement)) {
            throw new Error("CanvasHelpers.bindOrbitInputById: canvas not found or not a canvas: " + canvasId);
        }

        return this.bindOrbitInput(canvas, dotNetRef, mouseDownMethod, mouseMoveMethod, mouseUpMethod, wheelMethod, touchRotateMethod, touchZoomMethod);
    },

    unbindOrbitInput: function (bindingId) {
        const binding = canvasOrbitBindings.get(bindingId);
        if (!binding) {
            return;
        }

        binding.canvas.removeEventListener("mousedown", binding.onMouseDown);
        binding.canvas.removeEventListener("mousemove", binding.onMouseMove);
        binding.canvas.removeEventListener("mouseup", binding.onMouseUp);
        binding.canvas.removeEventListener("mouseleave", binding.onMouseLeave);
        binding.canvas.removeEventListener("wheel", binding.onWheel);
        binding.canvas.removeEventListener("touchstart", binding.onTouchStart);
        binding.canvas.removeEventListener("touchmove", binding.onTouchMove);
        binding.canvas.removeEventListener("touchend", binding.onTouchEnd);
        canvasOrbitBindings.delete(bindingId);
    },

    startAnimationLoopById: function (canvasId, dotNetRef, methodName) {
        const canvas = document.getElementById(canvasId);
        if (!(canvas instanceof HTMLCanvasElement)) {
            throw new Error("CanvasHelpers.startAnimationLoopById: canvas not found or not a canvas: " + canvasId);
        }

        if (!dotNetRef || typeof dotNetRef.invokeMethodAsync !== "function") {
            throw new Error("CanvasHelpers.startAnimationLoopById: dotNetRef is invalid");
        }

        const bindingId = `raf-${nextAnimationLoopId++}`;
        const loopState = {
            isRunning: true,
            rafId: 0
        };

        const tick = (timestampMs) => {
            if (!loopState.isRunning) {
                return;
            }

            Promise
                .resolve(dotNetRef.invokeMethodAsync(methodName, timestampMs))
                .catch(() => { })
                .finally(() => {
                    if (!loopState.isRunning) {
                        return;
                    }

                    loopState.rafId = window.requestAnimationFrame(tick);
                });
        };

        loopState.rafId = window.requestAnimationFrame(tick);
        canvasAnimationLoops.set(bindingId, loopState);
        return bindingId;
    },

    stopAnimationLoop: function (bindingId) {
        const loopState = canvasAnimationLoops.get(bindingId);
        if (!loopState) {
            return;
        }

        loopState.isRunning = false;
        if (loopState.rafId) {
            window.cancelAnimationFrame(loopState.rafId);
        }

        canvasAnimationLoops.delete(bindingId);
    },

    setDebugOverlayById: function (canvasId, text) {
        const canvas = document.getElementById(canvasId);
        if (!(canvas instanceof HTMLCanvasElement)) {
            return;
        }

        const id = `velvet-debug-${canvasId}`;
        let overlay = canvasDebugOverlays.get(id);

        if (!overlay) {
            overlay = document.createElement("div");
            overlay.id = id;
            overlay.style.position = "fixed";
            overlay.style.zIndex = "2147483647";
            overlay.style.pointerEvents = "none";
            overlay.style.background = "rgba(0, 0, 0, 0.65)";
            overlay.style.color = "#b8ffb8";
            overlay.style.fontFamily = "ui-monospace, SFMono-Regular, Menlo, Consolas, monospace";
            overlay.style.fontSize = "12px";
            overlay.style.lineHeight = "1.35";
            overlay.style.padding = "8px 10px";
            overlay.style.border = "1px solid rgba(184, 255, 184, 0.35)";
            overlay.style.borderRadius = "6px";
            overlay.style.whiteSpace = "pre";
            document.body.appendChild(overlay);
            canvasDebugOverlays.set(id, overlay);
        }

        const rect = canvas.getBoundingClientRect();
        overlay.style.left = `${Math.round(rect.left + 10)}px`;
        overlay.style.top = `${Math.round(rect.top + 10)}px`;
        overlay.textContent = String(text ?? "");
    },

    clearDebugOverlayById: function (canvasId) {
        const id = `velvet-debug-${canvasId}`;
        const overlay = canvasDebugOverlays.get(id);
        if (!overlay) {
            return;
        }

        overlay.remove();
        canvasDebugOverlays.delete(id);
    }
};
