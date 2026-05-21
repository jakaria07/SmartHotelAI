// 🆕 NEW: Load environment variables from .env file
using dotenv.net;
using Microsoft.SemanticKernel;
using SmartHotelAI.Api.Services;
using SmartHotelAI.AI.Kernel;
using SmartHotelAI.AI.Orchestrator;

DotEnv.Load();

// ========== DEPENDENCY INJECTION SETUP ==========

// Initialize services (EXISTING CODE)
var bookingService = new BookingService();
var fraudService = new FraudService(bookingService);

// 🆕 NEW: Load API credentials from .env file
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var modelId = Environment.GetEnvironmentVariable("OPENAI_MODEL_ID") ?? "gpt-4o-mini";

if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("❌ ERROR: OPENAI_API_KEY not found in .env file!");
    Console.WriteLine("📝 Please create a .env file in the project root with:");
    Console.WriteLine("   OPENAI_API_KEY=sk-...");
    Environment.Exit(1);
}

// 🔄 UPDATED: Use credentials from .env
var kernelBuilder = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(modelId, apiKey);

var classificationKernel = kernelBuilder.Build();

// Create factory for scoped kernels (EXISTING CODE)
// 🔄 UPDATED: Pass services to factory so it can inject them into plugins
var kernelFactory = new KernelFactory(kernelBuilder, bookingService, fraudService);

// Inject dependencies into orchestrator (EXISTING CODE)
var orchestrator = new HotelAssistantOrchestrator(
    classificationKernel,
    kernelFactory);

// ========== UI LOOP (EXISTING CODE) ==========

try { Console.Clear(); } catch { }
Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
Console.WriteLine("║   🏨 SMART HOTEL AI ASSISTANT (Semantic Kernel) 🤖   ║");
Console.WriteLine("║                                                       ║");
Console.WriteLine("║   ✨ Enterprise-Grade AI Integration                 ║");
Console.WriteLine("║   🚀 Programmatic Routing                            ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════╝\n");

Console.WriteLine("🎯 **What I can help with:**");
Console.WriteLine("   📅 Book a new hotel room (Standard $100, Deluxe $150, Suite $250, Premium $300, Presidential $500/night)");
Console.WriteLine("   🔍 Check if a booking is suspicious");
Console.WriteLine("   ❌ Cancel an existing booking");
Console.WriteLine("   📋 View booking history");
Console.WriteLine("   🚪 Type 'exit' to quit\n");

// Main interactive loop (EXISTING CODE)
while (true)
{
    Console.Write("👉 You: ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("\n👋 Thank you for using SmartHotel AI!");
        Console.WriteLine(orchestrator.GetTokenUsageSummary());
        break;
    }

    // Process with orchestrator (EXISTING CODE)
    try
    {
        var result = await orchestrator.HandleAsync(input);
        Console.WriteLine($"\n🤖 Assistant:\n{result}");
        
        // 🔄 Display per-query token usage
        Console.WriteLine($"\n{orchestrator.GetLastQueryTokenInfo()}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n❌ Error: {ex.Message}\n");
    }
}
