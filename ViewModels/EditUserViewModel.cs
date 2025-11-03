using System.ComponentModel.DataAnnotations;

namespace MyMvcPostgresApp.ViewModels
{
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Login jest wymagany")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Login musi mieć od 3 do 50 znaków")]
        [Display(Name = "Login")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rola jest wymagana")]
        [Display(Name = "Rola")]
        public string Role { get; set; } = string.Empty;
    }
}