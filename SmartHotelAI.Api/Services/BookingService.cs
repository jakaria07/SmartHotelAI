using SmartHotelAI.Domain.Models;

namespace SmartHotelAI.Api.Services;

public class BookingService
{
    private static List<Booking> _bookings = new();
    private static int _idCounter = 1;

    public Booking Book(string name, string roomType, int nights)
    {
        var booking = new Booking
        {
            Id = _idCounter++,
            GuestName = name,
            RoomType = roomType,
            Nights = nights,
            TotalAmount = nights * 100, // Simple pricing: $100 per night
            CreatedAt = DateTime.UtcNow
        };

        _bookings.Add(booking);
        return booking;
    }

    public List<Booking> GetAll() => _bookings;

    public Booking GetById(int id) => _bookings.FirstOrDefault(b => b.Id == id);

    public bool Cancel(int id)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == id);
        if (booking == null) return false;

        booking.Status = "Cancelled";
        return true;
    }
}