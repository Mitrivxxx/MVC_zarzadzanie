using System.ComponentModel.DataAnnotations;

namespace MyMvcPostgresApp.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Podaj aktualne hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Aktualne hasło")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj nowe hasło")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków")]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Potwierdź nowe hasło")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Hasła nie są zgodne")]
        [Display(Name = "Potwierdź nowe hasło")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
