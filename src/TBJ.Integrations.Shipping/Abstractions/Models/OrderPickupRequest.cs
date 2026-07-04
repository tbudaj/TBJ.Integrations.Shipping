using TBJ.Integrations.Shipping.Abstractions.Auth;

namespace TBJ.Integrations.Shipping.Abstractions.Models;

/// <summary>
/// Żądanie zamówienia kuriera po odbiór przesyłek.
/// Zawiera dane dyspozycji odbioru.
/// </summary>
public sealed class OrderPickupRequest
{
    /// <summary>
    /// Dane uwierzytelniające do API kuriera (opcjonalne).
    /// <para>
    /// Gdy podane — używane są credentials tenanta (Scenariusz A: konto tenanta).
    /// Gdy <c>null</c> — adapter używa domyślnych credentials z <c>appsettings.json</c>
    /// (Scenariusz B: nasze konto aplikacji, konfigurowane przez <c>Default*</c> w <c>*Options</c>).
    /// </para>
    /// </summary>
    public CarrierAuthInfo? AuthInfo { get; init; }

    /// <summary>
    /// Identyfikatory przesyłek (wartości z <see cref="ShipmentResult.CarrierShipmentId"/>)
    /// do odebrania w jednej dyspozycji.
    /// </summary>
    public required IReadOnlyList<string> ShipmentIds { get; init; }

    /// <summary>Adres, pod który ma przyjechać kurier.</summary>
    public required Address PickupAddress { get; init; }

    /// <summary>Dane kontaktowe osoby dostępnej w miejscu odbioru.</summary>
    public required ContactInfo PickupContact { get; init; }

    /// <summary>Data odbioru (data — bez godziny).</summary>
    public required DateOnly PickupDate { get; init; }

    /// <summary>
    /// Godzina, od której kurier może odebrać przesyłki (format HH:mm).
    /// Domyślnie: 08:00.
    /// </summary>
    public TimeOnly PickupTimeFrom { get; init; } = new TimeOnly(8, 0);

    /// <summary>
    /// Godzina, do której kurier musi odebrać przesyłki (format HH:mm).
    /// Domyślnie: 16:00.
    /// </summary>
    public TimeOnly PickupTimeTo { get; init; } = new TimeOnly(16, 0);
}
