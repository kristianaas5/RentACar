using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentACar.Models
{
    public class Car
    {
        // Primary key for persistence
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public required string Brand { get; set; }

        [Required]
        [StringLength(50)]
        public required string Model { get; set; }

        // Year of the car (basic range validation)
        [Required]
        [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100.")]
        public int Year { get; set; }

        // Number of passenger seats
        [Required]
        [Range(1, 20, ErrorMessage = "Seating capacity must be between 1 and 20.")]
        public int SeatingCapacity { get; set; }

        // Optional short description (technical info allowed)
        [StringLength(1000)]
        public string? Description { get; set; }

        // Rental price per day
        [Required]
        [Range(0.01, 10000, ErrorMessage = "Daily price must be positive.")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal DailyPrice { get; set; } = default(decimal);
        [Required]
        public bool IsDeleted { get; set; }

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}