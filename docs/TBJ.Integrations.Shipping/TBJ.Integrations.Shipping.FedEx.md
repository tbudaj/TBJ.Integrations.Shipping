# TBJ.Integrations.Shipping.FedEx

Adapter integracji z **FedEx Web Services REST API** — usługi kurierskie FedEx.

- **Protokół:** REST / JSON over HTTPS
- **Autentykacja:** OAuth 2.0 — Client Credentials Flow (token odnawiany automatycznie) — dane przekazywane per-żądanie przez `FedExAuthInfo`
- **API:** FedEx REST API v1

## Jak uzyskać dane dostępowe

### 1. Rejestracja konta FedEx Developer

1. Przejdź na https://developer.fedex.com
2. Kliknij **Create an Account** i zarejestruj się
3. Powiąż konto z istniejącym kontem wysyłkowym FedEx (lub utwórz nowe)

### 2. Utworzenie projektu (Client ID / Client Secret)

1. Po zalogowaniu przejdź do **My Projects → Create a Project**
2. Wybierz API, z których chcesz korzystać (Ship API, Track API, Pickup API)
3. Powiąż projekt z numerem konta FedEx
4. Po zapisaniu otrzymasz:
   - **API Key (Client ID)** — identyfikator aplikacji OAuth2
   - **Secret Key (Client Secret)** — klucz tajny (wyświetlany tylko raz — zapisz!)

### 3. Numer konta FedEx (Account Number)

- Numer konta widoczny w panelu FedEx → **My Profile** → **Account List**
- Format: 9-cyfrowy numeryczny, np. `123456789`
- Używany do rozliczania i tworzenia przesyłek

### 4. Środowisko testowe (Sandbox)

FedEx udostępnia pełne środowisko **Sandbox**:
- Osobny zestaw Client ID / Client Secret dla Sandbox (tworzony oddzielnie w panelu)
- Sandbox umożliwia pełne testowanie bez realnych opłat
- Konta sandbox tworzone są automatycznie — brak konieczności kontaktu z FedEx

> Dane te (`ClientId`, `ClientSecret`, `AccountNumber`) mogą być przekazywane per-żądanie (Scenariusz A)
> lub skonfigurowane jako domyślne w `appsettings.json` pod kluczami `Default*` (Scenariusz B — patrz sekcja niżej).

## Środowiska

| Środowisko | BaseUrl | TokenUrl |
|---|---|---|
| **Produkcyjne** | `https://apis.fedex.com` | `https://apis.fedex.com/oauth/token` |
| **Sandbox (testowe)** | `https://apis-sandbox.fedex.com` | `https://apis-sandbox.fedex.com/oauth/token` |

## Model uwierzytelniania (wielotenantowość)

Adapter obsługuje dwa scenariusze dostarczania credentials:

### Scenariusz A — konto tenanta (credentials per-request)

Credentials pobierane z kontekstu tenanta i przekazywane w `FedExAuthInfo` przy każdym żądaniu. Token OAuth2 cache'owany per `ClientId`.

```csharp
var request = new CreateShipmentRequest
{
    AuthInfo = new FedExAuthInfo
    {
        ClientId      = tenantSettings.FedExClientId,
        ClientSecret  = tenantSettings.FedExClientSecret,
        AccountNumber = tenantSettings.FedExAccountNumber,
    },
    // ...
};
```

### Scenariusz B — nasze konto (domyślne credentials z konfiguracji)

Gdy `AuthInfo` nie jest podane w żądaniu (`null`), adapter automatycznie użyje `Default*` z `FedExOptions`.

```jsonc
// appsettings.json — Scenariusz B
{
  "Shipping": {
    "FedEx": {
      "DefaultClientId":      "nasze_client_id",
      "DefaultClientSecret":  "nasze_client_secret",
      "DefaultAccountNumber": "123456789"
    }
  }
}
```

---

## Konfiguracja `appsettings.json`

```jsonc
// appsettings.json
{
  "Shipping": {
    "FedEx": {
      "BaseUrl":            "https://apis.fedex.com",
      "TokenUrl":           "https://apis.fedex.com/oauth/token",
      "Timeout":            "00:00:30",
      // Scenariusz B (opcjonalne — tylko gdy używamy własnego konta FedEx):
      "DefaultClientId":      "nasze_client_id",
      "DefaultClientSecret":  "nasze_client_secret",
      "DefaultAccountNumber": "123456789"
    }
  }
}
```

### Opis parametrów

| Parametr | Wymagany | Domyślna wartość | Opis |
|---|---|---|---|
| `BaseUrl` | nie | (produkcyjny j.w.) | Bazowy adres REST API |
| `TokenUrl` | nie | (produkcyjny j.w.) | Endpoint do pobierania tokenów OAuth2 |
| `Timeout` | nie | `00:00:30` | Timeout żądań HTTP (`hh:mm:ss`) |
| `DefaultClientId` | nie | `null` | Client ID naszego konta FedEx (Scenariusz B) |
| `DefaultClientSecret` | nie | `null` | Client Secret naszego konta FedEx (Scenariusz B) |
| `DefaultAccountNumber` | nie | `null` | Numer konta FedEx naszej firmy (Scenariusz B) |

### Typy usług FedEx (`DefaultServiceType`)

| Typ | Opis |
|---|---|
| `PRIORITY_OVERNIGHT` | FedEx Priority Overnight |
| `STANDARD_OVERNIGHT` | FedEx Standard Overnight |
| `FEDEX_2_DAY` | FedEx 2Day |
| `FEDEX_GROUND` | FedEx Ground |
| `INTERNATIONAL_PRIORITY` | FedEx International Priority |
| `INTERNATIONAL_ECONOMY` | FedEx International Economy (domyślny) |
| `EUROPE_FIRST_INTERNATIONAL_PRIORITY` | FedEx Europe First |
| `FEDEX_EXPRESS_SAVER` | FedEx Express Saver |

### Konfiguracja środowisk

```jsonc
// appsettings.Development.json — środowisko Sandbox FedEx
{
  "Shipping": {
    "FedEx": {
      "BaseUrl":  "https://apis-sandbox.fedex.com",
      "TokenUrl": "https://apis-sandbox.fedex.com/oauth/token"
    }
  }
}
```

## Rejestracja w DI

```csharp
// z appsettings
builder.Services
    .AddShippingGateway()
    .AddFedEx(builder.Configuration);

// lub inline — tylko infrastruktura, brak danych auth
builder.Services
    .AddShippingGateway()
    .AddFedEx(opt =>
    {
        // Sandbox:
        opt.BaseUrl  = "https://apis-sandbox.fedex.com";
        opt.TokenUrl = "https://apis-sandbox.fedex.com/oauth/token";
    });
```

## Dane uwierzytelniające

### Scenariusz A — credentials tenanta per-request

```csharp
var result = await _shipping.CreateShipmentAsync(CarrierType.FedEx, new CreateShipmentRequest
{
    AuthInfo = new FedExAuthInfo
    {
        ClientId      = tenantSettings.FedExClientId,
        ClientSecret  = tenantSettings.FedExClientSecret,
        AccountNumber = tenantSettings.FedExAccountNumber,
    },
    // ... pozostałe pola
});
```

### Scenariusz B — nasze konto (brak AuthInfo w żądaniu)

```csharp
// AuthInfo = null → adapter użyje DefaultClientId/DefaultClientSecret/DefaultAccountNumber z opcji
var result = await _shipping.CreateShipmentAsync(CarrierType.FedEx, new CreateShipmentRequest
{
    // AuthInfo nie ustawione — fallback na Default* z FedExOptions
    // ... pozostałe pola
});
```

| Pole FedExAuthInfo | Opis |
|---|---|
| `ClientId` | API Key (Client ID) z panelu developer.fedex.com |
| `ClientSecret` | Secret Key (Client Secret) aplikacji FedEx |
| `AccountNumber` | Numer konta FedEx (9 cyfr) |

## Obsługiwane operacje

| Operacja | Endpoint | Opis |
|---|---|---|
| `CreateShipmentAsync` | `POST /ship/v1/shipments` | Rejestracja przesyłki z etykietą |
| `OrderPickupAsync` | `POST /pickup/v1/pickups` | Dyspozycja odbioru |
| `TrackShipmentAsync` | `POST /track/v1/trackingnumbers` | Status i historia przesyłki |
| `GetDeliveryConfirmationAsync` | `POST /track/v1/trackingnumbers` | Potwierdzenie doręczenia (POD) |

## Mechanizm token cache (OAuth2)

Adapter automatycznie zarządza tokenami OAuth2:

- Token pobierany przy pierwszym żądaniu (Client Credentials Flow, form-encoded)
- Przechowywany w `FedExTokenCache` — singleton współdzielony między requestami
- Bufor bezpieczeństwa: token uznawany za wygasły **60 sekund przed** faktycznym wygaśnięciem
- Operacja thread-safe (`SemaphoreSlim`) — bezpieczna przy równoczesnych żądaniach
- Token wygasa po czasie podanym przez FedEx (typowo 3600 sekund = 1 godzina)

```
Żądanie → FedExTokenCache.GetTokenAsync()
              ├── token ważny? → zwróć istniejący
              └── wygasł/brak? → POST /oauth/token → zapisz → zwróć nowy
```

**Szczegół techniczny:** FedEx OAuth2 wymaga żądania tokenów jako `application/x-www-form-urlencoded`
(nie JSON) — adapter obsługuje to automatycznie.

## Uwagi

- FedEx REST API zastąpiło starsze SOAP Web Services (SOAPv6/v7) — adapter używa wyłącznie REST.
- `DefaultServiceType` używany gdy `CreateShipmentRequest.ServiceCode` jest puste.
- Etykieta zwracana w odpowiedzi jako Base64 (PDF lub PNG w zależności od żądania).
- FedEx Track API v1 używa POST (nie GET) nawet do pobierania statusu — adapter obsługuje to transparentnie.
- Środowisko sandbox wymaga osobnej rejestracji aplikacji w panelu developer.fedex.com.
