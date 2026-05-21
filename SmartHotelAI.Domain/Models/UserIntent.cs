namespace SmartHotelAI.Domain.Models;

/// <summary>
/// Supported user intents for hotel booking system
/// </summary>


public enum UserIntent
{
    BookRoom,   // Book a new room
    CheckFraud,  // check if booking is suspicious
    CancelBooking, // Cancel an existing booking
    GetHistory,   // View all bookings
    Unknown       // Unrecognized intent
}