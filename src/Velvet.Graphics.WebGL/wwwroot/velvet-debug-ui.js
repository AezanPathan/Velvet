// Debug UI intentionally disabled.
// This file is kept as a harmless no-op so any existing loader continues to work
// without rendering the developer overlay.
(function () {
  if (typeof window === "undefined") {
    return;
  }

  window.VelvetDebugUI = {
    init: function () {
      return null;
    },
    refresh: function () {
      return null;
    }
  };
})();
