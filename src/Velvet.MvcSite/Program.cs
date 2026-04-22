using Velvet.Core.Animation;
using Velvet.Core.Assets.Gltf;
using Velvet.Core.Math;
using Velvet.Core.Rendering.Cameras;
using Velvet.Core.Rendering.Controllers;
using Velvet.Graphics.WebGL;
using Velvet.Hosting.Web;
using Velvet.Hosting.Web.MvcRuntime;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddMvcVelvetHost(options =>
{
    MvcVelvetHost? host = null;
    Animator? animator = null;
    Velvet.Core.Scene.Scene? scene = null;

    options.Configure(async context =>
    {
        if (host is not null)
        {
            return;
        }

        var env = context.Services.GetRequiredService<IWebHostEnvironment>();
        JsBridge.Configure(context.Bridge);

        host = await MvcVelvetHost.CreateAsync(
            context.CanvasId,
            context.JsRuntime,
            ShaderProgram.CreateSkinnedAsync,
            context.Bridge).ConfigureAwait(false);

        var foxPath = Path.Combine(env.WebRootPath, "models", "Fox.glb");
        var bytes = await File.ReadAllBytesAsync(foxPath).ConfigureAwait(false);
        var result = await GltfLoader.LoadSceneWithAnimations(bytes, "models").ConfigureAwait(false);

        scene = result.Scene;
        animator = new Animator(scene);

        if (result.Animations.Count > 0)
        {
            animator.PlayClip(result.Animations[0]);
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

        await host.StartAsync(dt =>
        {
            animator?.Update(dt);
            if (scene is not null)
            {
                host.Render(scene);
            }

            return Task.CompletedTask;
        }).ConfigureAwait(false);
    });
});

var app = builder.Build();
app.UseMvcVelvetRuntime();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapBlazorHub();

app.Run();
