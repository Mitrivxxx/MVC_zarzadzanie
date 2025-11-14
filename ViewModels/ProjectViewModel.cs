using System.ComponentModel.DataAnnotations;

namespace MyMvcPostgresApp.ViewModels
{
    public class CreateProjectViewModel
    {
        [Required(ErrorMessage = "Nazwa projektu jest wymagana")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Nazwa musi mieć od 3 do 100 znaków")]
        [Display(Name = "Nazwa projektu")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Opis może mieć maksymalnie 1000 znaków")]
        [Display(Name = "Opis")]
        public string? Description { get; set; }

        [Display(Name = "Data rozpoczęcia")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Data zakończenia")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Status jest wymagany")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";
    }

    public class EditProjectViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa projektu jest wymagana")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Nazwa musi mieć od 3 do 100 znaków")]
        [Display(Name = "Nazwa projektu")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Opis może mieć maksymalnie 1000 znaków")]
        [Display(Name = "Opis")]
        public string? Description { get; set; }

        [Display(Name = "Data rozpoczęcia")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Data zakończenia")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Status jest wymagany")]
        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;
    }
}