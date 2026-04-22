(function () {
    let started = false;
    let startPromise = null;

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

    window.Velvet.start = function (canvasId) {
        if (started) {
            return Promise.resolve();
        }

        if (!canvasId || typeof canvasId !== "string") {
            return Promise.reject(new Error("Velvet.start(canvasId) requires a non-empty canvas id."));
        }

        if (startPromise) {
            return startPromise;
        }

        startPromise = Promise.resolve()
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
            .then(() => DotNet.invokeMethodAsync("Velvet.Hosting.Web", "Start", canvasId))
            .then(() => {
                started = true;
            })
            .catch((error) => {
                startPromise = null;
                throw error;
            });

        return startPromise;
    };
})();
