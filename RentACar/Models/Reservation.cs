namespace RentACar.Models;
using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a reservation of a car by a user.
/// Contains start/end dates, references to the user and car, and flags for soft-delete and active state.
/// </summary>
public class Reservation
{
    /// <summary>
    /// Primary key for the reservation. Generated as a GUID string.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Foreign key to the <see cref="User"/> who created the reservation.
    /// Required.
    /// </summary>
    [Required]
    public string UserId { get; set; }

    /// <summary>
    /// Navigation property to the <see cref="User"/> who created the reservation.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Foreign key to the reserved <see cref="Car"/>.
    /// Required.
    /// </summary>
    [Required]
    public string CarId { get; set; }

    /// <summary>
    /// Navigation property to the reserved <see cref="Car"/>.
    /// </summary>
    public Car Car { get; set; } = null!;

    /// <summary>
    /// Reservation start date (inclusive).
    /// Required.
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Reservation end date (exclusive).
    /// Required.
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Soft-delete flag. When true the reservation is considered deleted/hidden.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Indicates whether the reservation is currently active (reserved) or cancelled/completed.
    /// Required.
    /// </summary>
    [Required]
    public bool IsReserved { get; set; }
}
