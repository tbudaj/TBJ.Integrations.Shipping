# TBJ.Integrations.Shipping.DPD

Adapter integracji z **DPD Polska Web Service** — usługi kurierskie DPD.

- **Protokół:** SOAP 1.1 / XML over HTTPS
- **Autentykacja:** WS-Security — login i hasło w elemencie `authDataV1` każdego żądania SOAP — przekazywane per-żądanie przez `DpdAuthInfo`
- **API:** DPD Web Service (DPDPackageObjServices)

## Jak uzyskać dane dostępowe

### 1. Umowa z DPD Polska

Dostęp do Web Service wymaga podpisanej umowy z DPD Polska.
Skontaktuj się z opiekunem DPD lub działem handlowym:
- **WWW:** https://www.dpd.com.pl/dla-biznesu
- **Telefon:** 22 577 57 57

### 2. Dane dostępowe (po zawarciu umowy)

Po zawarciu umowy DPD przekazuje:
- **Username** — adres e-mail lub login konta w portalu DPD
- **Password** — hasło do konta DPD
- **FID** (Facility ID) — numer punktu/oddziału, z którego nadajesz przesyłki;
  widoczny w portalu https://dpd.com.pl → _Moje konto_ → _Informacje o koncie_

> Dane te (`Username`, `Password`, `Fid`) mogą być przekazywane per-żądanie (Scenariusz A)
> lub skonfigurowane jako domyślne w `appsettings.json` pod kluczami `Default*` (Scenariusz B — patrz sekcja niżej).

### 3. Środowisko testowe

DPD udostępnia środowisko testowe na żądanie — poproś opiekuna klienta o:
- dane logowania do środowiska test
- adres URL testowego Web Service

## Środowiska

| Środowisko | BaseUrl |
|---|---|
| **Produkcyjne** | `https://dpdservices.dpd.com.pl/DPDPackageObjServicesService/DPDPackageObjServices` |
| **Testowe** | podawany indywidualnie przez DPD (np. `https://dpdservices.dpd.com.pl/DPDPackageObjServicesService/DPDPackageObjServices?wsdl`) |

## Model uwierzytelniania (wielotenantowość)

Adapter obsługuje dwa scenariusze dostarczania credentials:

### Scenariusz A — konto tenanta (credentials per-request)

Credentials pobierane z kontekstu tenanta i przekazywane w `DpdAuthInfo` przy każdym żądaniu.

```csharp
var request = new CreateShipmentRequest
{
    AuthInfo = new DpdAuthInfo
    {
        Username = tenantSettings.DpdUsername,
        Password = tenantSettings.DpdPassword,
        Fid      = tenantSettings.DpdFid,
    },
    // ...
};
```

### Scenariusz B — nasze konto (domyślne credentials z konfiguracji)

Gdy `AuthInfo` nie jest podane w żądaniu (`null`), adapter automatycznie użyje `Default*` z `DpdOptions`.

```jsonc
// appsettings.json — Scenariusz B
{
  "Shipping": {
    "DPD": {
      "DefaultUsername": "nasz_login@firma.pl",
      "DefaultPassword": "nasze_haslo",
      "DefaultFid":      12345
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
    "DPD": {
      "BaseUrl":         "https://dpdservices.dpd.com.pl/DPDPackageObjServicesService/DPDPackageObjServices",
      "Timeout":         "00:01:00",
      "LabelFormat":     "PDF",
      "LabelPageFormat": "A4",
      // Scenariusz B (opcjonalne — tylko gdy używamy własnego konta DPD):
      "DefaultUsername": "nasz_login@firma.pl",
      "DefaultPassword": "nasze_haslo",
      "DefaultFid":      12345
    }
  }
}
```

### Opis parametrów

| Parametr | Wymagany | Domyślna wartość | Opis |
|---|---|---|---|
| `BaseUrl` | nie | (produkcyjny j.w.) | Adres endpointu SOAP Web Service |
| `Timeout` | nie | `00:01:00` | Timeout żądań SOAP (`hh:mm:ss`) |
| `LabelFormat` | nie | `PDF` | Format etykiety: `PDF`, `ZPL` |
| `LabelPageFormat` | nie | `A4` | Format strony: `A4`, `A5`, `A6` |
| `DefaultUsername` | nie | `null` | Login naszego konta DPD (Scenariusz B) |
| `DefaultPassword` | nie | `null` | Hasło naszego konta DPD (Scenariusz B) |
| `DefaultFid` | nie | `null` | Facility ID naszego konta DPD (Scenariusz B) |

### Konfiguracja środowisk

```jsonc
// appsettings.Development.json — środowisko testowe DPD
{
  "Shipping": {
    "DPD": {
      "BaseUrl": "https://dpdservices-test.dpd.com.pl/..."
    }
  }
}
```

## Rejestracja w DI

```csharp
// z appsettings
builder.Services
    .AddShippingGateway()
    .AddDPD(builder.Configuration);

// lub inline — tylko infrastruktura, brak danych auth
builder.Services
    .AddShippingGateway()
    .AddDPD(opt =>
    {
        opt.BaseUrl         = "https://dpdservices-test.dpd.com.pl/..."; // testowe
        opt.LabelFormat     = "PDF";
        opt.LabelPageFormat = "A4";
    });
```

## Dane uwierzytelniające

### Scenariusz A — credentials tenanta per-request

```csharp
var result = await _shipping.CreateShipmentAsync(CarrierType.DPD, new CreateShipmentRequest
{
    AuthInfo = new DpdAuthInfo
    {
        Username = tenantSettings.DpdUsername,
        Password = tenantSettings.DpdPassword,
        Fid      = tenantSettings.DpdFid,
    },
    // ... pozostałe pola
});
```

### Scenariusz B — nasze konto (brak AuthInfo w żądaniu)

```csharp
// AuthInfo = null → adapter użyje DefaultUsername/DefaultPassword/DefaultFid z opcji
var result = await _shipping.CreateShipmentAsync(CarrierType.DPD, new CreateShipmentRequest
{
    // AuthInfo nie ustawione — fallback na Default* z DpdOptions
    // ... pozostałe pola
});
```

| Pole DpdAuthInfo | Opis |
|---|---|
| `Username` | Login/e-mail konta DPD |
| `Password` | Hasło konta DPD |
| `Fid` | Facility ID — numer punktu odbioru |

## Obsługiwane operacje

| Operacja | Akcja SOAP | Opis |
|---|---|---|
| `CreateShipmentAsync` | `generatePackagesNumbersV1` + `generateSpedLabelsV1` | Rejestracja paczek i pobranie etykiet |
| `OrderPickupAsync` | `packagesPickupCallV1` | Dyspozycja odbioru |
| `TrackShipmentAsync` | `findPackageStatusV1` | Status i historia przesyłki |
| `GetDeliveryConfirmationAsync` | `findPackageStatusV1` | Potwierdzenie doręczenia |

## Uwagi

- Etykiety pobierane są w osobnym wywołaniu zaraz po rejestracji paczek.
- `Fid` jest obowiązkowy i musi odpowiadać lokalizacji, z której nadajesz przesyłki.
- SOAP WS-Security: login i hasło są wysyłane w każdym żądaniu w sekcji `authDataV1` —
  brak tokenów sesyjnych ani cache'u.
- Limity rozmiaru przesyłki zgodne z cennikiem DPD — adapter nie waliduje wymiarów po stronie klienta.
