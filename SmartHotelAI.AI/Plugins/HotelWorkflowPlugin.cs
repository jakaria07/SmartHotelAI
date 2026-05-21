using Microsoft.SemanticKernel;
using SmartHotelAI.Api.Services;
using System.ComponentModel;

namespace SmartHotelAI.AI.Plugins;

/// <summary>
/// Semantic Kernel plugin exposing hotel booking operations
/// 
/// Design: Workflow Abstraction Pattern
/// - Exposes business-level functions (not low-level database operations)
/// - Accepts user-friendly parameters (light names, not IDs)
/// - Returns pre-formatted responses
/// - Hides implementation details
/// 
/// Token Impact:
/// - Function schema is much smaller (~80-100 tokens)
/// - LLM can call functions correctly (easier intent match)
/// </summary>
[Description("Hotel booking and fraud detection operations")]
public class HotelWorkflowPlugin
{
    private readonly BookingService _bookingService;
    private readonly FraudService _fraudService;

    public HotelWorkflowPlugin(BookingService bookingService, FraudService fraudService)
    {
        _bookingService = bookingService;
        _fraudService = fraudService;
    }

    [KernelFunction("book_room")]
    [Description("Book a new hotel room with guest details")]
    public string BookRoom(
        [Description("Guest name")] string guestName,
        [Description("Room type (Standard, Deluxe, Suite, Premium, Presidential)")] string roomType,
        [Description("Number of nights")] int nights)
    {
        try
        {
            // Validate room type is recognized (best practice: fail fast with clarification)
            if (!_bookingService.IsValidRoomType(roomType))
            {
                return $@"❓ **ROOM TYPE UNCLEAR**
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
**Input:** {roomType}

**Valid room types are:**
- Standard ($100/night)
- Deluxe ($150/night)
- Suite ($250/night)
- Premium ($300/night)
- Presidential ($500/night)

**Please specify which room type you'd like.**
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
            }

            var booking = _bookingService.Book(guestName, roomType, nights);

            return $@"✅ **BOOKING CONFIRMED**
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
**Guest:** {booking.GuestName}
**Room Type:** {booking.RoomType}
**Nights:** {booking.Nights}
**Total Amount:** ${booking.TotalAmount}
**Booking ID:** {booking.Id}
**Status:** {booking.Status}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
        }
        catch (Exception ex)
        {
            return $"❌ Booking failed: {ex.Message}";
        }
    }

    [KernelFunction("check_fraud")]
    [Description("Check if a booking is suspicious or fraudulent")]
    public string CheckFraud(
        [Description("Booking ID to check")] int bookingId)
    {
        try
        {
            var result = _fraudService.Check(bookingId);

            if (result.IsSuspicious)
            {
                return $@"⚠️ **FRAUD ALERT**
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
**Booking ID:** {bookingId}
**Risk Level:** {result.RiskLevel}
**Reason:** {result.Reason}
**Action:** Manual review recommended
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
            }

            return $@"✅ **BOOKING VERIFIED - LOW RISK**
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
**Booking ID:** {bookingId}
**Risk Level:** {result.RiskLevel}
**Status:** {result.Reason}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
        }
        catch (Exception ex)
        {
            return $"❌ Fraud check failed: {ex.Message}";
        }
    }

    [KernelFunction("cancel_booking")]
    [Description("Cancel an existing booking")]
    public string CancelBooking(
        [Description("Booking ID to cancel")] int bookingId)
    {
        try
        {
            var success = _bookingService.Cancel(bookingId);

            if (success)
                return $"✅ Booking {bookingId} cancelled successfully.";

            return $"❌ Booking {bookingId} not found.";
        }
        catch (Exception ex)
        {
            return $"❌ Cancellation failed: {ex.Message}";
        }
    }

    [KernelFunction("get_booking_history")]
    [Description("Retrieve all bookings (booking history)")]
    public string GetBookingHistory()
    {
        try
        {
            var bookings = _bookingService.GetAll();

            if (bookings.Count == 0)
                return "📋 No bookings found.";

            var result = "📋 **BOOKING HISTORY**\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
            foreach (var booking in bookings)
            {
                result += $"• **ID {booking.Id}** | {booking.GuestName} | {booking.RoomType} | " +
                         $"{booking.Nights} nights | ${booking.TotalAmount} | {booking.Status}\n";
            }
            result += "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";

            return result;
        }
        catch (Exception ex)
        {
            return $"❌ History retrieval failed: {ex.Message}";
        }
    }
}