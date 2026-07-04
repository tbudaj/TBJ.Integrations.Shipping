# TBJ.Integrations.Shipping.UPS

Adapter integracji z **UPS Developer API** — usługi kurierskie UPS.

- **Protokół:** REST / JSON over HTTPS
- **Autentykacja:** OAuth 2.0 — Client Credentials Flow (token odnawiany automatycznie) — dane przekazywane per-żądanie przez `UpsAuthInfo`
- **API:** UPS Developer APIs (v2403)

## Jak uzyskać dane dostępowe

### 1. Rejestracja konta UPS Developer

1. Przejdź na https://developer.ups.com
2. Kliknij **Sign Up** i zarejestruj się lub zaloguj kontem UPS.com
3. Po zalogowaniu przejdź do **My Apps → Add Apps**

### 2. Utworzenie aplikacji (Client ID / Client Secret)

1. W panelu **My Apps** kliknij **Add Apps**
2. Powiąż aplikację z kontem wysyłkowym UPS (shipper account)
3. Po zapisaniu otrzymasz:
   - **Client ID** — identyfikator aplikacji OAuth2
   - **Client Secret** — klucz tajny aplikacji OAuth2 (wyświetlany tylko raz — zapisz!)
4. Dostęp do API produkcyjnego wymaga dodatkowej weryfikacji przez UPS (Production Access Agreement)

### 3. Numer konta UPS (Account Number / Shipper Number)

- Numer konta widoczny w panelu UPS → **My Account** → **Account Summary**
- Format: 6-znakowy alfanumeryczny, np. `A1B2C3`
- Używany do rozliczania i tworzenia przesyłek

### 4. Środowisko testowe (CIE)

UPS udostępnia środowisko **Customer Integration Environment (CIE)**:
- Osobny zestaw Client ID / Client Secret dla CIE (utwórz oddzielną aplikację w panelu)
- Nie generuje realnych przesyłek ani opłat

> Dane te (`ClientId`, `ClientSecret`, `AccountNumber`) mogą być przekazywane **per-żądanie** przez `UpsAuthInfo` (Scenariusz A) lub skonfigurowane jako domyślne w `appsettings.json` (Scenariusz B).

## Środowiska

| Środowisko | BaseUrl | TokenUrl |
|---|---|---|
| **Produkcyjne** | `https://onlinetools.ups.com/api` | `https://onlinetools.ups.com/security/v1/oauth/token` |
| **Testowe (CIE)** | `https://wwwcie.ups.com/api` | `https://wwwcie.ups.com/security/v1/oauth/token` |

## Model uwierzytelniania (wielotenantowość)

### Scenariusz A — credentials per-żądanie (wielotenantowy)

Każde żądanie zawiera dane logowania konkretnego tenanta. Używaj gdy obsługujesz wielu klientów,
z których każdy posiada własne konto UPS Developer.

```csharp
var authInfo = new UpsAuthInfo
{
    ClientId      = tenantSettings.UpsClientId,
    ClientSecret  = tenantSettings.UpsClientSecret,
    AccountNumber = tenantSettings.UpsAccountNumber,
};

var request = new CreateShipmentRequest
{
    AuthInfo = authInfo,
    // ... pozostałe pola
};

var result = await _shipping.CreateShipmentAsync(CarrierType.UPS, request);
```

### Scenariusz B — domyślne credentials z konfiguracji (single-tenant)

Gdy obsługujesz tylko jedno konto UPS (np. własna firma), możesz skonfigurować credentials
w `appsettings.json`. Żądania bez `UpsAuthInfo` automatycznie użyją tych danych.

```csharp
// Program.cs — konfiguracja
builder.Services
    .AddShippingGateway()
    .AddUPS(builder.Configuration); // DefaultClientId, DefaultClientSecret, DefaultAccountNumber z appsettings

// Użycie — bez AuthInfo
var request = new CreateShipmentRequest
{
    AuthInfo = null, // użyje DefaultClientId/DefaultClientSecret/DefaultAccountNumber z UpsOptions
    // ... pozostałe pola
};

var result = await _shipping.CreateShipmentAsync(CarrierType.UPS, request);
```

## Konfiguracja `appsettings.json`

```jsonc
{
  "Shipping": {
    "UPS": {
      "BaseUrl":  "https://onlinetools.ups.com/api",
      "TokenUrl": "https://onlinetools.ups.com/security/v1/oauth/token",
      "Timeout":  "00:00:30",
      // Scenariusz B: domyślne credentials naszego konta (opcjonalne)
      "DefaultClientId":      "moj_client_id_ups",
      "DefaultClientSecret":  "moj_client_secret_ups",
      "DefaultAccountNumber": "A1B2C3"
    }
  }
}
```

### Opis parametrów

| Parametr | Wymagany | Domyślna wartość | Opis |
|---|---|---|---|
| `BaseUrl` | nie | (produkcyjny j.w.) | Bazowy adres API |
| `TokenUrl` | nie | (produkcyjny j.w.) | Endpoint do pobierania tokenów OAuth2 |
| `Timeout` | nie | `00:00:30` | Timeout żądań HTTP (`hh:mm:ss`) |
| `DefaultClientId` | nie | — | Scenariusz B: Client ID aplikacji OAuth2 |
| `DefaultClientSecret` | nie | — | Scenariusz B: Client Secret aplikacji OAuth2 |
| `DefaultAccountNumber` | nie | — | Scenariusz B: numer konta UPS (Shipper Number) |

### Kody usług UPS (`DefaultServiceCode`)

| Kod | Opis |
|---|---|
| `01` | UPS Next Day Air |
| `02` | UPS 2nd Day Air |
| `03` | UPS Ground (domyślny) |
| `07` | UPS Express (międzynarodowy) |
| `08` | UPS Expedited (międzynarodowy) |
| `11` | UPS Standard |
| `12` | UPS 3 Day Select |
| `13` | UPS Next Day Air Saver |
| `65` | UPS Express Saver |

### Konfiguracja środowisk

```jsonc
// appsettings.Development.json — środowisko testowe CIE
{
  "Shipping": {
    "UPS": {
      "BaseUrl":  "https://wwwcie.ups.com/api",
      "TokenUrl": "https://wwwcie.ups.com/security/v1/oauth/token"
    }
  }
}
```

## Rejestracja w DI

```csharp
// z appsettings
builder.Services
    .AddShippingGateway()
    .AddUPS(builder.Configuration);

// lub inline — tylko infrastruktura, brak danych auth
builder.Services
    .AddShippingGateway()
    .AddUPS(opt =>
    {
        // Sandbox CIE:
        opt.BaseUrl  = "https://wwwcie.ups.com/api";
        opt.TokenUrl = "https://wwwcie.ups.com/security/v1/oauth/token";
    });
```

## Dane uwierzytelniające

Patrz sekcja [Model uwierzytelniania](#model-uwierzytelniania-wielotenantowość) powyżej.

Pola obiektu `UpsAuthInfo` (Scenariusz A):

| Pole | Opis |
|---|---|
| `ClientId` | Client ID aplikacji OAuth2 z panelu developer.ups.com |
| `ClientSecret` | Client Secret aplikacji OAuth2 |
| `AccountNumber` | Numer konta UPS (Shipper Number, 6 znaków alfanumerycznych) |

## Obsługiwane operacje

| Operacja | Endpoint | Opis |
|---|---|---|
| `CreateShipmentAsync` | `POST /shipments/v2403/ship` | Rejestracja przesyłki z etykietą |
| `OrderPickupAsync` | `POST /pickups/v2205/pickup` | Dyspozycja odbioru |
| `TrackShipmentAsync` | `GET /track/v1/details/{trackingNumber}` | Status i historia przesyłki |
| `GetDeliveryConfirmationAsync` | `GET /track/v1/details/{trackingNumber}` | Potwierdzenie doręczenia (POD) |

## Mechanizm token cache (OAuth2)

Adapter automatycznie zarządza tokenami OAuth2 — nie ma potrzeby ręcznego odnawiania:

- Token pobierany jest przy pierwszym żądaniu (Client Credentials Flow)
- Przechowywany w `UpsTokenCache` — singleton współdzielony między requestami
- Bufor bezpieczeństwa: token uznawany za wygasły **60 sekund przed** faktycznym wygaśnięciem
- Operacja thread-safe (`SemaphoreSlim`) — bezpieczna przy równoczesnych żądaniach
- Token wygasa po czasie podanym przez UPS (typowo 3600 sekund = 1 godzina)

```
Żądanie → UpsTokenCache.GetTokenAsync()
              ├── token ważny? → zwróć istniejący
              └── wygasł/brak? → POST /oauth/token → zapisz → zwróć nowy
```

## Uwagi

- UPS wymaga Production Access Agreement przed udzieleniem dostępu do środowiska produkcyjnego.
  Wniosek składa się w panelu https://developer.ups.com po przetestowaniu integracji w CIE.
- `DefaultServiceCode` używany gdy `CreateShipmentRequest.ServiceCode` jest puste.
- Etykieta zwracana w odpowiedzi jako Base64 PDF/GIF/ZPL.
