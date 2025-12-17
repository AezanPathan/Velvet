namespace Velvet.Core.Rendering;

/// <summary>
/// Opaque identifier for a GPU buffer allocated by a backend.
/// The meaning of the integer value is backend-defined.
/// </summary>
public readonly record struct GpuBufferId(int Value);
