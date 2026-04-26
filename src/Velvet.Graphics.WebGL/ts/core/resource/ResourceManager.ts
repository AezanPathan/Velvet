/**
 * Generic ID → Resource registry.
 *
 * Purpose:
 * - JS ↔ C# interop requires ID-based references
 * - WebGL resources cannot be passed directly across boundaries
 * - Centralized lifecycle management
 */
export class ResourceManager<TResource> {
    private resources = new Map<number, TResource>();
    private nextId = 1;

    /** Generate a new unique resource ID */
    public generateId(): number {
        return this.nextId++;
    }

    /** Add a resource and return its ID */
    public add(resource: TResource): number {
        const id = this.generateId();
        this.register(id, resource);
        return id;
    }

    /** Register resource with explicit ID */
    public register(id: number, resource: TResource): void {
        if (!Number.isInteger(id) || id <= 0) {
            throw new Error(`ResourceManager.register: invalid id=${id}`);
        }
        if (this.resources.has(id)) {
            throw new Error(`ResourceManager.register: duplicate id=${id}`);
        }

        this.resources.set(id, resource);

        if (id >= this.nextId) {
            this.nextId = id + 1;
        }
    }

    /** Get resource by ID */
    public get(id: number): TResource {
        const resource = this.resources.get(id);
        if (!resource) {
            throw new Error(`ResourceManager: missing id=${id}`);
        }
        return resource;
    }

    /** Remove resource */
    public remove(id: number): TResource | null {
        const resource = this.resources.get(id);
        this.resources.delete(id);
        return resource ?? null;
    }

    /** Clear all resources */
    public clear(): void {
        this.resources.clear();
    }
}