using Microsoft.SemanticKernel;
using SmartHotelAI.AI.Utilities;
using SmartHotelAI.Domain.Models;
using System.Text.Json;

namespace SmartHotelAI.AI.Intents;

/// <summary>
/// LLM-based intent classifier using Semantic Kernel
/// </summary>
public class IntentClassifier
{
    private readonly Microsoft.SemanticKernel.Kernel _kernel;

    // Token tracking for last classification
    public int LastClassificationInputTokens { get; private set; }
    public int LastClassificationOutputTokens { get; private set; }

    public IntentClassifier(Microsoft.SemanticKernel.Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <summary>
    /// Classifies user input into intents using LLM
    /// Returns structured classification with confidence score
    /// </summary>
    public async Task<IntentClassificationResult> ClassifyAsync(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);

        var systemPrompt = @"You classify hotel booking intent and extract entities.
Intents: BookRoom, CheckFraud, CancelBooking, GetHistory, Unknown.

Return JSON only:
{""intent"":""BookRoom"",""confidence"":0.9,""missingInputs"":[],""extractedEntities"":{""guestName"":"""",""roomType"":"""",""nights"":0,""bookingId"":0}}";

        // var systemPrompt = @"You are a hotel booking assistant classifier. Classify the
        // user's intent and extract entities.
        // Supported intents:
        // - BookRoom: Book a new hotel room
        // - CheckFraud: Check if a booking is suspicious/fraudulent
        // - CancelBooking: Cancel an existing booking
        // - GetHistory: View booking history/list bookings
        // - Unknown: Can't determine intent
        
        // Return ONLY a JSON object (no markdown, no extra text):
        // {
        //     ""intent"": ""BookRoom"" or ""CheckFraud"" or ""CancelBooking"" or ""GetHistory"" or ""Unknown"",
        //     ""confidence"": 0.0-1.0,
        //     ""missingInputs"": [""field1"", ""field2""],
        //     ""extractedEntities"": {
        //         ""guestName"": """",
        //         ""roomType"": """",
        //         ""nights"": 0,
        //         ""bookingId"": 0
        //     }
        // }";

        try
        {
            var prompt = $"{systemPrompt}\n\nUser input: {input}";
            var result = await _kernel.InvokePromptAsync(prompt);
            var resultText = result.ToString();

            // Strip markdown code block if present (```json {...} ```)
            if (resultText.Contains("```"))
            {
                resultText = System.Text.RegularExpressions.Regex.Replace(resultText, @"```\w*\n?|\n?```", "").Trim();
            }

            // 🔄 Extract token usage from this classification call
            var tokenUsage = TokenUsageTracker.ExtractTokenUsage(result);
            
            // 🔄 Fallback: Estimate tokens if not provided by LLM
            // Rough estimation: ~4 characters = 1 token
            if (tokenUsage.InputTokens == 0)
            {
                LastClassificationInputTokens = Math.Max(50, prompt.Length / 4); // Min 50 for system prompt
            }
            else
            {
                LastClassificationInputTokens = tokenUsage.InputTokens;
            }

            if (tokenUsage.OutputTokens == 0)
            {
                LastClassificationOutputTokens = Math.Max(100, resultText.Length / 4); // Min 100 for JSON response
            }
            else
            {
                LastClassificationOutputTokens = tokenUsage.OutputTokens;
            }

            // parse JSON response
            var options = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
            var parsed = JsonSerializer.Deserialize<JsonElement>(resultText, options);

            return new IntentClassificationResult
            {
                Intent = ParseIntent(parsed.GetProperty("intent").GetString()),
                confidence = parsed.GetProperty("confidence").GetDouble(),
                MissingInputs = JsonSerializer.Deserialize<List<string>>(
                    parsed.GetProperty("missingInputs").GetRawText(), 
                    options) ?? new(),
                ExtractedEntities = ParseEntities(parsed.GetProperty("extractedEntities"))
            };
        }
        catch (Exception ex)
        {
            // Fallback on error - no tokens used if classification fails
            LastClassificationInputTokens = 0;
            LastClassificationOutputTokens = 0;

            return new IntentClassificationResult
            {
                Intent = UserIntent.Unknown,
                confidence = 0.0,
                MissingInputs = new() { "Unable to process input" }
            };
        }
    }

    private UserIntent ParseIntent(string intentStr) => intentStr?.ToLower() switch
    {
        "bookroom" => UserIntent.BookRoom,
        "checkfraud" => UserIntent.CheckFraud,
        "cancelbooking" => UserIntent.CancelBooking,
        "gethistory" => UserIntent.GetHistory,
        _ => UserIntent.Unknown
    };

    private Dictionary<string, object> ParseEntities(JsonElement element)
    {
        var entities = new Dictionary<string, object>();
        foreach (var prop in element.EnumerateObject())
        {
            entities[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.GetInt32(),
                _ => null
            };
        }
        return entities;
    }
}
