using System.ComponentModel.DataAnnotations;

namespace RentACar.NewFolder
{
    public class LoginViewModel
    {
        // The username of the user. This property is required and is displayed with the name "Username".
        [Required(ErrorMessage = "Потребителското име е задължително")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;
        // The password of the user. This property is required, is of type password, and is displayed with the name "Password".
        [Required(ErrorMessage = "Паролата е задължителна")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
        // A boolean property indicating whether the user wants to be remembered on the device. This property is displayed with the name "Remember me".
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
