using Microsoft.AspNetCore.Mvc.RazorPages;
using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering.Cameras;
using Velvet.Core.Rendering.Controllers;
using Velvet.Core.Rendering.Meshes;
using Velvet.Core.Scene;
using Velvet.Graphics.WebGL;
using Velvet.Hosting.Web.Razor.Scene;

namespace Velvet.MvcSite.Pages;

public class CubeModel : PageModel
{
    private readonly RazorVelvetSceneRuntime _velvet;

    public CubeModel(RazorVelvetSceneRuntime velvet)
    {
        _velvet = velvet;
    }

    public async Task OnGetAsync()
    {
        await _velvet.StartScene("velvet-cube-canvas", async builder =>
        {
            var host = await builder.CreateHostAsync(ShaderProgram.CreateDefaultAsync).ConfigureAwait(false);

            var cubeGeometry = new CubeGeometry();
            var cubeMesh = new Mesh(cubeGeometry);
            var cubeNode = new SceneNode(
                localTransform: Matrix.Trs(
                    translation: Vector3.Zero,
                    rotation: Quaternion.Identity,
                    scale: new Vector3(1f, 1f, 1f)),
                meshes: new List<Mesh> { cubeMesh },
                children: new List<SceneNode>(),
                name: "Cube");

            var scene = new Scene(new List<SceneNode> { cubeNode });

            var camera = new Camera(
                new Vector3(0f, 1.5f, 4f),
                Vector3.Zero,
                Vector3.UnitY,
                MathF.PI / 3,
                16f / 9f,
                0.1f,
                100f);

            host.Add(scene);
            host.Camera = camera;
            host.SetController(new OrbitController(
                target: Vector3.Zero,
                yaw: 0f,
                pitch: 0.3f,
                distance: 4f,
                minDistance: 2f,
                maxDistance: 12f));

            builder.OnFrame(_ =>
            {
                host.Render(scene);
                return Task.CompletedTask;
            });
        }).ConfigureAwait(false);
    }
}
