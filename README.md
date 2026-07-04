# TBJ.Integrations.Shipping

[![build](https://github.com/tbudaj/TBJ.Integrations.Shipping/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/tbudaj/TBJ.Integrations.Shipping/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/TBJ.Integrations.Shipping)](https://www.nuget.org/packages/TBJ.Integrations.Shipping)

Centralny punkt wejścia do wszystkich integracji kurierskich w systemie TBJ.
Implementuje wzorzec **Facade + Strategy** — aplikacja zawsze operuje na jednym interfejsie
`IShippingGateway`, niezależnie od tego, który kurier jest używany.

## Architektura

```
Aplikacja
    │
    ▼
IShippingGateway              ← jeden punkt wejścia
    │
    ├── CarrierType.InPostLocker  → InPostLockerShippingClient   (Paczkomat)
    ├── CarrierType.InPostCourier → InPostCourierShippingClient  (Kurier door-to-door)
    ├── CarrierType.DPD           → DpdShippingClient
    ├── CarrierType.DHL           → DhlShippingClient
    ├── CarrierType.GLS           → GlsShippingClient
    ├── CarrierType.UPS           → UpsShippingClient
    └── CarrierType.FedEx         → FedExShippingClient
```

## Uwierzytelnianie

Dane logowania do kurierów mogą być przekazane na dwa sposoby:

### 1. Per-tenant — `*AuthInfo` jako parametr żądania

W scenariuszu wielotenantowym każde wywołanie bramki zawiera obiekt `*AuthInfo`
z credentials konkretnego tenanta. Credentials te mogą być odczytane z bazy danych,
z konfiguracji tenanta lub z dowolnego innego źródła — pakiet nie narzuca sposobu
ich pozyskania.

### 2. Ujednolicone — domyślne credentials z `appsettings.json`

W scenariuszu single-tenant lub gdy wszystkie przesyłki wysyłane są z jednego
konta kurierskiego, można przechować domyślne credentials w sekcji `Shipping:*`
w `appsettings.json`. Są one używane automatycznie, gdy żądanie nie zawiera `*AuthInfo`.

Szczegółowe nazwy pól (`DefaultLogin`, `DefaultAccessToken`, `DefaultClientId` itp.)
oraz przykłady konfiguracji dla konkretnego dostawcy znajdują się w dedykowanych
dokumentacjach: `TBJ.Integrations.Shipping.<Dostawca>.md`.

## Rejestracja w DI

### Wariant 1 — konfiguracja z `appsettings.json` (zalecany)

```csharp
// Program.cs
builder.Services
    .AddShippingGateway()
    .AddInPostLocker(builder.Configuration)   // InPost Paczkomat
    .AddInPostCourier(builder.Configuration)  // InPost Kurier door-to-door
    .AddDPD(builder.Configuration)
    .AddDHL(builder.Configuration)
    .AddGLS(builder.Configuration)
    .AddUPS(builder.Configuration)
    .AddFedEx(builder.Configuration);
```

```jsonc
// appsettings.json — parametry infrastrukturalne (opcjonalnie domyślne credentials)
{
  "Shipping": {
    "InPost": {
      "BaseUrl":     "https://api-shipx-pl.easypack24.net",
      "LabelFormat": "PDF"
      // Współdzielone przez InPostLocker i InPostCourier
    },
    "DPD": {
      "BaseUrl": "https://dpdservices.dpd.com.pl/DPDPackageObjServicesService/DPDPackageObjServices",
      "Wsdl":    "https://dpdservices.dpd.com.pl/DPDPackageObjServicesService/DPDPackageObjServices?wsdl"
    },
    "DHL": {
      "BaseUrl":             "https://dhl24.com.pl/webapi2/provider/service.html",
      "DefaultServiceType":  "AH"
    },
    "GLS": {
      "BaseUrl":     "https://adeplus.gls-poland.com/adeplus/pm1/ade_webapi.php",
      "CountryCode": "PL"
    },
    "UPS": {
      "BaseUrl":   "https://onlinetools.ups.com",
      "TokenUrl":  "https://onlinetools.ups.com/security/v1/oauth/token"
    },
    "FedEx": {
      "BaseUrl":  "https://apis.fedex.com",
      "TokenUrl": "https://apis.fedex.com/oauth/token"
    }
  }
}
```

### Wariant 2 — konfiguracja inline (np. testy, skrypty)

```csharp
builder.Services
    .AddShippingGateway()
    .AddInPostLocker(opt =>
    {
        opt.BaseUrl     = "https://api-shipx-pl.easypack24.net";
        opt.LabelFormat = "PDF";
    })
    .AddInPostCourier(opt =>
    {
        opt.BaseUrl     = "https://api-shipx-pl.easypack24.net";
        opt.LabelFormat = "PDF";
    })
    .AddGLS(opt =>
    {
        opt.BaseUrl     = "https://adeplus.gls-poland.com/adeplus/pm1/ade_webapi.php";
        opt.CountryCode = "PL";
    });
```

### Rejestracja selektywna

Rejestruj tylko tych kurierów, których faktycznie używasz.
Nieużywane adaptery nie muszą być konfigurowane.

```csharp
// Tylko InPost Paczkomat i DPD
builder.Services
    .AddShippingGateway()
    .AddInPostLocker(builder.Configuration)
    .AddDPD(builder.Configuration);
```

## Użycie — `IShippingGateway`

Wstrzyknij `IShippingGateway` przez konstruktor lub minimalny API:

```csharp
public class OrderFulfillmentService
{
    private readonly IShippingGateway _shipping;

    public OrderFulfillmentService(IShippingGateway shipping)
        => _shipping = shipping;
}
```

### Rejestracja przesyłki (`CreateShipmentAsync`)

```csharp
// AuthInfo pochodzi z kontekstu tenanta — np. z bazy danych lub konfiguracji per-tenant
var authInfo = new InPostAuthInfo
{
    AccessToken    = tenantSettings.InPostToken,
    OrganizationId = tenantSettings.InPostOrgId,
};

// Przykład A: dostawa do Paczkomatu
var lockerRequest = new CreateShipmentRequest
{
    AuthInfo = authInfo,
    LockerTargetMachineId = "WAW123M",          // wymagane dla InPostLocker
    ServiceCode = "inpost_locker_standard",
    SenderAddress = new Address
    {
        Name       = "Firma Sp. z o.o.",
        Street     = "ul. Magazynowa 1",
        PostalCode = "00-001",
        City       = "Warszawa",
        CountryCode = "PL",
    },
    SenderContact = new ContactInfo
    {
        Name  = "Jan Kowalski",
        Phone = "+48123456789",
        Email = "wysylka@firma.pl",
    },
    RecipientAddress = new Address
    {
        Name       = "Anna Nowak",
        Street     = "ul. Kwiatowa 5/3",
        PostalCode = "30-001",
        City       = "Kraków",
        CountryCode = "PL",
    },
    RecipientContact = new ContactInfo
    {
        Name  = "Anna Nowak",
        Phone = "+48987654321",
        Email = "anna@example.com",             // e-mail wymagany — kod odbioru z paczkomatu
    },
    Parcels = new List<ParcelDimensions>
    {
        new() { WeightKg = 1.5m, LengthCm = 38, WidthCm = 64, HeightCm = 41 }, // rozmiar A
    },
    Reference = "ZAMOWIENIE-2024-001",
};

ShipmentResult result = await _shipping.CreateShipmentAsync(CarrierType.InPostLocker, lockerRequest);

// Przykład B: dostawa kurierem door-to-door
var courierRequest = new CreateShipmentRequest
{
    AuthInfo = authInfo,
    ServiceCode = "inpost_courier_standard",
    SenderAddress = new Address
    {
        Name       = "Firma Sp. z o.o.",
        Street     = "ul. Magazynowa 1",
        PostalCode = "00-001",
        City       = "Warszawa",
        CountryCode = "PL",
    },
    SenderContact = new ContactInfo
    {
        Name  = "Jan Kowalski",
        Phone = "+48123456789",
        Email = "wysylka@firma.pl",
    },
    RecipientAddress = new Address
    {
        Name       = "Anna Nowak",
        Street     = "ul. Kwiatowa 5/3",
        PostalCode = "30-001",
        City       = "Kraków",
        CountryCode = "PL",
    },
    RecipientContact = new ContactInfo
    {
        Name  = "Anna Nowak",
        Phone = "+48987654321",
    },
    Parcels = new List<ParcelDimensions>
    {
        new() { WeightKg = 2.5m, LengthCm = 30, WidthCm = 20, HeightCm = 15 },
    },
    Reference = "ZAMOWIENIE-2024-002",
    Cod       = new CodInfo { AmountPln = 150.00m },
};

ShipmentResult courierResult = await _shipping.CreateShipmentAsync(CarrierType.InPostCourier, courierRequest);

Console.WriteLine(result.TrackingNumber);       // numer śledzenia
Console.WriteLine(result.CarrierShipmentId);    // wewnętrzny ID u kuriera
if (result.Label is not null)
{
    // result.Label.ContentType → "application/pdf" itp.
    // result.Label.Content     → byte[] z zawartością etykiety
    await File.WriteAllBytesAsync("etykieta.pdf", result.Label.Content);
}
```

### Zamówienie odbioru (`OrderPickupAsync`)

```csharp
var pickup = new OrderPickupRequest
{
    AuthInfo      = authInfo,   // ten sam authInfo co przy rejestracji przesyłki
    ShipmentIds   = new List<string> { result.TrackingNumber },
    PickupAddress = new Address
    {
        Name       = "Firma Sp. z o.o.",
        Street     = "ul. Magazynowa 1",
        PostalCode = "00-001",
        City       = "Warszawa",
    },
    PickupContact  = new ContactInfo { Name = "Jan Kowalski", Phone = "+48123456789" },
    PickupDate     = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
    PickupTimeFrom = TimeOnly.Parse("09:00"),
    PickupTimeTo   = TimeOnly.Parse("17:00"),
};

PickupResult pickupResult = await _shipping.OrderPickupAsync(CarrierType.InPost, pickup);
Console.WriteLine(pickupResult.PickupOrderId);
```

### Śledzenie przesyłki (`TrackShipmentAsync`)

`AuthInfo` jest tu przekazywany jawnie (przesyłka może być śledzona niezależnie od żądania rejestracji):

```csharp
var authInfo = new DpdAuthInfo { Username = "firma@dpd.com", Password = "haslo", Fid = 12345 };

TrackingResult tracking = await _shipping.TrackShipmentAsync(
    CarrierType.DPD, authInfo, "12345678901234");

Console.WriteLine(tracking.CurrentStatus);        // enum ShipmentStatus
Console.WriteLine(tracking.StatusDescription);    // opis tekstowy

foreach (var ev in tracking.Events)
{
    Console.WriteLine($"{ev.OccurredAt:g}  {ev.Location}  {ev.Description}");
}
```

Wartości `ShipmentStatus`:
| Wartość | Opis |
|---|---|
| `Registered` | Przesyłka zarejestrowana |
| `PickedUp` | Odebrana przez kuriera |
| `InTransit` | W transporcie |
| `OutForDelivery` | W doręczeniu |
| `Delivered` | Dostarczona |
| `DeliveryAttemptFailed` | Nieudana próba doręczenia |
| `ReturnedToSender` | Zwrócona do nadawcy |
| `Cancelled` | Anulowana |
| `Unknown` | Status nieznany |

### Potwierdzenie doręczenia — POD (`GetDeliveryConfirmationAsync`)

```csharp
var authInfo = new DhlAuthInfo { Login = "firma", Password = "haslo" };

DeliveryConfirmationResult pod = await _shipping.GetDeliveryConfirmationAsync(
    CarrierType.DHL, authInfo, "1234567890");

Console.WriteLine(pod.DeliveredAt);    // DateTimeOffset
Console.WriteLine(pod.ReceivedBy);     // imię odbiorcy / podpis
if (pod.PodDocument is not null)
    await File.WriteAllBytesAsync("pod.pdf", pod.PodDocument);
```

### Bezpośredni dostęp do adaptera (`GetClient`)

Gdy potrzebujesz wywołania specyficznego tylko dla danego kuriera:

```csharp
IShippingClient lockerClient  = _shipping.GetClient(CarrierType.InPostLocker);   // InPostLockerShippingClient
IShippingClient courierClient = _shipping.GetClient(CarrierType.InPostCourier);  // InPostCourierShippingClient
```

## Obsługa błędów

Wszystkie metody mogą rzucić:

| Wyjątek | Kiedy |
|---|---|
| `ShippingException` | API kuriera zwróciło błąd, timeout, błąd sieci |
| `InvalidOperationException` | Adapter dla danego `CarrierType` nie jest zarejestrowany lub przekazano błędny typ `AuthInfo` |

```csharp
try
{
    var result = await _shipping.CreateShipmentAsync(CarrierType.DPD, request);
}
catch (ShippingException ex)
{
    _logger.LogError(ex,
        "Błąd rejestracji przesyłki {Carrier}: {Message}",
        ex.Carrier, ex.Message);
    // ex.Carrier      → CarrierType
    // ex.StatusCode   → kod HTTP lub SOAP fault code (opcjonalnie)
    // ex.InnerException → oryginalny wyjątek transportowy
}
catch (InvalidOperationException ex)
{
    // Kurier nie jest skonfigurowany lub nieprawidłowy AuthInfo
    _logger.LogCritical(ex, "Brak adaptera kurierskiego lub błąd AuthInfo.");
}
```

## Cykl życia serwisów (DI lifetime)

| Serwis | Lifetime | Uzasadnienie |
|---|---|---|
| `IShippingGateway` | `Scoped` | Per-request — bezpieczny wątkowo |
| `IShippingClient` (adaptery) | `Scoped` | Per-request |
| `UpsTokenCache` | `Singleton` | Cache tokenów OAuth2 per-tenant (ClientId), współdzielony między requestami |
| `FedExTokenCache` | `Singleton` | Cache tokenów OAuth2 per-tenant (ClientId), współdzielony między requestami |
| `HttpClient` (wszystkie) | zarządzany przez `IHttpClientFactory` | Automatyczna rotacja handlerów |
