using Microsoft.AspNetCore.Mvc.RazorPages;
using Velvet.Core.Animation;
using Velvet.Core.Assets.Gltf;
using Velvet.Core.Math;
using Velvet.Core.Rendering.Cameras;
using Velvet.Core.Rendering.Controllers;
using Velvet.Graphics.WebGL;
using Velvet.Hosting.Web.Razor.Scene;

namespace Velvet.MvcSite.Pages;

public class VelvetTestModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    private readonly RazorVelvetSceneRuntime _velvet;

    public VelvetTestModel(IWebHostEnvironment env, RazorVelvetSceneRuntime velvet)
    {
        _env = env;
        _velvet = velvet;
    }

    public async Task OnGetAsync()
    {
        await _velvet.StartScene("velvet-canvas", async builder =>
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
                    // Match the Blazor demo behavior: prefer the second clip when available.
                    selected = result.Animations.Count > 1 ? result.Animations[1] : result.Animations[0];
                }

                animator.PlayClip(selected);
            }

            var camera = new Camera(
                new Vector3(0, 2, 6),
                Vector3.Zero,
                Vector3.UnitY,
                MathF.PI / 3,
                16f / 9f,
                0.1f,
                100f);

            host.Add(scene);
            host.Camera = camera;

            var orbit = new OrbitController(
                target: Vector3.Zero,
                yaw: 0f,
                pitch: 0.3f,
                distance: 6f,
                minDistance: 2f,
                maxDistance: 20f);
            host.SetController(orbit);

            builder.OnFrame(dt =>
            {
                animator.Update(dt);
                host.Render(scene);
                return Task.CompletedTask;
            });
        }).ConfigureAwait(false);
    }
}
