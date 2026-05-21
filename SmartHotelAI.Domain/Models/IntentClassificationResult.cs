namespace SmartHotelAI.Domain.Models;

/// <summary>
/// Complete result from intent classification by LLM
/// Includes intent, confidence, missing inputs, and extracted entities
/// </summary>


public class IntentClassificationResult
{
    /// <summary>
    /// The detected intent (enum, strongly-typed)
    /// </summary>
    public UserIntent Intent { get; set; }

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// Used to determine if LLM was confident about the classification
    /// </summary>
    public double confidence { get; set; }

    /// <summary>
    /// List of missing required inputs
    /// Used for specific clarification requests
    /// Example: ["room_type", "check_in_date"]
    /// </summary>
    public List<string> MissingInputs { get; set; } = new();

    /// <summary>
    /// Extracted entities from user input
    /// Example: { "guestName": "John", "nights": 3, "roomType": "Deluxe" }
    /// </summary>
    public Dictionary<string, object> ExtractedEntities { get; set; } = new();
}