using SmartHotelAI.Domain.Models;

namespace SmartHotelAI.Api.Services;

public class BookingService
{
    private static List<Booking> _bookings = new();
    private static int _idCounter = 1;

    public Booking Book(string name, string roomType, int nights)
    {
        var normalized = NormalizeRoomType(roomType);
        var nightly_rate = GetNightlyRate(normalized);
        
        var booking = new Booking
        {
            Id = _idCounter++,
            GuestName = name,
            RoomType = normalized,
            Nights = nights,
            TotalAmount = nights * nightly_rate,
            CreatedAt = DateTime.UtcNow
        };

        _bookings.Add(booking);
        return booking;
    }

    /// <summary>
    /// Normalizes room type input, handling common typos
    /// Returns original input if no clear match found
    /// </summary>
    private string NormalizeRoomType(string roomType)
    {
        var lower = roomType?.ToLower().Trim() ?? "";
        
        // Fuzzy matching - handle common typos
        return lower switch
        {
            // Standard variations
            "standard" or "std" or "normal" => "standard",
            // Deluxe variations
            "deluxe" or "delux" or "de luxe" => "deluxe",
            // Suite variations
            "suite" or "suit" or "suites" => "suite",
            // Premium variations
            "premium" or "prem" => "premium",
            // Presidential variations
            "presidential" or "pres" or "president" => "presidential",
            // Return as-is if no match (caller will validate)
            _ => lower
        };
    }

    /// <summary>
    /// Validates if room type is recognized after normalization
    /// </summary>
    public bool IsValidRoomType(string roomType)
    {
        var normalized = NormalizeRoomType(roomType);
        return new[] { "standard", "deluxe", "suite", "premium", "presidential" }.Contains(normalized);
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

    private decimal GetNightlyRate(string roomType)
    {
        return roomType switch
        {
            "standard" => 100m,
            "deluxe" => 150m,
            "suite" => 200m,
            "premium" => 250m,
            "presidential" => 500m,
            _ => 100m // Default rate
        };
    }
}
