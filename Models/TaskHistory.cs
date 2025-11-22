using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyMvcPostgresApp.Models
{
    public class TaskHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // Created, Updated, StatusChanged, AssignedTo, CommentAdded, etc.

        [StringLength(500)]
        public string? Details { get; set; }

        [StringLength(100)]
        public string? OldValue { get; set; }

        [StringLength(100)]
        public string? NewValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("TaskId")]
        public ProjectTask Task { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}