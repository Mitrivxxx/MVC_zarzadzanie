using System.ComponentModel.DataAnnotations;
using MyMvcPostgresApp.Models;

namespace MyMvcPostgresApp.ViewModels
{
    public class ManageProjectMembersViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public List<ProjectMember> Members { get; set; } = new();
        public List<User> AvailableUsers { get; set; } = new();
    }

    public class AddProjectMemberViewModel
    {
        [Required]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Wybierz użytkownika")]
        [Display(Name = "Użytkownik")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Wybierz rolę w projekcie")]
        [Display(Name = "Rola w projekcie")]
        public string MemberRole { get; set; } = "Member";
    }
}