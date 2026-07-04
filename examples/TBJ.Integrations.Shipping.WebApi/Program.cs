using TBJ.Integrations.Shipping;
using TBJ.Integrations.Shipping.Gateway;
using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Abstractions.Models;

var builder = WebApplication.CreateBuilder(args);

// Rejestracja bramki kurierskiej i wybranych adapterów.
builder.Services
    .AddShippingGateway()
    .AddInPostLocker(builder.Configuration)
    .AddInPostCourier(builder.Configuration)
    .AddDPD(builder.Configuration)
    .AddDHL(builder.Configuration)
    .AddGLS(builder.Configuration)
    .AddUPS(builder.Configuration)
    .AddFedEx(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TBJ.Integrations.Shipping WebApi",
        Version = "v1",
        Description = "Przykładowe API demonstrujące użycie pakietu TBJ.Integrations.Shipping."
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

/// <summary>Tworzy przesyłkę u wybranego kuriera.</summary>
app.MapPost("/api/shipments", async (IShippingGateway gateway, CreateShipmentRequest request, CarrierType carrier) =>
{
    var result = await gateway.CreateShipmentAsync(carrier, request);
    return Results.Ok(result);
})
.WithName("CreateShipment")
.WithOpenApi();

/// <summary>Śledzi przesyłkę u wybranego kuriera.</summary>
app.MapGet("/api/shipments/{trackingNumber}/track", async (IShippingGateway gateway, CarrierType carrier, string trackingNumber, CarrierAuthInfo authInfo) =>
{
    var result = await gateway.TrackShipmentAsync(carrier, authInfo, trackingNumber);
    return Results.Ok(result);
})
.WithName("TrackShipment")
.WithOpenApi();

/// <summary>Zamawia odbiór przesyłek u wybranego kuriera.</summary>
app.MapPost("/api/pickups", async (IShippingGateway gateway, CarrierType carrier, OrderPickupRequest request) =>
{
    var result = await gateway.OrderPickupAsync(carrier, request);
    return Results.Ok(result);
})
.WithName("OrderPickup")
.WithOpenApi();

/// <summary>Pobiera potwierdzenie doręczenia (POD) u wybranego kuriera.</summary>
app.MapGet("/api/shipments/{trackingNumber}/pod", async (IShippingGateway gateway, CarrierType carrier, string trackingNumber, CarrierAuthInfo authInfo) =>
{
    var result = await gateway.GetDeliveryConfirmationAsync(carrier, authInfo, trackingNumber);
    return Results.Ok(result);
})
.WithName("GetDeliveryConfirmation")
.WithOpenApi();

app.Run();
