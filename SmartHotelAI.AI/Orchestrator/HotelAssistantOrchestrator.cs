using Microsoft.SemanticKernel;
using SmartHotelAI.AI.Intents;
using SmartHotelAI.AI.Kernel;
using SmartHotelAI.AI.Utilities;
using SmartHotelAI.Domain.Models;

namespace SmartHotelAI.AI.Orchestrator;

/// <summary>
/// Central orchestrator coordinating the entire request lifecycle
/// 
/// Responsibilities:
/// 1. Intent classification
/// 2. Confidence validation
/// 3. Missing input detection
/// 4. Scoped kernel creation
/// 5. Programmatic function routing
/// 6. Response aggregation
/// 7. Token tracking
/// </summary>
public class HotelAssistantOrchestrator
{
    private readonly Microsoft.SemanticKernel.Kernel _classificationKernel;  // Lightweight kernel for classification only
    private readonly KernelFactory _kernelFactory;
    private readonly IntentClassifier _classifier;
    private const double ConfidenceThreshold = 0.65;  // Require 65%+ confidence
    private bool _useProgrammaticRouting = true;      // Direct function invocation

    // Token tracking
    public int TotalInputTokens { get; private set; }
    public int TotalOutputTokens { get; private set; }
    public int TotalTokens => TotalInputTokens + TotalOutputTokens;

    // Per-query token tracking
    public int LastQueryInputTokens { get; private set; }
    public int LastQueryOutputTokens { get; private set; }
    public int LastQueryTotalTokens => LastQueryInputTokens + LastQueryOutputTokens;

    public HotelAssistantOrchestrator(
        Microsoft.SemanticKernel.Kernel classificationKernel,
        KernelFactory kernelFactory)
    {
        _classificationKernel = classificationKernel;
        _kernelFactory = kernelFactory;
        _classifier = new IntentClassifier(_classificationKernel);
    }

    /// <summary>
    /// Main handler: Accept user input and return response
    /// </summary>
    public async Task<string> HandleAsync(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        // 🔄 Reset per-query token tracking
        LastQueryInputTokens = 0;
        LastQueryOutputTokens = 0;

        // ========== PHASE 1: INTENT CLASSIFICATION ==========
        var classificationResult = await _classifier.ClassifyAsync(input);

        // 🔄 Capture tokens used in classification and add to totals
        LastQueryInputTokens += _classifier.LastClassificationInputTokens;
        LastQueryOutputTokens += _classifier.LastClassificationOutputTokens;
        TotalInputTokens += _classifier.LastClassificationInputTokens;
        TotalOutputTokens += _classifier.LastClassificationOutputTokens;

        // ========== PHASE 2: CONFIDENCE VALIDATION ==========
        if (classificationResult.confidence < ConfidenceThreshold)
        {
            return GenerateClarificationRequest(classificationResult);
        }

        // ========== PHASE 3: MISSING INPUTS CHECK ==========
        if (classificationResult.MissingInputs.Any())
        {
            return GenerateMissingInputsRequest(classificationResult.MissingInputs);
        }

        // ========== PHASE 4: SCOPED KERNEL CREATION ==========
        var executionKernel = _kernelFactory.CreateScopedKernel(classificationResult.Intent);

        // ========== PHASE 5: PROGRAMMATIC EXECUTION ==========
        if (_useProgrammaticRouting)
        {
            return await ExecuteProgrammaticallyAsync(executionKernel, classificationResult);
        }
        else
        {
            // Fallback to LLM-based execution
            return await ExecuteWithLLMAsync(executionKernel, classificationResult, input);
        }
    }

    /// <summary>
    /// Direct function invocation (bypasses LLM)
    /// 
    /// Token Optimization:
    /// - No LLM execution roundtrip = 0 tokens
    /// - Saves ~400-450 tokens per request
    /// </summary>
    private async Task<string> ExecuteProgrammaticallyAsync(
        Microsoft.SemanticKernel.Kernel kernel,
        IntentClassificationResult classification)
    {
        try
        {
            string functionName = classification.Intent switch
            {
                UserIntent.BookRoom => "book_room",
                UserIntent.CheckFraud => "check_fraud",
                UserIntent.CancelBooking => "cancel_booking",
                UserIntent.GetHistory => "get_booking_history",
                _ => null
            };

            if (functionName == null)
                return "❓ Unable to determine operation.";

            // Extract arguments from entities
            var args = new KernelArguments();

            if (classification.ExtractedEntities.TryGetValue("guestName", out var guestName))
                args["guestName"] = guestName;

            if (classification.ExtractedEntities.TryGetValue("roomType", out var roomType))
                args["roomType"] = roomType;

            if (classification.ExtractedEntities.TryGetValue("nights", out var nights))
                args["nights"] = nights;

            if (classification.ExtractedEntities.TryGetValue("bookingId", out var bookingId))
                args["bookingId"] = bookingId;

            // Direct invocation
            var function = kernel.Plugins.SelectMany(p => p).FirstOrDefault(f => f.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
            if (function == null)
                return $"❌ Function '{functionName}' not found.";

            var result = await kernel.InvokeAsync(function, args);

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"❌ Execution failed: {ex.Message}";
        }
    }

    /// <summary>
    /// LLM-based execution (traditional approach - NOT recommended)
    /// Uses LLM to select function, not programmatic routing
    /// </summary>
    private async Task<string> ExecuteWithLLMAsync(
        Microsoft.SemanticKernel.Kernel kernel,
        IntentClassificationResult classification,
        string input)
    {
        try
        {
            // Build function selection prompt
            var prompt = $"Execute this hotel booking request: {input}";
            var result = await kernel.InvokePromptAsync(prompt);

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"❌ LLM execution failed: {ex.Message}";
        }
    }

    private string GenerateClarificationRequest(IntentClassificationResult result)
    {
        return $@"❓ **I'm not quite sure what you mean.**

Confidence: {(result.confidence * 100):F0}% (Need > 65%)

**What I can help with:**
- 📅 Book a new hotel room
- 🔍 Check if a booking is suspicious
- ❌ Cancel an existing booking
- 📋 View booking history

**Please clarify your request.**";
    }

    private string GenerateMissingInputsRequest(List<string> missingInputs)
    {
        return $@"❓ **I need more information to help you.**

**Missing details:**
- {string.Join("\n- ", missingInputs.Select(m => m))}

**Please provide the missing information.**";
    }

    /// <summary>
    /// Get token usage summary for this session
    /// </summary>
    public string GetTokenUsageSummary() =>
        $@"📊 **Token Usage Summary**
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Input Tokens: {TotalInputTokens}
Output Tokens: {TotalOutputTokens}
Total Tokens: {TotalTokens}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";

    /// <summary>
    /// Get token usage for the last query
    /// </summary>
    public string GetLastQueryTokenInfo() =>
        $"⚡ Tokens used: {LastQueryTotalTokens} (Input: {LastQueryInputTokens}, Output: {LastQueryOutputTokens})";
}