# TBJ.Integrations.Shipping.DHL

Adapter integracji z **DHL24 WebAPI2** — usługi kurierskie DHL Polska.

- **Protokół:** SOAP 1.1 / XML over HTTPS
- **Autentykacja:** WS-Security — login i hasło w elemencie `authData` każdego żądania SOAP — przekazywane per-żądanie przez `DhlAuthInfo`
- **API:** DHL24 WebAPI2

## Jak uzyskać dane dostępowe

### 1. Umowa z DHL Polska

Dostęp do WebAPI2 wymaga umowy z DHL Polska.
Skontaktuj się z działem obsługi klienta DHL:
- **WWW:** https://www.dhl.com/pl-pl/home/dla-biznesu.html
- **Telefon:** 42 6345 600
- **Portal klienta:** https://www.dhl24.com.pl

### 2. Dane dostępowe (po zawarciu umowy)

Po zarejestrowaniu się w portalu DHL24 i zawarciu umowy:
- **Login** — nazwa użytkownika konta DHL24 (nadana przez DHL)
- **Password** — hasło do konta DHL24 (ustawiane przez użytkownika w portalu)

Zarządzanie kontem API: https://www.dhl24.com.pl → _Moje konto_ → _API / WebAPI2_

> Dane te (`Login`, `Password`) mogą być przekazywane **per-żądanie** przez `DhlAuthInfo` (Scenariusz A) lub skonfigurowane jako domyślne w `appsettings.json` (Scenariusz B).

### 3. Środowisko testowe

DHL udostępnia oddzielny endpoint testowy.
Dane do środowiska testowego uzyskasz kontaktując się z opiekunem klienta DHL lub przez:
https://www.dhl24.com.pl/webapi2 (zakładka _Dokumentacja_ → dane testowe)

Typowe dane testowe DHL24 (publicznie dostępne w dokumentacji DHL):
- Login: `testwebapi`
- Password: `testwebapi`

## Środowiska

| Środowisko | BaseUrl |
|---|---|
| **Produkcyjne** | `https://dhl24.com.pl/webapi2/provider/service.html?ws=1` |
| **Testowe** | `https://sandbox.dhl24.com.pl/webapi2/provider/service.html?ws=1` |

## Model uwierzytelniania (wielotenantowość)

### Scenariusz A — credentials per-żądanie (wielotenantowy)

Każde żądanie zawiera dane logowania konkretnego tenanta. Używaj gdy obsługujesz wielu klientów,
z których każdy posiada własne konto DHL24.

```csharp
var authInfo = new DhlAuthInfo
{
    Login    = tenantSettings.DhlLogin,
    Password = tenantSettings.DhlPassword,
};

var request = new CreateShipmentRequest
{
    AuthInfo = authInfo,
    // ... pozostałe pola
};

var result = await _shipping.CreateShipmentAsync(CarrierType.DHL, request);
```

### Scenariusz B — domyślne credentials z konfiguracji (single-tenant)

Gdy obsługujesz tylko jedno konto DHL (np. własna firma), możesz skonfigurować credentials
w `appsettings.json`. Żądania bez `DhlAuthInfo` automatycznie użyją tych danych.

```csharp
// Program.cs — konfiguracja
builder.Services
    .AddShippingGateway()
    .AddDHL(builder.Configuration); // DefaultLogin i DefaultPassword z appsettings

// Użycie — bez AuthInfo
var request = new CreateShipmentRequest
{
    AuthInfo = null, // użyje DefaultLogin/DefaultPassword z DhlOptions
    // ... pozostałe pola
};

var result = await _shipping.CreateShipmentAsync(CarrierType.DHL, request);
```

## Konfiguracja `appsettings.json`

```jsonc
{
  "Shipping": {
    "DHL": {
      "BaseUrl":            "https://dhl24.com.pl/webapi2/provider/service.html?ws=1",
      "Timeout":            "00:01:00",
      "DefaultServiceType": "AH",
      // Scenariusz B: domyślne credentials naszego konta (opcjonalne)
      "DefaultLogin":    "moj_login_dhl24",
      "DefaultPassword": "moje_haslo_dhl24"
    }
  }
}
```

### Opis parametrów

| Parametr | Wymagany | Domyślna wartość | Opis |
|---|---|---|---|
| `BaseUrl` | nie | (produkcyjny j.w.) | Adres endpointu SOAP WebAPI2 |
| `Timeout` | nie | `00:01:00` | Timeout żądań SOAP (`hh:mm:ss`) |
| `DefaultServiceType` | nie | `AH` | Domyślny kod usługi (patrz tabela poniżej) |
| `DefaultLogin` | nie | — | Scenariusz B: login konta DHL24 |
| `DefaultPassword` | nie | — | Scenariusz B: hasło konta DHL24 |

### Kody usług DHL (`DefaultServiceType`)

| Kod | Opis |
|---|---|
| `AH` | Domestic (krajowa standard) — domyślny |
| `09` | DHL 9:00 (doręczenie do 9:00) |
| `12` | DHL 12:00 (doręczenie do 12:00) |
| `EK` | DHL Connect (ekonomiczna) |
| `SP` | DHL Parcel Connect (międzynarodowa) |

### Konfiguracja środowisk

```jsonc
// appsettings.Development.json — środowisko testowe DHL
{
  "Shipping": {
    "DHL": {
      "BaseUrl": "https://sandbox.dhl24.com.pl/webapi2/provider/service.html?ws=1"
    }
  }
}
```

## Rejestracja w DI

```csharp
// z appsettings
builder.Services
    .AddShippingGateway()
    .AddDHL(builder.Configuration);

// lub inline — tylko infrastruktura, brak danych auth
builder.Services
    .AddShippingGateway()
    .AddDHL(opt =>
    {
        opt.BaseUrl = "https://sandbox.dhl24.com.pl/webapi2/provider/service.html?ws=1"; // testowe
    });
```

## Dane uwierzytelniające

Patrz sekcja [Model uwierzytelniania](#model-uwierzytelniania-wielotenantowość) powyżej.

Pola obiektu `DhlAuthInfo` (Scenariusz A):

| Pole | Opis |
|---|---|
| `Login` | Nazwa użytkownika konta DHL24 |
| `Password` | Hasło konta DHL24 |

## Obsługiwane operacje

| Operacja | Akcja SOAP | Opis |
|---|---|---|
| `CreateShipmentAsync` | `createShipments` | Rejestracja przesyłki z etykietą PDF |
| `OrderPickupAsync` | `bookCourier` | Dyspozycja odbioru przez kuriera |
| `TrackShipmentAsync` | `getTrackAndTraceInfo` | Status i historia przesyłki |
| `GetDeliveryConfirmationAsync` | `getEpod` | Elektroniczne potwierdzenie doręczenia (ePOD) |

## Uwagi

- DHL24 zwraca etykietę bezpośrednio w odpowiedzi `createShipments` jako Base64.
- `getEpod` zwraca dokument PDF z podpisem odbiorcy — dostępny po faktycznym doręczeniu.
- `bookCourier` wymaga podania okna czasowego odbioru — jeśli nie podano `PickupTimeFrom`/`PickupTimeTo`,
  adapter używa domyślnego okna 08:00–18:00.
- SOAP WS-Security bez cache sesji — dane logowania wysyłane przy każdym żądaniu.
