using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Abstractions.Models;

namespace TBJ.Integrations.Shipping.Abstractions.Interfaces;

/// <summary>
/// Adapter konkretnego kuriera. Implementowany przez każdy projekt integracji
/// (InPost, DPD, DHL, GLS, UPS, FedEx).
/// <para>
/// Dostęp z poziomu aplikacji odbywa się przez <see cref="IShippingGateway"/>,
/// który dobiera właściwą implementację na podstawie parametru <see cref="CarrierType"/>.
/// </para>
/// <para>
/// Obsługuje dwa scenariusze uwierzytelniania:
/// <list type="bullet">
/// <item><description>
/// <b>Scenariusz A (konto tenanta):</b> <see cref="CarrierAuthInfo"/> przekazywany per-żądanie z bazy danych.
/// </description></item>
/// <item><description>
/// <b>Scenariusz B (nasze konto):</b> gdy <see cref="CarrierAuthInfo"/> jest <c>null</c>,
/// adapter używa domyślnych credentials z <c>appsettings.json</c> (pola <c>Default*</c> w <c>*Options</c>).
/// </description></item>
/// </list>
/// </para>
/// </summary>
public interface IShippingClient
{
    /// <summary>Identyfikator kuriera obsługiwanego przez ten adapter.</summary>
    CarrierType Carrier { get; }

    /// <summary>
    /// Rejestruje nową przesyłkę w systemie kuriera i opcjonalnie pobiera etykietę.
    /// </summary>
    /// <param name="request">
    /// Dane przesyłki: nadawca, odbiorca, paczki, usługi dodatkowe.
    /// Zawiera <see cref="CarrierAuthInfo"/> specyficzne dla tenanta.
    /// </param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Wynik rejestracji z numerem śledzenia i etykietą.</returns>
    /// <exception cref="ShippingException">Gdy API kuriera zwróci błąd lub komunikacja się nie powiedzie.</exception>
    Task<ShipmentResult> CreateShipmentAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Zamawia przyjazd kuriera po odbiór zarejestrowanych przesyłek.
    /// </summary>
    /// <param name="request">
    /// Dane dyspozycji: lista przesyłek, adres odbioru, okno czasowe.
    /// Zawiera <see cref="CarrierAuthInfo"/> specyficzne dla tenanta.
    /// </param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Wynik z identyfikatorem dyspozycji odbioru.</returns>
    /// <exception cref="ShippingException">Gdy API kuriera zwróci błąd lub komunikacja się nie powiedzie.</exception>
    Task<PickupResult> OrderPickupAsync(
        OrderPickupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera aktualny status i historię zdarzeń dla podanego numeru śledzenia.
    /// </summary>
    /// <param name="authInfo">
    /// Dane uwierzytelniające do API kuriera (opcjonalne).
    /// Gdy <c>null</c> — adapter użyje domyślnych credentials z <c>appsettings.json</c>.
    /// </param>
    /// <param name="trackingNumber">Numer śledzenia przesyłki.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Wynik z aktualnym statusem i listą zdarzeń.</returns>
    /// <exception cref="ShippingException">Gdy API kuriera zwróci błąd lub komunikacja się nie powiedzie.</exception>
    /// <exception cref="InvalidOperationException">Gdy brak credentials (nie przekazano i brak Default* w opcjach).</exception>
    Task<TrackingResult> TrackShipmentAsync(
        CarrierAuthInfo? authInfo,
        string trackingNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera potwierdzenie doręczenia (Proof of Delivery — POD) dla przesyłki.
    /// </summary>
    /// <param name="authInfo">
    /// Dane uwierzytelniające do API kuriera (opcjonalne).
    /// Gdy <c>null</c> — adapter użyje domyślnych credentials z <c>appsettings.json</c>.
    /// </param>
    /// <param name="trackingNumber">Numer śledzenia przesyłki.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Wynik z datą doręczenia, podpisem odbiorcy i opcjonalnym dokumentem POD.</returns>
    /// <exception cref="ShippingException">Gdy API kuriera zwróci błąd, przesyłka nie istnieje lub nie została jeszcze dostarczona.</exception>
    /// <exception cref="InvalidOperationException">Gdy brak credentials (nie przekazano i brak Default* w opcjach).</exception>
    Task<DeliveryConfirmationResult> GetDeliveryConfirmationAsync(
        CarrierAuthInfo? authInfo,
        string trackingNumber,
        CancellationToken cancellationToken = default);
}
