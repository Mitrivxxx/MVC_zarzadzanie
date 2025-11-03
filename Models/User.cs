using System.ComponentModel.DataAnnotations;

namespace MyMvcPostgresApp.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Login { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "User"; // Admin, User, itp.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
