using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace RentACar.Models
{
    /// <summary>
    /// Application user entity that extends <see cref="IdentityUser"/>.
    /// Stores profile data (first/last name), a national identifier (EGN),
    /// a soft-delete flag and navigation to the user's reservations.
    /// </summary>
    public class User : IdentityUser
    {
        /// <summary>
        /// User first name. Required. Maximum length 50 characters.
        /// </summary>
        [Required]
        [StringLength(50)]
        public required string FirstName { get; set; }

        /// <summary>
        /// User last name. Required. Maximum length 50 characters.
        /// </summary>
        [Required]
        [StringLength(50)]
        public required string LastName { get; set; }

        /// <summary>
        /// ЕГН — Bulgarian national identification number.
        /// Must be exactly 10 digits (validated with a regular expression).
        /// </summary>
        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "EGN must be exactly 10 digits.")]
        public required string EGN { get; set; }

        /// <summary>
        /// Soft-delete flag. When true the user is considered deleted/hidden.
        /// </summary>
        [Required]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Navigation property for reservations created by the user.
        /// Initialized to an empty collection to avoid null checks.
        /// </summary>
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
