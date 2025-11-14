using System.ComponentModel.DataAnnotations;

namespace MyMvcPostgresApp.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Login jest wymagany")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Login musi mieć od 3 do 50 znaków")]
        [Display(Name = "Login")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane")]
        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź hasło")]
        [Compare("Password", ErrorMessage = "Hasła nie są zgodne")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rola jest wymagana")]
        [Display(Name = "Rola")]
        public string Role { get; set; } = "Pracownik";
    }
}