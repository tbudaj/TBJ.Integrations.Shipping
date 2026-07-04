using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Abstractions.Models;

namespace TBJ.Integrations.Shipping.Abstractions.Interfaces;

/// <summary>
/// Główny punkt wejścia do integracji z firmami kurierskimi.
/// <para>
/// Umożliwia wykonanie wszystkich operacji wysyłkowych (rejestracja, odbiór, śledzenie, POD)
/// dla dowolnego wspieranego kuriera — wystarczy podać <see cref="CarrierType"/> jako parametr.
/// </para>
/// <para>
/// Obsługuje dwa scenariusze uwierzytelniania:
/// <list type="bullet">
/// <item><description>
/// <b>Scenariusz A (konto tenanta):</b> <see cref="CarrierAuthInfo"/> przekazywany per-żądanie
/// z bazy danych aplikacji nadrzędnej — każdy tenant ma własne dane dostępowe.
/// </description></item>
/// <item><description>
/// <b>Scenariusz B (nasze konto):</b> gdy <see cref="CarrierAuthInfo"/> nie jest przekazany (<c>null</c>),
/// adapter używa domyślnych credentials z <c>appsettings.json</c> (pola <c>Default*</c> w <c>*Options</c>).
/// </description></item>
/// </list>
/// </para>
/// </summary>
/// <example>
/// <code>
/// // Scenariusz A: rejestracja przesyłki InPost Paczkomat z kontem tenanta
/// var result = await gateway.CreateShipmentAsync(CarrierType.InPostLocker, new CreateShipmentRequest
/// {
///     AuthInfo = new InPostAuthInfo { AccessToken = tenant.InPostToken, OrganizationId = tenant.InPostOrgId },
///     LockerTargetMachineId = "WAW123M",  // wymagane dla InPostLocker
///     SenderAddress = ...,
/// });
///
/// // Scenariusz A: rejestracja przesyłki InPost Kurier z kontem tenanta
/// var result = await gateway.CreateShipmentAsync(CarrierType.InPostCourier, new CreateShipmentRequest
/// {
///     AuthInfo = new InPostAuthInfo { AccessToken = tenant.InPostToken, OrganizationId = tenant.InPostOrgId },
///     SenderAddress = ...,
/// });
///
/// // Scenariusz B: rejestracja z naszego konta (credentials z appsettings)
/// var result = await gateway.CreateShipmentAsync(CarrierType.InPostLocker, new CreateShipmentRequest
/// {
///     // AuthInfo = null (pominięte) — użyje DefaultAccessToken i DefaultOrganizationId z InPostOptions
///     LockerTargetMachineId = "WAW123M",
///     SenderAddress = ...,
/// });
///
/// // Scenariusz A: śledzenie z kontem tenanta
/// var tracking = await gateway.TrackShipmentAsync(
///     CarrierType.DPD,
///     new DpdAuthInfo { Username = tenant.DpdLogin, Password = tenant.DpdPassword, Fid = tenant.DpdFid },
///     "12345678901234");
///
/// // Scenariusz B: śledzenie z naszego konta (credentials z appsettings)
/// var tracking = await gateway.TrackShipmentAsync(CarrierType.InPostLocker, null, "12345678901234");
/// </code>
/// </example>
public interface IShippingGateway
{
    /// <summary>
    /// Rejestruje nową przesyłkę u wybranego kuriera.
    /// </summary>
    /// <param name="carrier">Firma kurierska.</param>
    /// <param name="request">
    /// Dane przesyłki. <see cref="CreateShipmentRequest.AuthInfo"/> może być <c>null</c>
    /// — wtedy adapter użyje domyślnych credentials z opcji (<c>Default*</c> w <c>*Options</c>).
    /// </param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Wynik rejestracji z numerem śledzenia i etykietą.</returns>
    /// <exception cref="ShippingException">Gdy API kuriera zwróci błąd.</exception>
    /// <exception cref="InvalidOperationException">Gdy adapter dla podanego kuriera nie jest zarejestrowany lub brak credentials.</exception>
    Task<ShipmentResult> CreateShipmentAsync(
        CarrierType carrier,
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Zamawia przyjazd kuriera po odbiór przesyłek.
    /// </summary>
    /// <param name="carrier">Firma kurierska.</param>
    /// <param name="request">
    /// Dane dyspozycji odbioru. <see cref="OrderPickupRequest.AuthInfo"/> może być <c>null</c>
    /// — wtedy adapter użyje domyślnych credentials z opcji (<c>Default*</c> w <c>*Options</c>).
    /// </param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Wynik z identyfikatorem dyspozycji odbioru.</returns>
    /// <exception cref="ShippingException">Gdy API kuriera zwróci błąd.</exception>
    /// <exception cref="InvalidOperationException">Gdy adapter dla podanego kuriera nie jest zarejestrowany lub brak credentials.</exception>
    Task<PickupResult> OrderPickupAsync(
        CarrierType carrier,
        OrderPickupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera aktualny status i historię zdarzeń przesyłki.
    /// </summary>
    /// <param name="carrier">Firma kurierska.</param>
    /// <param name="authInfo">
    /// Dane uwierzytelniające do API kuriera (opcjonalne).
    /// Gdy <c>null</c> — adapter użyje domyślnych credentials z opcji (<c>Default*</c> w <c>*Options</c>).
    /// </param>
    /// <param name="trackingNumber">Numer śledzenia przesyłki.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Wynik z aktualnym statusem i listą zdarzeń.</returns>
    /// <exception cref="ShippingException">Gdy API kuriera zwróci błąd.</exception>
    /// <exception cref="InvalidOperationException">Gdy adapter dla podanego kuriera nie jest zarejestrowany lub brak credentials.</exception>
    Task<TrackingResult> TrackShipmentAsync(
        CarrierType carrier,
        CarrierAuthInfo? authInfo,
        string trackingNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera potwierdzenie doręczenia (Proof of Delivery) dla przesyłki.
    /// </summary>
    /// <param name="carrier">Firma kurierska.</param>
    /// <param name="authInfo">
    /// Dane uwierzytelniające do API kuriera (opcjonalne).
    /// Gdy <c>null</c> — adapter użyje domyślnych credentials z opcji (<c>Default*</c> w <c>*Options</c>).
    /// </param>
    /// <param name="trackingNumber">Numer śledzenia przesyłki.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Wynik z danymi doręczenia i opcjonalnym dokumentem POD.</returns>
    /// <exception cref="ShippingException">Gdy API kuriera zwróci błąd.</exception>
    /// <exception cref="InvalidOperationException">Gdy adapter dla podanego kuriera nie jest zarejestrowany lub brak credentials.</exception>
    Task<DeliveryConfirmationResult> GetDeliveryConfirmationAsync(
        CarrierType carrier,
        CarrierAuthInfo? authInfo,
        string trackingNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca adapter (klienta) dla konkretnego kuriera.
    /// </summary>
    /// <param name="carrier">Firma kurierska.</param>
    /// <returns>Implementacja <see cref="IShippingClient"/> dla danego kuriera.</returns>
    /// <exception cref="InvalidOperationException">Gdy adapter dla podanego kuriera nie jest zarejestrowany.</exception>
    IShippingClient GetClient(CarrierType carrier);
}
