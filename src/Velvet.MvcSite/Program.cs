using Velvet.Hosting.Web.MvcRuntime;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorPages();
builder.Services.AddMvcVelvetHost(options =>
{
    // Optional global fallback. Page-level scenes should be registered via MvcVelvetSceneRuntime.
    options.Configure(_ => Task.CompletedTask);
});

var app = builder.Build();
app.UseMvcVelvetRuntime();

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
