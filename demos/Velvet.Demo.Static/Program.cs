using System.Threading.Tasks;
using Velvet.Core.Engine;
using Velvet.WebGL;

// Entry point for the plain WebAssembly demo (no Blazor).
public partial class Program
{
    public static async Task Main(string[] args)
    {
        var app = new VelvetApp();
        app.UseWebGL();
        app.Add(new DrawTriangle());
        await app.RunAsync();
    }
}
