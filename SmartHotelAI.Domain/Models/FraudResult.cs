namespace SmartHotelAI.Domain.Models;

public class FraudResult
{
    public bool IsSuspicious { get; set; }
    public string RiskLevel { get; set; }
    public string Reason { get; set; }
}