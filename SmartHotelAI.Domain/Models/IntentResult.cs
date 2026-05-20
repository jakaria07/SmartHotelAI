namespace SmartHotelAI.Domain.Models;

public class IntentResult
{
    public string Intent { get; set; }
    public double Confidence { get; set; }
    public string Name { get; set; }
    public string RoomType { get; set; }
    public int Nights { get; set; }
    public int BookingId { get; set; }
}