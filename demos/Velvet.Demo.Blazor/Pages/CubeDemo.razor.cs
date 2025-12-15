using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Velvet.WebGL;

namespace Velvet.Demo.Blazor.Pages;

public partial class CubeDemo : ComponentBase
{
    private ElementReference canvasRef;
    private BlazorWebGLBridge? bridge;
    
    // Velvet resource IDs
    private int rendererId = -1;
    private int vertexShaderId = -1;
    private int fragmentShaderId = -1;
    private int programId = -1;
    private int meshId = -1;

    // State
    private bool isInitialized = false;
    private bool isAnimating = false;
    private string? statusMessage = "Ready to initialize";
    private string? errorMessage = null;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            bridge = new BlazorWebGLBridge(JS);
            StateHasChanged();
        }
    }

    private async Task InitializeDemo()
    {
        try
        {
            errorMessage = null;
            statusMessage = "Initializing Velvet WebGL renderer...";
            StateHasChanged();

            if (bridge == null)
            {
                throw new InvalidOperationException("Bridge not initialized");
            }

            // Step 1: Initialize renderer with canvas ElementReference
            rendererId = await bridge.InitWithElementAsync(canvasRef);
            Console.WriteLine($"✅ Renderer initialized: ID = {rendererId}");

            // Step 2: Load shader source code
            statusMessage = "Loading shaders...";
            StateHasChanged();

            string vertexSource = await JS.InvokeAsync<string>("loadShaderSource", "shaders/simple.vert");
            string fragmentSource = await JS.InvokeAsync<string>("loadShaderSource", "shaders/simple.frag");
            
            Console.WriteLine($"✅ Shaders loaded: {vertexSource.Length + fragmentSource.Length} chars total");

            // Step 3: Create and compile shaders using VelvetAPI
            statusMessage = "Compiling shaders...";
            StateHasChanged();

            vertexShaderId = await JS.InvokeAsync<int>("Velvet.createShader", vertexSource, "vertex");
            fragmentShaderId = await JS.InvokeAsync<int>("Velvet.createShader", fragmentSource, "fragment");
            
            Console.WriteLine($"✅ Shaders compiled: Vertex={vertexShaderId}, Fragment={fragmentShaderId}");

            // Step 4: Create GPU program
            statusMessage = "Creating GPU program...";
            StateHasChanged();

            programId = await JS.InvokeAsync<int>("Velvet.createProgram");
            Console.WriteLine($"✅ Program created: ID = {programId}");

            // Step 5: Attach shaders to program
            statusMessage = "Attaching shaders...";
            StateHasChanged();

            await JS.InvokeVoidAsync("Velvet.attachShader", programId, vertexShaderId);
            await JS.InvokeVoidAsync("Velvet.attachShader", programId, fragmentShaderId);
            Console.WriteLine($"✅ Shaders attached to program");

            // Step 6: Link program
            statusMessage = "Linking program...";
            StateHasChanged();

            await JS.InvokeVoidAsync("Velvet.linkProgram", programId);
            Console.WriteLine($"✅ Program linked successfully");

            // Step 7: Create cube mesh geometry
            statusMessage = "Creating cube mesh...";
            StateHasChanged();

            float[] cubeVertices = GetCubeVertices();
            meshId = await JS.InvokeAsync<int>("Velvet.createMesh", cubeVertices);
            
            Console.WriteLine($"✅ Mesh created: ID = {meshId}, vertices = {cubeVertices.Length / 6} ({cubeVertices.Length} floats)");

            // Complete!
            isInitialized = true;
            statusMessage = "✅ Initialization complete! Ready to animate.";
            StateHasChanged();

            Console.WriteLine("🎉 Demo initialization successful!");
        }
        catch (Exception ex)
        {
            errorMessage = $"Initialization failed: {ex.Message}";
            statusMessage = null;
            isInitialized = false;
            Console.Error.WriteLine($"❌ Error during initialization: {ex}");
            StateHasChanged();
        }
    }

    private async Task StartAnimation()
    {
        try
        {
            errorMessage = null;
            statusMessage = "Starting animation loop...";
            StateHasChanged();

            // Call JavaScript animation function with resource IDs
            await JS.InvokeVoidAsync(
                "startCubeAnimation",
                rendererId,
                programId,
                meshId,
                800,  // canvas width
                600   // canvas height
            );
            
            isAnimating = true;
            statusMessage = "🔄 Animation running!";
            StateHasChanged();

            Console.WriteLine("🎬 Animation started!");
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to start animation: {ex.Message}";
            statusMessage = null;
            Console.Error.WriteLine($"❌ Error starting animation: {ex}");
            StateHasChanged();
        }
    }

    private async Task StopAnimation()
    {
        try
        {
            await JS.InvokeVoidAsync("stopCubeAnimation");
            
            isAnimating = false;
            statusMessage = "⏸️ Animation stopped.";
            StateHasChanged();

            Console.WriteLine("⏸️ Animation stopped!");
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to stop animation: {ex.Message}";
            Console.Error.WriteLine($"❌ Error stopping animation: {ex}");
            StateHasChanged();
        }
    }

    /// <summary>
    /// Generate cube vertices with positions and colors.
    /// Format: [x, y, z, r, g, b] per vertex
    /// </summary>
    private float[] GetCubeVertices()
    {
        return new float[]
        {
            // Front face (RED)
            -1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 0.0f,
             1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 0.0f,
             1.0f,  1.0f,  1.0f,  1.0f, 0.0f, 0.0f,
            -1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 0.0f,
             1.0f,  1.0f,  1.0f,  1.0f, 0.0f, 0.0f,
            -1.0f,  1.0f,  1.0f,  1.0f, 0.0f, 0.0f,

            // Back face (GREEN)
            -1.0f, -1.0f, -1.0f,  0.0f, 1.0f, 0.0f,
            -1.0f,  1.0f, -1.0f,  0.0f, 1.0f, 0.0f,
             1.0f,  1.0f, -1.0f,  0.0f, 1.0f, 0.0f,
            -1.0f, -1.0f, -1.0f,  0.0f, 1.0f, 0.0f,
             1.0f,  1.0f, -1.0f,  0.0f, 1.0f, 0.0f,
             1.0f, -1.0f, -1.0f,  0.0f, 1.0f, 0.0f,

            // Top face (BLUE)
            -1.0f,  1.0f, -1.0f,  0.0f, 0.0f, 1.0f,
            -1.0f,  1.0f,  1.0f,  0.0f, 0.0f, 1.0f,
             1.0f,  1.0f,  1.0f,  0.0f, 0.0f, 1.0f,
            -1.0f,  1.0f, -1.0f,  0.0f, 0.0f, 1.0f,
             1.0f,  1.0f,  1.0f,  0.0f, 0.0f, 1.0f,
             1.0f,  1.0f, -1.0f,  0.0f, 0.0f, 1.0f,

            // Bottom face (YELLOW)
            -1.0f, -1.0f, -1.0f,  1.0f, 1.0f, 0.0f,
             1.0f, -1.0f, -1.0f,  1.0f, 1.0f, 0.0f,
             1.0f, -1.0f,  1.0f,  1.0f, 1.0f, 0.0f,
            -1.0f, -1.0f, -1.0f,  1.0f, 1.0f, 0.0f,
             1.0f, -1.0f,  1.0f,  1.0f, 1.0f, 0.0f,
            -1.0f, -1.0f,  1.0f,  1.0f, 1.0f, 0.0f,

            // Right face (MAGENTA)
             1.0f, -1.0f, -1.0f,  1.0f, 0.0f, 1.0f,
             1.0f,  1.0f, -1.0f,  1.0f, 0.0f, 1.0f,
             1.0f,  1.0f,  1.0f,  1.0f, 0.0f, 1.0f,
             1.0f, -1.0f, -1.0f,  1.0f, 0.0f, 1.0f,
             1.0f,  1.0f,  1.0f,  1.0f, 0.0f, 1.0f,
             1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 1.0f,

            // Left face (CYAN)
            -1.0f, -1.0f, -1.0f,  0.0f, 1.0f, 1.0f,
            -1.0f, -1.0f,  1.0f,  0.0f, 1.0f, 1.0f,
            -1.0f,  1.0f,  1.0f,  0.0f, 1.0f, 1.0f,
            -1.0f, -1.0f, -1.0f,  0.0f, 1.0f, 1.0f,
            -1.0f,  1.0f,  1.0f,  0.0f, 1.0f, 1.0f,
            -1.0f,  1.0f, -1.0f,  0.0f, 1.0f, 1.0f
        };
    }
}
