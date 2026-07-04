# TBJ.Integrations.Shipping

Jednolita bramka kurierska dla platformy TBJ. Pakiet ujednolica komunikację z InPost, DPD, DHL, GLS, UPS i FedEx poprzez jeden interfejs `IShippingGateway`.

## Wspierani kurierzy

| Kurier | Adapter | Protokół |
|---|---|---|
| InPost Paczkomat | `InPostLockerShippingClient` | REST |
| InPost Kurier | `InPostCourierShippingClient` | REST |
| DPD | `DpdShippingClient` | SOAP |
| DHL | `DhlShippingClient` | SOAP |
| GLS | `GlsShippingClient` | SOAP |
| UPS | `UpsShippingClient` | REST |
| FedEx | `FedExShippingClient` | REST |

## Instalacja

```bash
dotnet add package TBJ.Integrations.Shipping
```

## Podstawowa konfiguracja

```csharp
builder.Services
    .AddShippingGateway()
    .AddInPostLocker(builder.Configuration)
    .AddInPostCourier(builder.Configuration)
    .AddDPD(builder.Configuration)
    .AddDHL(builder.Configuration)
    .AddGLS(builder.Configuration)
    .AddUPS(builder.Configuration)
    .AddFedEx(builder.Configuration);
```

Szczegółową dokumentację dla każdego kuriera znajdziesz w folderze `docs/TBJ.Integrations.Shipping`.
