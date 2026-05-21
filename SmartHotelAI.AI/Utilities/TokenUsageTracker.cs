using Microsoft.SemanticKernel;

namespace SmartHotelAI.AI.Utilities;

/// <summary>
/// Extracts and tracks token usage from Semantic Kernel responses
/// 
/// Challenge: Semantic Kernel's auto-function calling makes multiple HTTP requests,
/// but only exposes the last roundtrip's token usage
/// 
/// Solution: Multi-attempt fallback extraction to handle SDK version changes
/// </summary>
public class TokenUsageTracker
{
    public class TokenUsage
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens => InputTokens + OutputTokens;
    }

    /// <summary>
    /// Extracts token usage with robust multi-attempt fallback
    /// Attempts 1-4 handle SDK variations, Attempt 5 is JSON parsing fallback
    /// </summary>
    public static TokenUsage ExtractTokenUsage(FunctionResult result)
    {
        try
        {
            if (result?.Metadata == null)
                return new TokenUsage();

            // Attempt 1: Direct metadata access using non-generic IDictionary
            if (result.Metadata is System.Collections.IDictionary metadata)
            {
                if (metadata.Contains("Usage"))
                {
                    var usage = metadata["Usage"];
                    if (usage is Dictionary<string, int> usageDict)
                    {
                        return new TokenUsage
                        {
                            InputTokens = usageDict.TryGetValue("input_tokens", out var input) ? input : 0,
                            OutputTokens = usageDict.TryGetValue("output_tokens", out var output) ? output : 0
                        };
                    }
                }
            }

            // Attempt 2: Try accessing as generic IDictionary<string, object>
            if (result.Metadata is System.Collections.Generic.IDictionary<string, object> objDict)
            {
                if (objDict.TryGetValue("Usage", out var usageObj))
                {
                    var usageStr = usageObj.ToString();
                    if (ParseTokensFromJson(usageStr, out var parsed))
                        return parsed;
                }
            }

            // Attempt 3: Check InnerContent (ChatCompletion) property
            var innerContent = result.GetType().GetProperty("InnerContent")?.GetValue(result);
            if (innerContent != null)
            {
                var usage = innerContent.GetType().GetProperty("Usage")?.GetValue(innerContent);
                if (usage != null && ParseTokensFromObject(usage, out var parsed))
                    return parsed;
            }

            // Attempt 4: Recursive property search through object graph
            foreach (var prop in result.GetType().GetProperties())
            {
                if (prop.Name.Contains("usage", StringComparison.OrdinalIgnoreCase))
                {
                    var val = prop.GetValue(result);
                    if (val != null && ParseTokensFromObject(val, out var parsed))
                        return parsed;
                }
            }

            // Attempt 5: JSON string parsing fallback
            var metadataStr = result.Metadata?.ToString() ?? "";
            if (ParseTokensFromJson(metadataStr, out var jsonParsed))
                return jsonParsed;

            // Fallback
            return new TokenUsage { InputTokens = 0, OutputTokens = 0 };
        }
        catch
        {
            return new TokenUsage { InputTokens = 0, OutputTokens = 0 };
        }
    }

    private static bool ParseTokensFromObject(object obj, out TokenUsage usage)
    {
        usage = new TokenUsage();
        try
        {
            var type = obj.GetType();
            var inputProp = type.GetProperty("InputTokens") ?? type.GetProperty("input_tokens");
            var outputProp = type.GetProperty("OutputTokens") ?? type.GetProperty("output_tokens");

            if (inputProp?.GetValue(obj) is int input && outputProp?.GetValue(obj) is int output)
            {
                usage.InputTokens = input;
                usage.OutputTokens = output;
                return true;
            }
        }
        catch { }

        return false;
    }

    private static bool ParseTokensFromJson(string json, out TokenUsage usage)
    {
        usage = new TokenUsage();
        try
        {
            if (json.Contains("\"input_tokens\"") && json.Contains("\"output_tokens\""))
            {
                var inputMatch = System.Text.RegularExpressions.Regex.Match(json, @"""input_tokens""\s*:\s*(\d+)");
                var outputMatch = System.Text.RegularExpressions.Regex.Match(json, @"""output_tokens""\s*:\s*(\d+)");

                if (inputMatch.Success && outputMatch.Success)
                {
                    usage.InputTokens = int.Parse(inputMatch.Groups[1].Value);
                    usage.OutputTokens = int.Parse(outputMatch.Groups[1].Value);
                    return true;
                }
            }
        }
        catch { }

        return false;
    }
}