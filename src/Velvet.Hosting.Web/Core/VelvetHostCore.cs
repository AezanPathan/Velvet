using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Batching;
using Velvet.Core.Rendering.Cameras;
using Velvet.Core.Rendering.Culling;
using Velvet.Core.Rendering.Environment;
using Velvet.Core.Rendering.Lighting;
using Velvet.Core.Rendering.Meshes;
using Velvet.Core.Rendering.Skinning;
using Velvet.Core.Scene;
using Velvet.Graphics.WebGL;

namespace Velvet.Hosting.Web.Core;

/// <summary>
/// Shared rendering core for Velvet hosts.
/// Contains scene management, batching, lighting, and frame execution logic.
/// Hosts (Blazor and Razor) delegate rendering operations to this core.
/// </summary>
public abstract class VelvetHostCore
{
    protected readonly IWebGLBridge Bridge;
    protected readonly IMeshUploader MeshUploader;
    protected readonly int RendererId;

    protected readonly List<MeshInstance> Instances = new();
    protected readonly List<(Scene Scene, int Start, int Count)> SceneInstanceRanges = new();
    protected readonly Dictionary<Skin, float[]> BoneMatrixCache = new();
    protected readonly Frustum Frustum = new();

    protected List<RenderBatch>? Batches;
    protected int LastFrameTotalMeshes;
    protected int LastFrameCulledMeshes;
    protected int LastFrameRenderedMeshes;

    public ShaderProgram? Program;
    public ShaderProgram? SkyboxProgram;
    public Camera? Camera;

    public DirectionalLight? DirectionalLight;
    public PointLight? PointLight;
    public SpotLight? SpotLight;
    public Skybox? Skybox;

    protected bool DirectionalEnabled = true;
    protected bool PointEnabled = true;

    public bool EnableFrustumCulling { get; set; } = true;

    protected VelvetHostCore(IWebGLBridge bridge, int rendererId)
    {
        Bridge = bridge;
        MeshUploader = new WebGLMeshUploader(bridge);
        RendererId = rendererId;
    }

    /// <summary>
    /// Registers a scene with the application. Upload occurs on StartAsync.
    /// </summary>
    public void Add(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ThrowIfRunning();

        var sceneInstances = new List<MeshInstance>();
        scene.CollectMeshes(sceneInstances);

        var start = Instances.Count;
        foreach (var instance in sceneInstances)
        {
            Instances.Add(instance);
        }

        SceneInstanceRanges.Add((scene, start, sceneInstances.Count));
    }

    /// <summary>
    /// Prepares a scene for rendering using its current node transforms.
    /// Updates mesh instance matrices and computes bone matrices for skinned meshes.
    /// Animation time is not advanced here; call Animator.Update(dt) explicitly before rendering.
    /// </summary>
    public void Render(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        foreach (var range in SceneInstanceRanges)
        {
            if (!ReferenceEquals(range.Scene, scene))
            {
                continue;
            }

            UpdateSceneInstances(scene, range.Start, range.Count);
            UpdateBoneMatrices(scene, range.Start, range.Count);
        }
    }

    /// <summary>
    /// Sets the skybox for the scene.
    /// </summary>
    protected async Task SetSkyboxImplAsync(Skybox skybox)
    {
        ArgumentNullException.ThrowIfNull(skybox);
        ThrowIfRunning();

        Skybox = skybox;

        if (SkyboxProgram is null)
        {
            SkyboxProgram = await ShaderProgram.CreateSkyboxAsync(Bridge).ConfigureAwait(false);
        }

        await skybox.Mesh.UploadAsync(MeshUploader).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates and sets a cubemap skybox from 6 face images.
    /// Face order: +X, -X, +Y, -Y, +Z, -Z
    /// </summary>
    protected async Task SetCubemapSkyboxImplAsync(string px, string nx, string py, string ny, string pz, string nz)
    {
        ThrowIfRunning();

        var faceUrls = new[] { px, nx, py, ny, pz, nz };
        var textureId = await Bridge.CreateCubemapTextureAsync(faceUrls).ConfigureAwait(false);

        var geometry = new SkyboxGeometry();
        var mesh = new Mesh(geometry);
        var skybox = new Skybox(mesh, textureId);

        await SetSkyboxImplAsync(skybox).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates mesh instance matrices based on current scene transforms.
    /// </summary>
    protected void UpdateSceneInstances(Scene scene, int startIndex, int instanceCount)
    {
        var nextIndex = startIndex;
        var endIndex = startIndex + instanceCount;

        foreach (var root in scene.Roots)
        {
            UpdateSceneNode(root, Matrix4.Identity.Data, ref nextIndex, endIndex);
        }

        if (nextIndex != endIndex)
        {
            throw new InvalidOperationException("Mesh instance count mismatch while updating transforms.");
        }
    }

    /// <summary>
    /// Recursively updates scene nodes and their mesh transforms.
    /// </summary>
    protected void UpdateSceneNode(SceneNode node, float[] parentWorld, ref int nextIndex, int endIndex)
    {
        var world = Matrix4.Multiply(parentWorld, node.LocalTransform).Data;

        foreach (var mesh in node.Meshes)
        {
            if (nextIndex >= endIndex)
            {
                throw new InvalidOperationException("Mesh instance count mismatch while updating transforms.");
            }

            ApplyInstanceTransform(nextIndex, world);
            nextIndex++;
        }

        foreach (var child in node.Children)
        {
            UpdateSceneNode(child, world, ref nextIndex, endIndex);
        }
    }

    /// <summary>
    /// Updates bone matrices for skinned meshes in a scene range.
    /// </summary>
    protected void UpdateBoneMatrices(Scene scene, int startIndex, int instanceCount)
    {
        var endIndex = startIndex + instanceCount;
        var preparedSkins = new HashSet<Skin>();

        for (var i = startIndex; i < endIndex; i++)
        {
            var skin = Instances[i].Skin;
            if (skin is null || !preparedSkins.Add(skin))
            {
                continue;
            }

            BoneMatrixCache[skin] = BoneMatrixCalculator.ComputeBoneMatrices(skin, scene.Roots);
        }
    }

    /// <summary>
    /// Applies a transform to a mesh instance.
    /// </summary>
    protected void ApplyInstanceTransform(int index, float[] world)
    {
        var instance = Instances[index];

        var normalMatrix = Matrix.NormalMatrix(world);
        Instances[index] = new MeshInstance(instance.Mesh, world, normalMatrix, instance.Skin);
    }

    /// <summary>
    /// Renders the skybox if configured.
    /// </summary>
    protected async Task RenderSkyboxAsync(Camera camera)
    {
        if (Skybox is null || SkyboxProgram is null)
        {
            return;
        }

        await Bridge.SetDepthMaskAsync(RendererId, false).ConfigureAwait(false);
        await SkyboxProgram.SetUniformMatrix4fvAsync("uView", camera.ViewMatrix).ConfigureAwait(false);
        await SkyboxProgram.SetUniformMatrix4fvAsync("uProjection", camera.ProjectionMatrix).ConfigureAwait(false);

        if (Skybox.CubemapTextureId.HasValue)
        {
            await Bridge.BindCubemapTextureAsync(
                SkyboxProgram.ProgramId,
                "u_Skybox",
                Skybox.CubemapTextureId.Value,
                0).ConfigureAwait(false);
            await SkyboxProgram.SetUniform1bAsync("u_HasCubemap", true).ConfigureAwait(false);
        }
        else
        {
            await SkyboxProgram.SetUniform1bAsync("u_HasCubemap", false).ConfigureAwait(false);
        }

        var skyboxMeshId = Skybox.Mesh.Resources.VertexBufferId.Value;
        await SkyboxProgram.DrawMeshAsync(skyboxMeshId, RendererId).ConfigureAwait(false);
        await Bridge.SetDepthMaskAsync(RendererId, true).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets frame uniforms (view and projection matrices, lights).
    /// </summary>
    protected async Task SetFrameUniformsAsync(ShaderProgram program, Camera camera)
    {
        await program.SetUniformMatrix4fvAsync("uView", camera.ViewMatrix).ConfigureAwait(false);
        await program.SetUniformMatrix4fvAsync("uProjection", camera.ProjectionMatrix).ConfigureAwait(false);
        await SetFrameLightsAsync(program).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets lighting uniforms for the frame.
    /// </summary>
  protected async Task SetFrameLightsAsync(ShaderProgram program)
{
    // ==============================
    // DEFAULTS (VERY IMPORTANT)
    // ==============================

    // Directional (off by default)
    await program.SetUniform3fAsync("uLightDirection", 0f, -1f, 0f);
    await program.SetUniform3fAsync("uLightColor", 0f, 0f, 0f);
    await program.SetUniform1fAsync("uLightIntensity", 0f);

    // Point (off by default)
    await program.SetUniform3fAsync("uPointLightPosition", 0f, 0f, 0f);
    await program.SetUniform3fAsync("uPointLightColor", 0f, 0f, 0f);
    await program.SetUniform1fAsync("uPointLightIntensity", 0f);
    await program.SetUniform1fAsync("uPointLightConstant", 1f);
    await program.SetUniform1fAsync("uPointLightLinear", 0f);
    await program.SetUniform1fAsync("uPointLightQuadratic", 0f);

    // Spot (off by default)
    await program.SetUniform3fAsync("uSpotLightPosition", 0f, 0f, 0f);
    await program.SetUniform3fAsync("uSpotLightDirection", 0f, -1f, 0f);
    await program.SetUniform3fAsync("uSpotLightColor", 0f, 0f, 0f);
    await program.SetUniform1fAsync("uSpotLightIntensity", 0f);
    await program.SetUniform1fAsync("uSpotLightCutoff", 0f);
    await program.SetUniform1fAsync("uSpotLightOuterCutoff", 0f);
    await program.SetUniform1fAsync("uSpotLightConstant", 1f);
    await program.SetUniform1fAsync("uSpotLightLinear", 0f);
    await program.SetUniform1fAsync("uSpotLightQuadratic", 0f);


    // ==============================
    // DIRECTIONAL LIGHT
    // ==============================
    // if (DirectionalLight is not null && DirectionalEnabled)
    // {
    //     var dir = DirectionalLight.Direction;
    //     var normalizedDir = dir.LengthSquared > 0.000001f
    //         ? dir.Normalized()
    //         : new Vector3(0f, -1f, 0f);

    //     await program.SetUniform3fAsync("uLightDirection",
    //         normalizedDir.X, normalizedDir.Y, normalizedDir.Z);

    //     await program.SetUniform3fAsync("uLightColor",
    //         DirectionalLight.Color.X,
    //         DirectionalLight.Color.Y,
    //         DirectionalLight.Color.Z);

    //     await program.SetUniform1fAsync("uLightIntensity",
    //         DirectionalLight.Intensity);
    // }
    if (DirectionalLight is not null && DirectionalEnabled)
    {
        var dir = DirectionalLight.Direction;
        var normalizedDir = dir.LengthSquared > 0.000001f
            ? dir.Normalized()
            : new Vector3(0f, -1f, 0f);

        await program.SetUniform3fAsync("uLightDirection", normalizedDir.X, normalizedDir.Y, normalizedDir.Z);
        await program.SetUniform3fAsync("uLightColor",
            DirectionalLight.Color.X, DirectionalLight.Color.Y, DirectionalLight.Color.Z);
        await program.SetUniform1fAsync("uLightIntensity", DirectionalLight.Intensity);
    }



    // ==============================
    // POINT LIGHT
    // ==============================
    if (PointLight is not null && PointEnabled)
    {
        await program.SetUniform3fAsync("uPointLightPosition",
            PointLight.Position.X,
            PointLight.Position.Y,
            PointLight.Position.Z);

        await program.SetUniform3fAsync("uPointLightColor",
            PointLight.Color.X,
            PointLight.Color.Y,
            PointLight.Color.Z);

        await program.SetUniform1fAsync("uPointLightIntensity",
            PointLight.Intensity);

        await program.SetUniform1fAsync("uPointLightConstant",
            PointLight.Constant);

        await program.SetUniform1fAsync("uPointLightLinear",
            PointLight.Linear);

        await program.SetUniform1fAsync("uPointLightQuadratic",
            PointLight.Quadratic);
    }


    // ==============================
    // SPOT LIGHT
    // ==============================
    if (SpotLight is not null)
    {
        var dir = SpotLight.Direction;
        var normalizedDir = dir.LengthSquared > 0.000001f
            ? dir.Normalized()
            : new Vector3(0f, -1f, 0f);

        await program.SetUniform3fAsync("uSpotLightPosition",
            SpotLight.Position.X,
            SpotLight.Position.Y,
            SpotLight.Position.Z);

        await program.SetUniform3fAsync("uSpotLightDirection",
            normalizedDir.X,
            normalizedDir.Y,
            normalizedDir.Z);

        await program.SetUniform3fAsync("uSpotLightColor",
            SpotLight.Color.X,
            SpotLight.Color.Y,
            SpotLight.Color.Z);

        await program.SetUniform1fAsync("uSpotLightIntensity",
            SpotLight.Intensity);

        await program.SetUniform1fAsync("uSpotLightCutoff",
            SpotLight.Cutoff);

        await program.SetUniform1fAsync("uSpotLightOuterCutoff",
            SpotLight.OuterCutoff);

        await program.SetUniform1fAsync("uSpotLightConstant",
            SpotLight.Constant);

        await program.SetUniform1fAsync("uSpotLightLinear",
            SpotLight.Linear);

        await program.SetUniform1fAsync("uSpotLightQuadratic",
            SpotLight.Quadratic);
    }
}

    /// <summary>
    /// Renders all batches with frustum culling support.
    /// </summary>
    protected async Task RenderBatchesAsync(ShaderProgram program, List<RenderBatch> batches, Func<Mesh, Task>? beforeDrawMesh = null)
    {
        LastFrameTotalMeshes = 0;
        LastFrameCulledMeshes = 0;
        LastFrameRenderedMeshes = 0;

        foreach (var batch in batches)
        {
            await batch.Key.Material.ApplyAsync(program).ConfigureAwait(false);

            foreach (var instanceIndex in batch.InstanceIndices)
            {
                if ((uint)instanceIndex >= (uint)Instances.Count)
                {
                    continue;
                }

                var instance = Instances[instanceIndex];
                LastFrameTotalMeshes++;

                if (EnableFrustumCulling && !Frustum.Intersects(instance.BoundingBox))
                {
                    LastFrameCulledMeshes++;
                    continue;
                }

                var mesh = instance.Mesh;
                var meshId = mesh.Resources.VertexBufferId.Value;
                mesh.Skin = instance.Skin;

                if (beforeDrawMesh is not null)
                {
                    await beforeDrawMesh(mesh).ConfigureAwait(false);
                }

                if (mesh.Skin is not null && BoneMatrixCache.TryGetValue(mesh.Skin, out var boneMatrices))
                {
                    await program.SetBoneMatricesAsync(boneMatrices, mesh.Skin.JointCount).ConfigureAwait(false);
                }

                await program.SetUniformMatrix4fvAsync("uModel", instance.ModelMatrix).ConfigureAwait(false);
                await program.SetUniformMatrix3fvAsync("uNormalMatrix", instance.NormalMatrix).ConfigureAwait(false);
                await program.DrawMeshAsync(meshId, RendererId).ConfigureAwait(false);
                LastFrameRenderedMeshes++;
            }
        }
    }

    /// <summary>
    /// Uploads all mesh instances to GPU.
    /// </summary>
    protected async Task UploadSceneMeshesAsync(CancellationToken cancellationToken)
    {
        foreach (var instance in Instances)
        {
            await instance.Mesh.UploadAsync(MeshUploader, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Throws if the render loop is running.
    /// </summary>
    protected abstract void ThrowIfRunning();
}
