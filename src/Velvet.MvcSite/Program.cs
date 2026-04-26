using Velvet.Hosting.Web.Razor.Runtime;
using Velvet.Hosting.Web.Razor.Setup;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorPages();
builder.Services.AddRazorVelvetHost(options =>
{
    // Optional global fallback. Page-level scenes should be registered via RazorVelvetSceneRuntime.
    options.Configure(_ => Task.CompletedTask);
});

var app = builder.Build();
app.UseRazorVelvetRuntime();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
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
