using System.ComponentModel.DataAnnotations;
using MyMvcPostgresApp.Models;

namespace MyMvcPostgresApp.ViewModels
{
    public class CreateTaskViewModel
    {
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Tytuł musi mieć od 3 do 200 znaków")]
        [Display(Name = "Tytuł zadania")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Opis może mieć maksymalnie 2000 znaków")]
        [Display(Name = "Opis")]
        public string? Description { get; set; }

        [Display(Name = "Przypisz do")]
        public int? AssignedToUserId { get; set; }

        [Required(ErrorMessage = "Priorytet jest wymagany")]
        [Display(Name = "Priorytet")]
        public string Priority { get; set; } = "Medium";

        [Display(Name = "Termin")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Szacowany czas (godz.)")]
        [Range(0, 1000, ErrorMessage = "Czas musi być między 0 a 1000 godzin")]
        public int EstimatedHours { get; set; } = 0;

        [Display(Name = "Tagi")]
        [StringLength(50)]
        public string? Tags { get; set; }
    }

    public class EditTaskViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany")]
        [StringLength(200, MinimumLength = 3)]
        [Display(Name = "Tytuł zadania")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Opis")]
        public string? Description { get; set; }

        [Display(Name = "Przypisz do")]
        public int? AssignedToUserId { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Priorytet")]
        public string Priority { get; set; } = string.Empty;

        [Display(Name = "Termin")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Postęp (%)")]
        [Range(0, 100)]
        public int ProgressPercentage { get; set; }

        [Display(Name = "Szacowany czas (godz.)")]
        [Range(0, 1000)]
        public int EstimatedHours { get; set; }

        [Display(Name = "Faktyczny czas (godz.)")]
        [Range(0, 1000)]
        public int ActualHours { get; set; }

        [Display(Name = "Tagi")]
        [StringLength(50)]
        public string? Tags { get; set; }
    }

    public class TaskDetailsViewModel
    {
        public ProjectTask Task { get; set; } = null!;
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanComment { get; set; }
        public List<User> AvailableAssignees { get; set; } = new();
        public List<TaskComment> Comments { get; set; } = new();
        public List<TaskAttachment> Attachments { get; set; } = new();
        public List<TaskHistory> History { get; set; } = new();
    }

    public class AddTaskCommentViewModel
    {
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Treść komentarza jest wymagana")]
        [StringLength(1000, MinimumLength = 1)]
        [Display(Name = "Komentarz")]
        public string Content { get; set; } = string.Empty;
    }

    public class TaskBoardViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public bool IsLead { get; set; }
        public List<ProjectTask> ToDoTasks { get; set; } = new();
        public List<ProjectTask> InProgressTasks { get; set; } = new();
        public List<ProjectTask> InReviewTasks { get; set; } = new();
        public List<ProjectTask> DoneTasks { get; set; } = new();
        public List<ProjectTask> BlockedTasks { get; set; } = new();
        public Dictionary<string, int> StatusCounts { get; set; } = new();
        public List<User> TeamMembers { get; set; } = new();
    }
}