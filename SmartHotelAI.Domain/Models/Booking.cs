namespace SmartHotelAI.Domain.Models;

public class Booking
{
    public int Id { get; set; }
    public string GuestName { get; set; }
    public string RoomType { get; set; }
    public DateTime CheckInDate { get; set; }
    public int Nights { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Confirmed";
    public DateTime CreatedAt { get; set; }
}