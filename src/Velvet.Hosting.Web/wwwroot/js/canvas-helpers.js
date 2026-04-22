const canvasResizeBindings = new Map();
const canvasOrbitBindings = new Map();
let nextResizeBindingId = 1;
let nextOrbitBindingId = 1;

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

    bindOrbitInput: function (canvas, dotNetRef, mouseDownMethod, mouseMoveMethod, mouseUpMethod, wheelMethod) {
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

        canvas.addEventListener("mousedown", onMouseDown);
        canvas.addEventListener("mousemove", onMouseMove);
        canvas.addEventListener("mouseup", onMouseUp);
        canvas.addEventListener("mouseleave", onMouseLeave);
        canvas.addEventListener("wheel", onWheel, { passive: false });

        const bindingId = `orbit-${nextOrbitBindingId++}`;
        canvasOrbitBindings.set(bindingId, {
            canvas,
            onMouseDown,
            onMouseMove,
            onMouseUp,
            onMouseLeave,
            onWheel
        });

        return bindingId;
    },

    bindOrbitInputById: function (canvasId, dotNetRef, mouseDownMethod, mouseMoveMethod, mouseUpMethod, wheelMethod) {
        const canvas = document.getElementById(canvasId);
        if (!(canvas instanceof HTMLCanvasElement)) {
            throw new Error("CanvasHelpers.bindOrbitInputById: canvas not found or not a canvas: " + canvasId);
        }

        return this.bindOrbitInput(canvas, dotNetRef, mouseDownMethod, mouseMoveMethod, mouseUpMethod, wheelMethod);
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
        canvasOrbitBindings.delete(bindingId);
    }
};
