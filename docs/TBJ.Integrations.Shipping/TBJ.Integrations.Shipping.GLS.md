# TBJ.Integrations.Shipping.GLS

Adapter integracji z **GLS ADE-Plus WebAPI** — usługi kurierskie GLS Poland.

- **Protokół:** SOAP 1.1 / XML over HTTPS
- **Autentykacja:** Dane logowania w elemencie `session` każdego żądania SOAP — przekazywane per-żądanie przez `GlsAuthInfo`
- **API:** GLS ADE-Plus WebAPI (Automatyczny Data Exchange)

## Jak uzyskać dane dostępowe

### 1. Umowa z GLS Poland

Dostęp do ADE-Plus WebAPI wymaga umowy z GLS Poland.
Skontaktuj się z działem obsługi:
- **WWW:** https://gls-group.com/PL/pl/home
- **Portal klienta:** https://adeplus.gls-poland.com
- **Telefon:** 61 868 35 00

### 2. Dane dostępowe (po zawarciu umowy)

GLS przekazuje po zawarciu umowy:
- **Username** — login do systemu ADE-Plus (zazwyczaj numer klienta lub e-mail)
- **Password** — hasło do systemu ADE-Plus

Dane można zweryfikować logując się na https://adeplus.gls-poland.com

> Dane te (`Username`, `Password`) mogą być przekazywane **per-żądanie** przez `GlsAuthInfo` (Scenariusz A) lub skonfigurowane jako domyślne w `appsettings.json` (Scenariusz B).

### 3. Środowisko testowe

GLS Poland udostępnia środowisko testowe ADE-Plus na żądanie.
Skontaktuj się z opiekunem klienta GLS, aby uzyskać:
- dane logowania testowego
- adres URL środowiska testowego

Typowy adres testowy: `https://adeplus-test.gls-poland.com/adeplus/pm1/ade_webapi.php`

## Środowiska

| Środowisko | BaseUrl |
|---|---|
| **Produkcyjne** | `https://adeplus.gls-poland.com/adeplus/pm1/ade_webapi.php` |
| **Testowe** | `https://adeplus-test.gls-poland.com/adeplus/pm1/ade_webapi.php` *(na żądanie)* |

## Model uwierzytelniania (wielotenantowość)

### Scenariusz A — credentials per-żądanie (wielotenantowy)

Każde żądanie zawiera dane logowania konkretnego tenanta. Używaj gdy obsługujesz wielu klientów,
z których każdy posiada własne konto GLS ADE-Plus.

```csharp
var authInfo = new GlsAuthInfo
{
    Username = tenantSettings.GlsUsername,
    Password = tenantSettings.GlsPassword,
};

var request = new CreateShipmentRequest
{
    AuthInfo = authInfo,
    // ... pozostałe pola
};

var result = await _shipping.CreateShipmentAsync(CarrierType.GLS, request);
```

### Scenariusz B — domyślne credentials z konfiguracji (single-tenant)

Gdy obsługujesz tylko jedno konto GLS (np. własna firma), możesz skonfigurować credentials
w `appsettings.json`. Żądania bez `GlsAuthInfo` automatycznie użyją tych danych.

```csharp
// Program.cs — konfiguracja
builder.Services
    .AddShippingGateway()
    .AddGLS(builder.Configuration); // DefaultUsername i DefaultPassword z appsettings

// Użycie — bez AuthInfo
var request = new CreateShipmentRequest
{
    AuthInfo = null, // użyje DefaultUsername/DefaultPassword z GlsOptions
    // ... pozostałe pola
};

var result = await _shipping.CreateShipmentAsync(CarrierType.GLS, request);
```

## Konfiguracja `appsettings.json`

```jsonc
{
  "Shipping": {
    "GLS": {
      "BaseUrl":     "https://adeplus.gls-poland.com/adeplus/pm1/ade_webapi.php",
      "Timeout":     "00:01:00",
      "CountryCode": "PL",
      // Scenariusz B: domyślne credentials naszego konta (opcjonalne)
      "DefaultUsername": "moja_nazwa_uzytkownika_gls",
      "DefaultPassword": "moje_haslo_gls"
    }
  }
}
```

### Opis parametrów

| Parametr | Wymagany | Domyślna wartość | Opis |
|---|---|---|---|
| `BaseUrl` | nie | (produkcyjny j.w.) | Adres endpointu SOAP ADE-Plus WebAPI |
| `Timeout` | nie | `00:01:00` | Timeout żądań SOAP (`hh:mm:ss`) |
| `CountryCode` | nie | `PL` | Kod kraju nadawcy (ISO 3166-1 alfa-2) |
| `DefaultUsername` | nie | — | Scenariusz B: login konta GLS ADE-Plus |
| `DefaultPassword` | nie | — | Scenariusz B: hasło konta GLS ADE-Plus |

### Konfiguracja środowisk

```jsonc
// appsettings.Development.json — środowisko testowe GLS
{
  "Shipping": {
    "GLS": {
      "BaseUrl": "https://adeplus-test.gls-poland.com/adeplus/pm1/ade_webapi.php"
    }
  }
}
```

## Rejestracja w DI

```csharp
// z appsettings
builder.Services
    .AddShippingGateway()
    .AddGLS(builder.Configuration);

// lub inline — tylko infrastruktura, brak danych auth
builder.Services
    .AddShippingGateway()
    .AddGLS(opt =>
    {
        opt.BaseUrl     = "https://adeplus-test.gls-poland.com/adeplus/pm1/ade_webapi.php"; // testowe
        opt.CountryCode = "PL";
    });
```

## Dane uwierzytelniające

Patrz sekcja [Model uwierzytelniania](#model-uwierzytelniania-wielotenantowość) powyżej.

Pola obiektu `GlsAuthInfo` (Scenariusz A):

| Pole | Opis |
|---|---|
| `Username` | Login do systemu GLS ADE-Plus |
| `Password` | Hasło do systemu GLS ADE-Plus |

## Obsługiwane operacje

| Operacja | Akcja SOAP | Opis |
|---|---|---|
| `CreateShipmentAsync` | `adePrepareConsignments` + `adeGetConsignLabels` | Rejestracja przesyłki i pobranie etykiety |
| `OrderPickupAsync` | `adePickup_CallPickup` | Dyspozycja odbioru przez kuriera |
| `TrackShipmentAsync` | `adeGetConsignStatus` | Status i historia przesyłki |
| `GetDeliveryConfirmationAsync` | `adeGetConsignStatus` | Potwierdzenie doręczenia na podstawie statusu |

## Uwagi

- GLS ADE-Plus używa własnego formatu SOAP — żądania są budowane ręcznie jako XML
  (GLS nie udostępnia standardowego WSDL dla WS-Security).
- Etykiety pobierane są w osobnym wywołaniu `adeGetConsignLabels` po rejestracji.
- `CountryCode` musi odpowiadać krajowi, z którego nadajesz — najczęściej `PL`.
- Dane logowania wysyłane są przy każdym żądaniu w sekcji `session` — brak tokenów sesyjnych.
- POD (`GetDeliveryConfirmationAsync`) oparty o status `adeGetConsignStatus` — GLS ADE-Plus
  nie oferuje osobnego endpointu ePOD z dokumentem PDF.
