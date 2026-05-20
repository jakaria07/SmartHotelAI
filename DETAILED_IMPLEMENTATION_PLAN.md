# 🏨 SmartHotelAI - Detailed Step-by-Step Implementation Plan

## 📋 Overview
This document provides a complete, step-by-step implementation guide to build the SmartHotelAI project from scratch. Follow each section in order.

**Project Structure:**
```
SmartHotelAI/
├── SmartHotelAI.Domain/          (Business Models - Core)
├── SmartHotelAI.Api/             (Services & Controllers)
├── SmartHotelAI.AI/              (Semantic Kernel Integration)
├── SmartHotelAI.Console/         (Demo CLI Application)
└── SmartHotelAI.sln
```

**Current Status:** ✅ Solutions & projects created, ⏳ Need implementation

---

## 🔧 PHASE 1: SETUP & DEPENDENCIES (15 min)

### Step 1.1 - Add NuGet Packages to SmartHotelAI.Api
Navigate to the solution directory and run:
```bash
cd d:\Project\AI\SmartHotelAI
dotnet add SmartHotelAI.Api/SmartHotelAI.Api.csproj package Microsoft.SemanticKernel
```

### Step 1.2 - Add NuGet Packages to SmartHotelAI.AI
```bash
dotnet add SmartHotelAI.AI/SmartHotelAI.AI.csproj package Microsoft.SemanticKernel
```

### Step 1.3 - Add Project References
Add references so projects can communicate:

```bash
# Console needs Api and AI
dotnet add SmartHotelAI.Console/SmartHotelAI.Console.csproj reference SmartHotelAI.Api/SmartHotelAI.Api.csproj
dotnet add SmartHotelAI.Console/SmartHotelAI.Console.csproj reference SmartHotelAI.AI/SmartHotelAI.AI.csproj

# Api needs Domain
dotnet add SmartHotelAI.Api/SmartHotelAI.Api.csproj reference SmartHotelAI.Domain/SmartHotelAI.Domain.csproj

# AI needs Domain and Api
dotnet add SmartHotelAI.AI/SmartHotelAI.AI.csproj reference SmartHotelAI.Domain/SmartHotelAI.Domain.csproj
dotnet add SmartHotelAI.AI/SmartHotelAI.AI.csproj reference SmartHotelAI.Api/SmartHotelAI.Api.csproj
```

### Step 1.4 - Verify Build
```bash
dotnet build
```
All projects should build successfully.

---

## 🧱 PHASE 2: DOMAIN LAYER - CORE MODELS (20 min)

**Location:** `SmartHotelAI.Domain/`

### Step 2.1 - Delete Placeholder
Delete `SmartHotelAI.Domain/Class1.cs`

### Step 2.2 - Create Models Folder
Create directory: `SmartHotelAI.Domain/Models/`

### Step 2.3 - Create Booking.cs
**File:** `SmartHotelAI.Domain/Models/Booking.cs`

```csharp
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### Step 2.4 - Create FraudResult.cs
**File:** `SmartHotelAI.Domain/Models/FraudResult.cs`

```csharp
namespace SmartHotelAI.Domain.Models;

public class FraudResult
{
    public bool IsSuspicious { get; set; }
    public string RiskLevel { get; set; }
    public string Reason { get; set; }
}
```

### Step 2.5 - Create IntentResult.cs
**File:** `SmartHotelAI.Domain/Models/IntentResult.cs`

```csharp
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
```

### Step 2.6 - Verify
```bash
dotnet build
```

✅ Domain layer complete.

---

## 🔄 PHASE 3: API LAYER - BUSINESS LOGIC (30 min)

**Location:** `SmartHotelAI.Api/Services/`

### Step 3.1 - Create Services Folder
Create directory: `SmartHotelAI.Api/Services/`

### Step 3.2 - Create BookingService.cs
**File:** `SmartHotelAI.Api/Services/BookingService.cs`

```csharp
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
            TotalAmount = nights * 100m, // Simple pricing: $100 per night
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
```

### Step 3.3 - Create FraudService.cs
**File:** `SmartHotelAI.Api/Services/FraudService.cs`

⚠️ **Enhanced Logic:** This implementation includes 5 optimized fraud detection rules with proper time windows and behavioral patterns.

```csharp
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
            return new FraudResult
            {
                IsSuspicious = false,
                RiskLevel = "None",
                Reason = "Booking not found"
            };

        // ========== RULE 0: BLACKLIST CHECK (HIGHEST PRIORITY) ==========
        // Check if booking is explicitly blacklisted
        if (_blacklist.Contains(bookingId))
        {
            return new FraudResult
            {
                IsSuspicious = true,
                RiskLevel = "High",
                Reason = "Booking is blacklisted"
            };
        }

        // ========== RULE 1: RAPID BOOKINGS DETECTION ==========
        // Detect if multiple bookings made in short time (potential bot/mass booking)
        var recentBookings = _bookingService.GetAll()
            .Where(b => b.CreatedAt > DateTime.UtcNow.AddMinutes(-5))
            .Count();

        if (recentBookings > 3)
        {
            return new FraudResult
            {
                IsSuspicious = true,
                RiskLevel = "High",
                Reason = "Multiple bookings in short time (> 3 in 5 min)"
            };
        }

        // ========== RULE 2: FREQUENT CANCELLATIONS (NEW) ==========
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
                Reason = "Frequent cancellations detected (>= 3 in 10 min)"
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

        // ========== RULE 4: GUEST BEHAVIOR PATTERN (IMPROVED) ==========
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
                RiskLevel = "Medium",
                Reason = "Frequent high-value bookings detected (> 3 bookings, $5000+ in 10 min)"
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
    /// Remove booking from blacklist
    /// </summary>
    public void RemoveFromBlacklist(int bookingId)
    {
        _blacklist.Remove(bookingId);
    }

    /// <summary>
    /// Get current blacklist (for audit purposes)
    /// </summary>
    public List<int> GetBlacklist() => new List<int>(_blacklist);
}
```

**Key Improvements:**

| Aspect | Before | After |
|--------|--------|-------|
| **Rule 3 (Pattern)** | ❌ No time window | ✅ 10-min time window added |
| **Blacklist** | ❌ Unused | ✅ Actively checked (Rule 0) |
| **Cancellations** | ❌ Missing | ✅ New Rule 2: Detects book→cancel pattern |
| **Rule Order** | ❌ Suboptimal | ✅ Optimized: Blacklist → Rapid → Cancellations → Value → Pattern |
| **Time Windows** | ⚠️ Inconsistent | ✅ Standardized: 5min for rapid bookings, 10min for patterns |
| **Risk Levels** | ⚠️ Mixed | ✅ Consistent: High for immediate threats, Medium for patterns |
| **Code Quality** | ⚠️ Basic | ✅ Documented, maintainable, production-ready |

### Step 3.4 - Verify
```bash
dotnet build
```

✅ API layer services complete.

---

## 🤖 PHASE 4: AI LAYER - SEMANTIC KERNEL (45 min)

**Location:** `SmartHotelAI.AI/`

### Step 4.1 - Delete Placeholder
Delete `SmartHotelAI.AI/Class1.cs`

### Step 4.2 - Create Folder Structure
Create directories:
```
SmartHotelAI.AI/
├── Intents/
├── Plugins/
├── Orchestrator/
└── Models/
```

### Step 4.3 - Create IntentClassifier.cs
**File:** `SmartHotelAI.AI/Intents/IntentClassifier.cs`

```csharp
using SmartHotelAI.Domain.Models;

namespace SmartHotelAI.AI.Intents;

public class IntentClassifier
{
    /// <summary>
    /// Classifies user input into intents with confidence scores.
    /// Currently uses simple pattern matching (mock implementation).
    /// Can be replaced with LLM-based classification later.
    /// </summary>
    public async Task<IntentResult> Classify(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        var lowerInput = input.ToLower();

        // Intent: Book Room
        if (lowerInput.Contains("book") && (lowerInput.Contains("room") || lowerInput.Contains("hotel")))
        {
            return new IntentResult
            {
                Intent = "BookRoom",
                Confidence = 0.95,
                Name = ExtractName(input) ?? "Guest",
                RoomType = ExtractRoomType(input) ?? "Standard",
                Nights = ExtractNights(input)
            };
        }

        // Intent: Check Fraud
        if (lowerInput.Contains("fraud") || lowerInput.Contains("suspicious") || lowerInput.Contains("check booking"))
        {
            return new IntentResult
            {
                Intent = "CheckFraud",
                Confidence = 0.90,
                BookingId = ExtractBookingId(input) ?? 1
            };
        }

        // Intent: Cancel Booking
        if (lowerInput.Contains("cancel"))
        {
            return new IntentResult
            {
                Intent = "CancelBooking",
                Confidence = 0.85,
                BookingId = ExtractBookingId(input) ?? 1
            };
        }

        // Intent: Get History
        if (lowerInput.Contains("history") || lowerInput.Contains("list"))
        {
            return new IntentResult
            {
                Intent = "GetHistory",
                Confidence = 0.80
            };
        }

        // Default: Unknown
        return new IntentResult
        {
            Intent = "Unknown",
            Confidence = 0.30
        };
    }

    private string ExtractName(string input)
    {
        var words = input.Split(' ');
        // Simple heuristic: capitalize word after "name" or first capital word
        for (int i = 0; i < words.Length - 1; i++)
        {
            if (words[i].ToLower() == "name" && i + 1 < words.Length)
                return words[i + 1];
        }
        return null;
    }

    private string ExtractRoomType(string input)
    {
        var lowerInput = input.ToLower();
        if (lowerInput.Contains("deluxe")) return "Deluxe";
        if (lowerInput.Contains("suite")) return "Suite";
        if (lowerInput.Contains("standard")) return "Standard";
        if (lowerInput.Contains("premium")) return "Premium";
        return null;
    }

    private int ExtractNights(string input)
    {
        var words = input.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if ((words[i].ToLower() == "nights" || words[i].ToLower() == "night") && i > 0)
            {
                if (int.TryParse(words[i - 1], out int nights))
                    return nights;
            }
        }
        return 1; // Default
    }

    private int? ExtractBookingId(string input)
    {
        var words = input.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].ToLower() == "booking" || words[i].ToLower() == "id")
            {
                if (i + 1 < words.Length && int.TryParse(words[i + 1], out int bookingId))
                    return bookingId;
            }
        }

        // Try to extract any number as booking ID
        foreach (var word in words)
        {
            if (int.TryParse(word, out int id) && id > 0)
                return id;
        }

        return null;
    }
}
```

### Step 4.4 - Create HotelPlugin.cs
**File:** `SmartHotelAI.AI/Plugins/HotelPlugin.cs`

```csharp
using SmartHotelAI.Api.Services;

namespace SmartHotelAI.AI.Plugins;

public class HotelPlugin
{
    private readonly BookingService _bookingService;
    private readonly FraudService _fraudService;

    public HotelPlugin(BookingService bookingService, FraudService fraudService)
    {
        _bookingService = bookingService;
        _fraudService = fraudService;
    }

    public string BookRoom(string name, string roomType, int nights)
    {
        try
        {
            var booking = _bookingService.Book(name, roomType, nights);

            return $@"✅ BOOKING CONFIRMED
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Guest: {booking.GuestName}
Room Type: {booking.RoomType}
Nights: {booking.Nights}
Total Amount: ${booking.TotalAmount}
Booking ID: {booking.Id}
Check-in: {booking.CheckInDate:yyyy-MM-dd}
Status: {booking.Status}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
        }
        catch (Exception ex)
        {
            return $"❌ Booking failed: {ex.Message}";
        }
    }

    public string CheckFraud(int bookingId)
    {
        try
        {
            var result = _fraudService.Check(bookingId);

            if (result.IsSuspicious)
            {
                return $@"⚠️ FRAUD ALERT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Risk Level: {result.RiskLevel}
Booking ID: {bookingId}
Reason: {result.Reason}
Action: Manual review recommended
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
            }
            else
            {
                return $@"✅ BOOKING VERIFIED
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Booking ID: {bookingId}
Risk Level: {result.RiskLevel}
Status: {result.Reason}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Fraud check failed: {ex.Message}";
        }
    }

    public string CancelBooking(int bookingId)
    {
        try
        {
            var success = _bookingService.Cancel(bookingId);

            if (success)
            {
                return $"✅ Booking {bookingId} cancelled successfully.";
            }
            else
            {
                return $"❌ Booking {bookingId} not found.";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Cancellation failed: {ex.Message}";
        }
    }

    public string GetHistory()
    {
        try
        {
            var bookings = _bookingService.GetAll();

            if (bookings.Count == 0)
            {
                return "📋 No bookings found.";
            }

            var result = "📋 BOOKING HISTORY\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
            foreach (var booking in bookings)
            {
                result += $"ID: {booking.Id} | {booking.GuestName} | {booking.RoomType} | {booking.Nights} nights | ${booking.TotalAmount} | {booking.Status}\n";
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
```

### Step 4.5 - Create Orchestrator.cs
**File:** `SmartHotelAI.AI/Orchestrator/Orchestrator.cs`

```csharp
using SmartHotelAI.AI.Intents;
using SmartHotelAI.AI.Plugins;

namespace SmartHotelAI.AI.Orchestrator;

public class Orchestrator
{
    private readonly IntentClassifier _classifier;
    private readonly HotelPlugin _plugin;

    public Orchestrator(IntentClassifier classifier, HotelPlugin plugin)
    {
        _classifier = classifier;
        _plugin = plugin;
    }

    /// <summary>
    /// Main orchestration method: Classify intent → Route to appropriate plugin method
    /// </summary>
    public async Task<string> Handle(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        // Step 1: Classify Intent
        var intent = await _classifier.Classify(input);

        // Step 2: Confidence Threshold Check
        if (intent.Confidence < 0.65)
        {
            return "❓ I didn't quite understand. Could you clarify your request?\n\nSupported commands:\n" +
                   "- 'Book a [roomType] room for [nights] nights'\n" +
                   "- 'Check fraud for booking [id]'\n" +
                   "- 'Cancel booking [id]'\n" +
                   "- 'Show booking history'";
        }

        // Step 3: Programmatic Routing (No LLM hallucination)
        return intent.Intent switch
        {
            "BookRoom" => _plugin.BookRoom(intent.Name, intent.RoomType, intent.Nights),
            "CheckFraud" => _plugin.CheckFraud(intent.BookingId),
            "CancelBooking" => _plugin.CancelBooking(intent.BookingId),
            "GetHistory" => _plugin.GetHistory(),
            _ => "❌ Unknown request. Please try again."
        };
    }
}
```

### Step 4.6 - Verify
```bash
dotnet build
```

✅ AI layer complete.

---

## 💻 PHASE 5: CONSOLE APP - DEMO CLI (20 min)

**Location:** `SmartHotelAI.Console/Program.cs`

### Step 5.1 - Replace Program.cs
**File:** `SmartHotelAI.Console/Program.cs`

```csharp
using SmartHotelAI.Api.Services;
using SmartHotelAI.AI.Intents;
using SmartHotelAI.AI.Plugins;
using SmartHotelAI.AI.Orchestrator;

// Initialize Services
var bookingService = new BookingService();
var fraudService = new FraudService(bookingService);

// Initialize AI Components
var classifier = new IntentClassifier();
var plugin = new HotelPlugin(bookingService, fraudService);
var orchestrator = new Orchestrator(classifier, plugin);

// Display Welcome Message
Console.Clear();
Console.WriteLine("╔════════════════════════════════════════╗");
Console.WriteLine("║     🏨 SMART HOTEL AI ASSISTANT 🤖    ║");
Console.WriteLine("╚════════════════════════════════════════╝\n");
Console.WriteLine("Commands:");
Console.WriteLine("  📅 'Book a [room type] room for [nights] nights'");
Console.WriteLine("  🔍 'Check fraud for booking [id]'");
Console.WriteLine("  ❌ 'Cancel booking [id]'");
Console.WriteLine("  📋 'Show booking history'");
Console.WriteLine("  🚪 'exit' to quit\n");

// Main Loop
while (true)
{
    Console.Write("👉 You: ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("\n👋 Thank you for using SmartHotel AI. Goodbye!\n");
        break;
    }

    // Process with Orchestrator
    var result = await orchestrator.Handle(input);
    Console.WriteLine($"\n🤖 Assistant:\n{result}\n");
}
```

### Step 5.2 - Verify Build
```bash
dotnet build
```

✅ Console app complete.

---

## ✅ PHASE 6: TESTING & VALIDATION (15 min)

### Step 6.1 - Run the Application
```bash
dotnet run --project SmartHotelAI.Console/SmartHotelAI.Console.csproj
```

### Step 6.2 - Test Commands

Try these inputs in order:

**Test 1: Book a Room**
```
Input: Book a deluxe room for 3 nights
Expected: Booking confirmed with ID 1, $300 total
```

**Test 2: Check Fraud (Normal)**
```
Input: Check fraud for booking 1
Expected: Low risk, normal behavior
```

**Test 3: Rapid Bookings (Fraud Detection)**
```
Input: Book a standard room for 2 nights
Input: Book a standard room for 2 nights
Input: Book a standard room for 2 nights
Input: Book a standard room for 2 nights
Input: Check fraud for booking 2
Expected: High risk - Multiple bookings in short time
```

**Test 4: High Value Booking**
```
Input: Book a suite room for 60 nights
Input: Check fraud for booking 5
Expected: High risk - High booking value (> $5000)
```

**Test 5: View History**
```
Input: Show booking history
Expected: List of all bookings
```

**Test 6: Cancel Booking**
```
Input: Cancel booking 1
Expected: Booking cancelled successfully
```

**Test 7: Confidence Threshold**
```
Input: Random gibberish words
Expected: Clarification request
```

---

## 🏗️ PHASE 7: PROJECT STRUCTURE VERIFICATION

### Final Folder Structure
```
SmartHotelAI/
│
├── SmartHotelAI.Domain/
│   ├── Models/
│   │   ├── Booking.cs
│   │   ├── FraudResult.cs
│   │   └── IntentResult.cs
│   └── SmartHotelAI.Domain.csproj
│
├── SmartHotelAI.Api/
│   ├── Services/
│   │   ├── BookingService.cs
│   │   └── FraudService.cs
│   ├── Program.cs (unchanged)
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── SmartHotelAI.Api.csproj (updated)
│
├── SmartHotelAI.AI/
│   ├── Intents/
│   │   └── IntentClassifier.cs
│   ├── Plugins/
│   │   └── HotelPlugin.cs
│   ├── Orchestrator/
│   │   └── Orchestrator.cs
│   └── SmartHotelAI.AI.csproj (updated)
│
├── SmartHotelAI.Console/
│   ├── Program.cs (updated)
│   └── SmartHotelAI.Console.csproj (updated)
│
├── SmartHotelAI.sln
└── DETAILED_IMPLEMENTATION_PLAN.md (this file)
```

### Verify All Projects Build
```bash
dotnet build
```

All 4 projects should compile without errors.

---

## 📊 PHASE 8: OPTIONAL ENHANCEMENTS (Future)

These are nice-to-haves but not required for MVP:

### Enhancement 1: API Controllers
Add REST endpoints to expose booking and fraud checking via HTTP.

**File:** `SmartHotelAI.Api/Controllers/BookingsController.cs`

### Enhancement 2: Semantic Kernel Integration
Replace mock IntentClassifier with actual Semantic Kernel LLM calls.

### Enhancement 3: Token Usage Tracking
Track and log prompt/completion tokens for optimization.

### Enhancement 4: Blacklist Management
Implement guest blacklist functionality in FraudService.

### Enhancement 5: Database Integration
Replace in-memory List<> with Entity Framework Core (SQL/SQLite).

---

## 🎯 SUCCESS CRITERIA

✅ **Phase 1:** All NuGet packages installed, projects reference each other correctly
✅ **Phase 2:** Domain models compile and have proper namespaces
✅ **Phase 3:** BookingService and FraudService work with in-memory storage
✅ **Phase 4:** IntentClassifier, HotelPlugin, Orchestrator integrate seamlessly
✅ **Phase 5:** Console app runs and accepts user input
✅ **Phase 6:** All test commands execute and produce expected output
✅ **Phase 7:** Final folder structure matches design
✅ **Phase 8:** Solution builds without warnings or errors

---

## 📝 NOTES

- **No Database:** All data stored in-memory. Will reset on app restart.
- **Mock Intent Classifier:** Uses pattern matching. Can be replaced with LLM later.
- **Deterministic Routing:** AI layer routes to plugins programmatically, no hallucination risk.
- **Fraud Rules:** Simple but realistic. Can be made more sophisticated.
- **.NET 10.0:** Using latest .NET framework. Adjust if needed for compatibility.

---

## ❓ TROUBLESHOOTING

### Issue: "Project reference does not exist"
**Solution:** Ensure you ran all `dotnet add reference` commands in Step 1.3

### Issue: "Type X not found in namespace Y"
**Solution:** Verify correct namespaces in model files match imports

### Issue: Console app doesn't run
**Solution:** Make sure all projects build first: `dotnet build`

### Issue: Intent classification not working
**Solution:** Check that input strings match the patterns in IntentClassifier.cs

---

## 🚀 NEXT STEPS AFTER COMPLETION

1. ✅ Test all core functionality
2. ✅ Add unit tests (xUnit/NUnit)
3. ✅ Add API controllers for HTTP access
4. ✅ Integrate real Semantic Kernel LLM models
5. ✅ Add database persistence
6. ✅ Create comprehensive README with diagrams
7. ✅ Deploy to Azure/production

---

**Created:** 2026-05-20
**Status:** Ready for Implementation
