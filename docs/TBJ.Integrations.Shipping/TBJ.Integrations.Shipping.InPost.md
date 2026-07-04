# TBJ.Integrations.Shipping.InPost

Adapter integracji z **InPost ShipX API** — usługi kurierskie InPost (paczkomaty oraz kurier door-to-door).

- **Protokół:** REST / JSON over HTTPS
- **Autentykacja:** Bearer Token (statyczny klucz API) — przekazywany per-żądanie przez `InPostAuthInfo`
- **API:** InPost ShipX API v1

## Tryby dostawy

InPost udostępnia dwa zupełnie różne modele dostawy, zarejestrowane jako osobne adaptery:

| CarrierType | Klasa klienta | Opis | Wymagane pole |
|---|---|---|---|
| `InPostLocker` | `InPostLockerShippingClient` | Dostawa do Paczkomatu | `LockerTargetMachineId` |
| `InPostCourier` | `InPostCourierShippingClient` | Dostawa kurierem door-to-door | — |

Oba adaptery korzystają z tej samej konfiguracji (`InPostOptions`) i tego samego klienta HTTP.

## Jak uzyskać dane dostępowe

### 1. Rejestracja konta

Przejdź do portalu InPost dla firm:
- **Produkcja:** https://manager.paczkomaty.pl → zakładka _Integracje_ → _ShipX API_
- **Sandbox:** https://sandbox-manager.paczkomaty.pl

### 2. Uzyskanie Access Token (klucz API)

1. Po zalogowaniu przejdź do: **Ustawienia → API → Klucze dostępowe**
2. Kliknij **Wygeneruj nowy klucz**
3. Skopiuj wygenerowany token — wyświetlany jest tylko raz
4. Token ma format: `tok_live_XXXXXXXXXXXXXXXXXXXXXXXXXXXX` (produkcja) lub `tok_sandbox_...` (sandbox)

### 3. Uzyskanie Organization ID

`OrganizationId` to identyfikator Twojej organizacji w systemie InPost:
- W panelu menedżera: **Ustawienia → Informacje o firmie → ID organizacji**
- Wartość numeryczna, np. `12345`

> Dane te (`AccessToken`, `OrganizationId`) mogą być przekazywane **per-żądanie** przez `InPostAuthInfo` (Scenariusz A) lub skonfigurowane jako domyślne w `appsettings.json` (Scenariusz B).

## Środowiska

| Środowisko | BaseUrl |
|---|---|
| **Produkcyjne** | `https://api-shipx-pl.easypack24.net` |
| **Sandbox (testowe)** | `https://sandbox-api-shipx-pl.easypack24.net` |

W środowisku sandbox można tworzyć przesyłki testowe bez realnych opłat.
Konto sandbox zakładasz oddzielnie na https://sandbox-manager.paczkomaty.pl

## Model uwierzytelniania (wielotenantowość)

Adapter obsługuje dwa scenariusze uwierzytelniania:

### Scenariusz A — konto tenanta

Tenant posiada własne konto InPost. Credentials przekazywane **per-żądanie** przez `InPostAuthInfo`.

```csharp
var authInfo = new InPostAuthInfo
{
    AccessToken    = tenantSettings.InPostToken,
    OrganizationId = tenantSettings.InPostOrgId,
};

var result = await _shipping.CreateShipmentAsync(CarrierType.InPostLocker, new CreateShipmentRequest
{
    AuthInfo = authInfo,
    LockerTargetMachineId = "WAW123M",
    // ...
});
```

### Scenariusz B — nasze konto aplikacji

Chcemy wysyłać z **naszego** konta InPost (np. własna logistyka). Credentials konfigurowane
w `appsettings.json` przez pola `Default*` — używane automatycznie gdy `AuthInfo = null`.

```csharp
// AuthInfo pominięte — adapter użyje DefaultAccessToken i DefaultOrganizationId z InPostOptions
var result = await _shipping.CreateShipmentAsync(CarrierType.InPostCourier, new CreateShipmentRequest
{
    // AuthInfo = null (pominięte)
    SenderAddress = ...,
});
```

---

## Konfiguracja `appsettings.json`

```jsonc
// appsettings.json
{
  "Shipping": {
    "InPost": {
      "BaseUrl":     "https://api-shipx-pl.easypack24.net",
      "Timeout":     "00:00:30",
      "LabelFormat": "pdf",

      // Scenariusz B — nasze konto InPost (opcjonalne).
      // Używane gdy żądanie nie zawiera InPostAuthInfo.
      "DefaultAccessToken":    "tok_live_XXXXXXXXXXXXXXXXXXXXXXXXXXXX",
      "DefaultOrganizationId": "12345"
    }
  }
}
```

### Opis parametrów

| Parametr | Wymagany | Domyślna wartość | Opis |
|---|---|---|---|
| `BaseUrl` | nie | `https://api-shipx-pl.easypack24.net` | Adres bazowy API (zmień na sandbox do testów) |
| `Timeout` | nie | `00:00:30` | Timeout żądań HTTP (`hh:mm:ss`) |
| `LabelFormat` | nie | `pdf` | Format etykiety: `pdf`, `zpl`, `epl` |
| `DefaultAccessToken` | nie* | — | Bearer token naszego konta InPost (Scenariusz B) |
| `DefaultOrganizationId` | nie* | — | ID organizacji naszego konta InPost (Scenariusz B) |

*Wymagane razem jeśli używasz Scenariusza B bez przekazywania `InPostAuthInfo`.

### Konfiguracja środowisk (zalecana struktura)

```jsonc
// appsettings.Development.json — sandbox
{
  "Shipping": {
    "InPost": {
      "BaseUrl": "https://sandbox-api-shipx-pl.easypack24.net"
    }
  }
}

// appsettings.Production.json — produkcja
{
  "Shipping": {
    "InPost": {
      "BaseUrl": "https://api-shipx-pl.easypack24.net"
    }
  }
}
```

## Rejestracja w DI

```csharp
// z appsettings (zalecane) — oba tryby dostawy, wspólna konfiguracja
builder.Services
    .AddShippingGateway()
    .AddInPostLocker(builder.Configuration)
    .AddInPostCourier(builder.Configuration);

// lub inline — można rejestrować tylko wybrany tryb
builder.Services
    .AddShippingGateway()
    .AddInPostLocker(opt =>
    {
        opt.BaseUrl     = "https://sandbox-api-shipx-pl.easypack24.net"; // sandbox
        opt.LabelFormat = "pdf";
    })
    .AddInPostCourier(opt =>
    {
        opt.BaseUrl     = "https://sandbox-api-shipx-pl.easypack24.net";
        opt.LabelFormat = "pdf";
    });
```

> Oba adaptery współdzielą `InPostHttpClient` i `InPostOptions` — konfiguracja
> jest idempotentna; opcje są rejestrowane tylko raz nawet przy jednoczesnym wywołaniu
> `AddInPostLocker` i `AddInPostCourier`.

## Obsługiwane operacje

| Operacja | Metoda API | Opis |
|---|---|---|
| `CreateShipmentAsync` | `POST /v1/organizations/{id}/shipments` | Rejestracja przesyłki, zwraca etykietę PDF |
| `OrderPickupAsync` | `POST /v1/organizations/{id}/dispatch_orders` | Dyspozycja odbioru przesyłek przez kuriera |
| `TrackShipmentAsync` | `GET /v1/tracking/{trackingNumber}` | Status i historia przesyłki |
| `GetDeliveryConfirmationAsync` | `GET /v1/tracking/{trackingNumber}` | Potwierdzenie doręczenia (POD) |

## Kody usług (`ServiceCode`)

Ustaw `CreateShipmentRequest.ServiceCode`, aby wybrać wariant usługi.
Gdy `ServiceCode` jest `null`, InPost ShipX API użyje usługi domyślnej dla danego trybu.

### Paczkomat (`CarrierType.InPostLocker`)

| Kod | Opis |
|---|---|
| `inpost_locker_standard` | Paczkomat — standard (domyślny) |
| `inpost_locker_next` | Paczkomat — ekspres (next day) |

### Kurier door-to-door (`CarrierType.InPostCourier`)

| Kod | Opis |
|---|---|
| `inpost_courier_standard` | Kurier standard (domyślny) |
| `inpost_courier_express` | Kurier ekspresowy |
| `inpost_courier_c2c` | Kurier C2C (klient do klienta) |

## Przykłady użycia

### Paczkomat — dostawa do wybranego paczkomatu

```csharp
var result = await _shipping.CreateShipmentAsync(CarrierType.InPostLocker, new CreateShipmentRequest
{
    AuthInfo = new InPostAuthInfo
    {
        AccessToken    = tenantSettings.InPostToken,
        OrganizationId = tenantSettings.InPostOrgId,
    },
    LockerTargetMachineId = "WAW123M",  // wymagane — identyfikator paczkomatu
    ServiceCode = "inpost_locker_standard",

    SenderAddress = new Address
    {
        Name       = "Firma Sp. z o.o.",
        Street     = "ul. Magazynowa 1",
        PostalCode = "00-001",
        City       = "Warszawa",
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
        Street     = "ul. Kwiatowa 5/3",   // dla paczkomatu może być adres odbiorcy
        PostalCode = "30-001",
        City       = "Kraków",
    },
    RecipientContact = new ContactInfo
    {
        Name  = "Anna Nowak",
        Phone = "+48987654321",
        Email = "anna@example.com",         // e-mail wymagany — SMS/e-mail z kodem odbioru
    },
    Parcels = new List<ParcelDimensions>
    {
        new() { WeightKg = 1.5m, LengthCm = 38, WidthCm = 64, HeightCm = 41 }, // rozmiar A
    },
    Reference = "ZAMOWIENIE-2024-001",
});
```

### Kurier — dostawa pod wskazany adres

```csharp
var result = await _shipping.CreateShipmentAsync(CarrierType.InPostCourier, new CreateShipmentRequest
{
    AuthInfo = new InPostAuthInfo
    {
        AccessToken    = tenantSettings.InPostToken,
        OrganizationId = tenantSettings.InPostOrgId,
    },
    ServiceCode = "inpost_courier_standard",

    SenderAddress = new Address
    {
        Name       = "Firma Sp. z o.o.",
        Street     = "ul. Magazynowa 1",
        PostalCode = "00-001",
        City       = "Warszawa",
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
        Street     = "ul. Kwiatowa 5",
        BuildingNumber = "3",
        PostalCode = "30-001",
        City       = "Kraków",
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
});
```

## Uwagi

- Etykieta jest zwracana inline w odpowiedzi na `CreateShipmentAsync` (brak osobnego endpointu).
- `LockerTargetMachineId` jest **wymagane** dla `CarrierType.InPostLocker` — adapter rzuci
  `InvalidOperationException` jeśli brakuje tego pola.
- Dla usług paczkomatowych `RecipientContact.Email` powinien być wypełniony —
  InPost wysyła na niego powiadomienie z kodem odbioru.
- W żądaniu odbioru (`OrderPickupAsync`) `ShipmentIds` powinny zawierać numery śledzenia
  przesyłek wcześniej zarejestrowanych w tej samej sesji/dniu.
- API InPost nie obsługuje standardowego POD z podpisem — `GetDeliveryConfirmationAsync`
  zwraca datę i status doręczenia na podstawie trackingu.
- Identyfikator paczkomatu można uzyskać z oficjalnej mapy: https://inpost.pl/znajdz-paczkomat
