using Microsoft.AspNetCore.Mvc.RazorPages;
using Velvet.Core.Animation;
using Velvet.Core.Assets.Gltf;
using Velvet.Core.Math;
using Velvet.Core.Rendering.Cameras;
using Velvet.Core.Rendering.Controllers;
using Velvet.Core.Rendering.Lighting;
using Velvet.Graphics.WebGL;
using Velvet.Hosting.Web.Razor.Scene;

namespace Velvet.MvcSite.Pages;

public class Scene2Model : PageModel
{
    private readonly IWebHostEnvironment _env;
    private readonly RazorVelvetSceneRuntime _velvet;

    public Scene2Model(IWebHostEnvironment env, RazorVelvetSceneRuntime velvet)
    {
        _env = env;
        _velvet = velvet;
    }

    public async Task OnGetAsync()
    {
        await _velvet.StartScene("scene2-canvas", async builder =>
        {
            var host = await builder.CreateHostAsync(ShaderProgram.CreateSkinnedAsync).ConfigureAwait(false);

            var foxPath = Path.Combine(_env.WebRootPath, "models", "Fox.glb");
            var bytes = await System.IO.File.ReadAllBytesAsync(foxPath).ConfigureAwait(false);
            var result = await GltfLoader.LoadSceneWithAnimations(bytes, "models").ConfigureAwait(false);

            var scene = result.Scene;
            var animator = new Animator(scene);

            if (result.Animations.Count > 0)
            {
                var selected = result.Animations
                    .FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.Name) && a.Name.Contains("walk", StringComparison.OrdinalIgnoreCase));

                if (selected is null)
                {
                    selected = result.Animations.Count > 1 ? result.Animations[1] : result.Animations[0];
                }

                animator.PlayClip(selected);
            }

            var camera = new Camera(
                new Vector3(0f, 2f, 6f),
                Vector3.Zero,
                Vector3.UnitY,
                MathF.PI / 3,
                16f / 9f,
                0.1f,
                100f);

            var directional = new DirectionalLight(
                direction: new Vector3(0.4f, -1.0f, -0.25f),
                color: new Vector3(1, 1, 1),
                intensity: 1.1f);

            var point = new PointLight(
                position: new Vector3(1.5f, 1.1f, 1.6f),
                color: new Vector3(1.0f, 0.95f, 0.9f),
                intensity: 2.0f,
                constant: 1.0f,
                linear: 0.14f,
                quadratic: 0.07f);

            var spot = new SpotLight(
                position: new Vector3(0.0f, 2.2f, 2.2f),
                direction: new Vector3(0.0f, -1.0f, -1.0f),
                color: new Vector3(1.0f, 1.0f, 1.0f),
                intensity: 0.0f,
                cutoff: 12.0f * (MathF.PI / 180.0f),
                outerCutoff: 20.0f * (MathF.PI / 180.0f),
                constant: 1.0f,
                linear: 0.09f,
                quadratic: 0.032f);

            host.Add(scene);
            await host.SetCubemapSkyboxAsync(
                "skybox/px.png",
                "skybox/nx.png",
                "skybox/py.png",
                "skybox/ny.png",
                "skybox/pz.png",
                "skybox/nz.png").ConfigureAwait(false);

            var bounds = scene.ComputeBounds();
            camera.Frame(bounds, frameMultiplier: 1.3f);

            host.Camera = camera;
            host.DirectionalLight = directional;
            host.PointLight = point;
            host.SpotLight = spot;
            host.SetDirectionalEnabled(true);
            host.SetPointEnabled(true);

            var orbitController = new OrbitController(
                target: bounds.Center,
                yaw: 0f,
                pitch: 0.3f,
                distance: (bounds.Center - camera.Position).Length,
                minDistance: bounds.Radius * 0.5f,
                maxDistance: bounds.Radius * 10f);
            host.SetController(orbitController);

            builder.OnFrame(dt =>
            {
                animator.Update(dt);
                host.Render(scene);
                return Task.CompletedTask;
            });
        }).ConfigureAwait(false);
    }
}
