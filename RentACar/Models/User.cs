using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace RentACar.Models
{
    public class User : IdentityUser
    {
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
        [Required]
        public bool IsDeleted { get; set; }
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
