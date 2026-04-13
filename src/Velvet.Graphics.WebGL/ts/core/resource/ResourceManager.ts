/**
 * ResourceManager
 * ----------------
 * Central registry for all engine-side resources.
 *
 * Why this exists:
 * - JS cannot pass objects to C# (only IDs)
 * - Blazor cannot store JS object references
 * - WebGL resources need proper cleanup
 * - Engine requires a single place to store shaders, buffers, programs, meshes
 *
 * This manager gives Velvet a stable ID → Resource lookup system.
 */

export class ResourceManager<TResource> {
    private resources = new Map<number, TResource>();
    private nextId = 1;

    /**
     * Creates a unique ID for a new resource.
     */
    public generateId(): number {
        return this.nextId++;
    }

    /**
     * Registers a resource and returns its ID.
     */
    public add(resource: TResource): number {
        const id = this.generateId();
        this.register(id, resource);
        return id;
    }

    /**
     * Registers a resource with an explicit ID.
     * Useful when the resource object also stores its own ID and both must stay aligned.
     */
    public register(id: number, resource: TResource): void {
        if (!Number.isInteger(id) || id <= 0) {
            throw new Error(`ResourceManager.register: id must be a positive integer (received ${id})`);
        }
        if (this.resources.has(id)) {
            throw new Error(`ResourceManager.register: duplicate id=${id}`);
        }

        this.resources.set(id, resource);
        if (id >= this.nextId) {
            this.nextId = id + 1;
        }
    }

    /**
     * Retrieves a resource by ID.
     */
    public get(id: number): TResource {
        const resource = this.resources.get(id);
        if (resource === undefined) {
            throw new Error(`ResourceManager: No resource found for id=${id}`);
        }
        return resource;
    }

    /**
     * Removes and returns a resource.
     */
    public remove(id: number): TResource | null {
        const resource = this.resources.get(id);
        this.resources.delete(id);
        return resource ?? null;
    }

    /**
     * Clears ALL resources.
     */
    public clear(): void {
        this.resources.clear();
    }
}
