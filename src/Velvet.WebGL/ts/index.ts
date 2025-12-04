import { init, ensureCanvas, drawTriangle, drawCube } from './api/VelvetAPI';
import './api/types';

// Expose global API
window.Velvet = {
    init,
    ensureCanvas,
    drawTriangle,
    drawCube,
};

// Export for module usage
export { init, ensureCanvas, drawTriangle, drawCube };
