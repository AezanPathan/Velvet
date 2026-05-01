using Velvet.Hosting.Web.Razor.Setup;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorPages();
builder.Services.AddRazorVelvetHost(options =>
{
    // Optional global fallback. Page-level scenes should be registered via RazorVelvetSceneRuntime.
    options.Configure(_ => Task.CompletedTask);
});

// Provide HttpClient for components that inject it (server prerendering)
builder.Services.AddHttpClient();
builder.Services.AddScoped(sp => sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient());

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
