(function () {
    const startedCanvases = new Set();
    const startPromises = new Map();
    let blazorStartPromise = null;

    if (!window.Velvet) {
        window.Velvet = {};
    }

    function errorMessage(error) {
        return error && error.message ? String(error.message) : String(error);
    }

    function isBlazorAlreadyStartedError(error) {
        const message = errorMessage(error);
        return message.toLowerCase().includes("blazor has already started");
    }

    function ensureBlazorStarted() {
        if (blazorStartPromise) {
            return blazorStartPromise;
        }

        blazorStartPromise = Promise.resolve()
            .then(() => {
                if (!window.Blazor || typeof window.Blazor.start !== "function") {
                    throw new Error("Blazor runtime not loaded. Include /_framework/blazor.server.js before Velvet.start().");
                }

                try {
                    const result = window.Blazor.start();
                    return Promise.resolve(result).catch((error) => {
                        if (isBlazorAlreadyStartedError(error)) {
                            return;
                        }

                        throw error;
                    });
                } catch (error) {
                    if (isBlazorAlreadyStartedError(error)) {
                        return Promise.resolve();
                    }

                    throw error;
                }
            })
            .catch((error) => {
                blazorStartPromise = null;
                throw error;
            });

        return blazorStartPromise;
    }

    window.Velvet.start = function (canvasId) {
        if (startedCanvases.has(canvasId)) {
            return Promise.resolve();
        }

        if (!canvasId || typeof canvasId !== "string") {
            return Promise.reject(new Error("Velvet.start(canvasId) requires a non-empty canvas id."));
        }

        if (startPromises.has(canvasId)) {
            return startPromises.get(canvasId);
        }

        const startPromise = ensureBlazorStarted()
            .then(() => DotNet.invokeMethodAsync("Velvet.Hosting.Web", "Start", canvasId))
            .then(() => {
                startedCanvases.add(canvasId);
            })
            .catch((error) => {
                startPromises.delete(canvasId);
                throw error;
            });

        startPromises.set(canvasId, startPromise);

        return startPromise;
    };
})();
