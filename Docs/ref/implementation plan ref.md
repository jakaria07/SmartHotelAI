# 🏗️ 0. PROJECT SETUP (IMPORTANT)

Create solution:

```bash
dotnet new sln -n SmartHotelAI
cd SmartHotelAI
```

Create projects:

```bash
dotnet new webapi -n SmartHotelAI.Api
dotnet new classlib -n SmartHotelAI.Domain
dotnet new classlib -n SmartHotelAI.AI
dotnet new console -n SmartHotelAI.Console
```

Add to solution:

```bash
dotnet sln add **/*
```

---

# 📁 FINAL STRUCTURE

```text
SmartHotelAI/
│
├── SmartHotelAI.Api/
├── SmartHotelAI.Domain/
├── SmartHotelAI.AI/
├── SmartHotelAI.Console/
└── SmartHotelAI.sln
```

---

# 🧱 1️⃣ DOMAIN LAYER (CORE MODELS)

📁 `SmartHotelAI.Domain/Models/`

---

## Booking.cs

```csharp
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

---

## FraudResult.cs

```csharp
public class FraudResult
{
    public bool IsSuspicious { get; set; }
    public string RiskLevel { get; set; }
    public string Reason { get; set; }
}
```

---

# 🧱 2️⃣ API LAYER (BUSINESS LOGIC)

📁 `SmartHotelAI.Api/Services/`

---

## BookingService.cs

```csharp
public class BookingService
{
    private static List<Booking> bookings = new();
    private static int idCounter = 1;

    public Booking Book(string name, string roomType, int nights)
    {
        var booking = new Booking
        {
            Id = idCounter++,
            GuestName = name,
            RoomType = roomType,
            Nights = nights,
            TotalAmount = nights * 100 // simple pricing
        };

        bookings.Add(booking);
        return booking;
    }

    public List<Booking> GetAll() => bookings;

    public Booking GetById(int id) => bookings.FirstOrDefault(b => b.Id == id);
}
```

---

## FraudService.cs

```csharp
public class FraudService
{
    private readonly BookingService _bookingService;

    public FraudService(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    public FraudResult Check(int bookingId)
    {
        var booking = _bookingService.GetById(bookingId);

        if (booking == null)
            return new FraudResult { IsSuspicious = false, RiskLevel = "None", Reason = "Booking not found" };

        var recentBookings = _bookingService.GetAll()
            .Where(b => b.CreatedAt > DateTime.UtcNow.AddMinutes(-5))
            .Count();

        if (recentBookings > 3)
        {
            return new FraudResult
            {
                IsSuspicious = true,
                RiskLevel = "High",
                Reason = "Multiple bookings in short time"
            };
        }

        if (booking.TotalAmount > 5000)
        {
            return new FraudResult
            {
                IsSuspicious = true,
                RiskLevel = "Medium",
                Reason = "High booking value"
            };
        }

        return new FraudResult
        {
            IsSuspicious = false,
            RiskLevel = "Low",
            Reason = "Normal behavior"
        };
    }
}
```

---

# 🧱 3️⃣ AI LAYER (SEMANTIC KERNEL)

📁 `SmartHotelAI.AI/`

---

## Install package

```bash
dotnet add package Microsoft.SemanticKernel
```

---

## 📁 Intents/IntentResult.cs

```csharp
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

---

## 📁 Intents/IntentClassifier.cs

👉 Keep it simple (LLM-based)

```csharp
public class IntentClassifier
{
    public async Task<IntentResult> Classify(string input)
    {
        // mock for now (you can replace with OpenAI later)

        if (input.ToLower().Contains("book"))
        {
            return new IntentResult
            {
                Intent = "BookRoom",
                Confidence = 0.9,
                Name = "Rahim",
                RoomType = "Deluxe",
                Nights = 2
            };
        }

        if (input.ToLower().Contains("fraud"))
        {
            return new IntentResult
            {
                Intent = "CheckFraud",
                Confidence = 0.9,
                BookingId = 1
            };
        }

        return new IntentResult { Intent = "Unknown", Confidence = 0.5 };
    }
}
```

---

## 📁 Plugins/HotelPlugin.cs

```csharp
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
        var booking = _bookingService.Book(name, roomType, nights);

        return $"✅ Booking Confirmed: {booking.GuestName}, Room: {booking.RoomType}, Nights: {booking.Nights}";
    }

    public string CheckFraud(int bookingId)
    {
        var result = _fraudService.Check(bookingId);

        return $"⚠️ Risk: {result.RiskLevel}\nReason: {result.Reason}";
    }
}
```

---

## 📁 Orchestrator/Orchestrator.cs

```csharp
public class Orchestrator
{
    private readonly IntentClassifier _classifier;
    private readonly HotelPlugin _plugin;

    public Orchestrator(IntentClassifier classifier, HotelPlugin plugin)
    {
        _classifier = classifier;
        _plugin = plugin;
    }

    public async Task<string> Handle(string input)
    {
        var intent = await _classifier.Classify(input);

        if (intent.Confidence < 0.65)
            return "❓ Could you clarify your request?";

        switch (intent.Intent)
        {
            case "BookRoom":
                return _plugin.BookRoom(intent.Name, intent.RoomType, intent.Nights);

            case "CheckFraud":
                return _plugin.CheckFraud(intent.BookingId);

            default:
                return "❌ Unknown request";
        }
    }
}
```

---

# 🧱 4️⃣ CONSOLE APP (DEMO)

📁 `SmartHotelAI.Console/Program.cs`

---

```csharp
var bookingService = new BookingService();
var fraudService = new FraudService(bookingService);

var plugin = new HotelPlugin(bookingService, fraudService);
var classifier = new IntentClassifier();

var orchestrator = new Orchestrator(classifier, plugin);

while (true)
{
    Console.Write("👉 Ask: ");
    var input = Console.ReadLine();

    var result = await orchestrator.Handle(input);

    Console.WriteLine("🤖 " + result);
}
```

---

# 🧪 TEST COMMANDS

Try:

```text
Book a room
Check fraud for booking 1
```

---

# 🎯 FINAL OUTPUT YOU ACHIEVE

```text
✔ AI-driven booking
✔ Fraud detection
✔ Intent-first architecture
✔ Programmatic routing
✔ Clean architecture
✔ Demo-ready system
```



