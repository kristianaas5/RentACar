using System.ComponentModel.DataAnnotations;

namespace RentACar.Models
{
    public class User
    {
        // Primary key (useful for EF / persistence)
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Username { get; set; }

        // Store a hashed password for security. Do not store plain text in production.
        [Required]
        [DataType(DataType.Password)]
        public required string PasswordHash { get; set; }

        [Required]
        [StringLength(50)]
        public required string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public required string LastName { get; set; }

        // ЕГН — exactly 10 digits
        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "EGN must be exactly 10 digits.")]
        public required string EGN { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }
    }
}
