using SmartHotelAI.Domain.Models;

namespace SmartHotelAI.Api.Services;

public class FraudService
{
    private readonly BookingService _bookingService;
    private static List<int> _blacklist = new(); // Blocked booking IDs

    public FraudService(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    public FraudResult Check(int bookingId)
    {
        var booking = _bookingService.GetById(bookingId);

        if (booking == null)
        {
            return new FraudResult
            {
                IsSuspicious = true,
                RiskLevel = "None",
                Reason = "Booking not found"
            };
        }

        // ========== RULE 0: BLACKLIST CHECK (HIGHEST PRIORITY) ==========
        // Check if booking is explicitly blacklisted
        if (_blacklist.Contains(bookingId))
        {
            return new FraudResult
            {
                IsSuspicious = true,
                RiskLevel = "High",
                Reason = "Booking ID is blacklisted"
            };
        }

        // ========== RULE 1: RAPID BOOKINGS DETECTION ==========
        // Detect if THIS SPECIFIC GUEST made multiple bookings in short time (potential bot/mass booking)
        // Per-guest check: Only flag if THIS guest has >3 bookings in 5 minutes
        var guestRecentBookings = _bookingService.GetAll()
            .Where(b => b.GuestName == booking.GuestName &&
                        b.CreatedAt > DateTime.UtcNow.AddMinutes(-5))
            .Count();
        
        if(guestRecentBookings > 3)
        {
            return new FraudResult
            {
                IsSuspicious = true,
                RiskLevel = "High",
                Reason = "This guest made multiple bookings in short time (> 3 in 5 minutes)"
            };
        }

        // ========== RULE 2: FREQUENT CANCELLATIONS ==========
        // Detect book→cancel→book pattern (manipulation attempt)
        var cancelledBookingsCount = _bookingService.GetAll()
            .Where(b => b.GuestName == booking.GuestName &&
                        b.Status == "Cancelled" &&
                        b.CreatedAt > DateTime.UtcNow.AddMinutes(-10))
            .Count();

        if (cancelledBookingsCount >= 3)
        {
            return new FraudResult
            {
                IsSuspicious = true,
                RiskLevel = "Medium",
                Reason = "Frequent cancellations detected (> 2 in 10 minutes)"
            };
        }

        // ========== RULE 3: HIGH BOOKING VALUE ==========
        // Flag unusually high-value transactions
        if (booking.TotalAmount > 5000)
        {
            return new FraudResult
            {
                IsSuspicious = true,
                RiskLevel = "Medium",
                Reason = "High booking value (> $5000)"
            };
        }

        // ========== RULE 4: GUEST BEHAVIOR PATTERN ==========
        // Enhanced: Now includes TIME WINDOW - checks only recent bookings
        // Detect guests making multiple high-value bookings in short period
        var recentGuestBookings = _bookingService.GetAll()
            .Where(b => b.GuestName == booking.GuestName &&
                        b.CreatedAt > DateTime.UtcNow.AddMinutes(-10))
            .ToList();

        if (recentGuestBookings.Count > 3 &&
            recentGuestBookings.Sum(b => b.TotalAmount) > 5000)
        {
            return new FraudResult
            {
                IsSuspicious = true,
                RiskLevel = "High",
                Reason = "Multiple high-value bookings by same guest in short time"
            };
        }

        // ========== DEFAULT: SAFE ==========
        return new FraudResult
        {
            IsSuspicious = false,
            RiskLevel = "Low",
            Reason = "Normal booking behavior"
        };
    }

    /// <summary>
    /// Add a booking to the blacklist for manual review or ban
    /// </summary>
    public void AddToBlacklist(int bookingId)
    {
        if (!_blacklist.Contains(bookingId))
            _blacklist.Add(bookingId);
    }

    /// <summary>
    /// Get current blacklist (for audit purposes)
    /// </summary>
    public List<int> GetBlacklist() => new List<int>(_blacklist);
}