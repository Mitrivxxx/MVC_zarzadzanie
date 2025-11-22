using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyMvcPostgresApp.Models
{

    public class ProjectTask
    {

        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "ToDo"; // ToDo, InProgress, InReview, Done, Blocked

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical

        public int? AssignedToUserId { get; set; }

        public int CreatedByUserId { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [Range(0, 100)]
        public int ProgressPercentage { get; set; } = 0;

        [StringLength(50)]
        public string? Tags { get; set; }

        public int EstimatedHours { get; set; } = 0;

        public int ActualHours { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ProjectId")]
        public Project Project { get; set; } = null!;

        [ForeignKey("AssignedToUserId")]
        public User? AssignedToUser { get; set; }

        [ForeignKey("CreatedByUserId")]
        public User CreatedByUser { get; set; } = null!;

        public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
        public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
        public ICollection<TaskHistory> History { get; set; } = new List<TaskHistory>();
    }
}