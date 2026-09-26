namespace MarketLink.Models;

/// <summary>
/// The lifecycle of a customer pre-order.
/// </summary>
public enum OrderStatus
{
    Placed = 0,
    Accepted = 1,
    ReadyForPickup = 2,
    Completed = 3,
    Cancelled = 4,
    Declined = 5
}
