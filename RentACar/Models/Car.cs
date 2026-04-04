using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentACar.Models
{
    /// <summary>
    /// Represents a car that can be rented.
    /// Contains identification, technical/spec information, pricing and soft-delete flag.
    /// Also holds navigation to related <see cref="Reservation"/> entries.
    /// </summary>
    public class Car
    {
        /// <summary>
        /// Primary key for persistence. Generated as a GUID string by default.
        /// </summary>
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Manufacturer / brand of the car (e.g. "Toyota").
        /// Required. Maximum length 50 characters.
        /// </summary>
        [Required]
        [StringLength(50)]
        public required string Brand { get; set; }

        /// <summary>
        /// Model name of the car (e.g. "Corolla").
        /// Required. Maximum length 50 characters.
        /// </summary>
        [Required]
        [StringLength(50)]
        public required string Model { get; set; }

        /// <summary>
        /// Production year of the car.
        /// Required. Validated to a reasonable range.
        /// </summary>
        [Required]
        [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100.")]
        public int Year { get; set; }

        /// <summary>
        /// Number of passenger seats.
        /// Required. Validated to a realistic range.
        /// </summary>
        [Required]
        [Range(1, 20, ErrorMessage = "Seating capacity must be between 1 and 20.")]
        public int SeatingCapacity { get; set; }

        /// <summary>
        /// Optional short description or notes about the car.
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Rental price per day.
        /// Required. Stored as decimal(18,2) in the database.
        /// </summary>
        [Required]
        [Range(0.01, 10000, ErrorMessage = "Daily price must be positive.")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal DailyPrice { get; set; } = default(decimal);

        /// <summary>
        /// Soft-delete flag. When true the car is considered deleted/hidden.
        /// </summary>
        [Required]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Navigation collection of reservations that reference this car.
        /// Initialized to an empty list to avoid null checks.
        /// </summary>
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}