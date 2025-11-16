using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMvcPostgresApp.Data;
using MyMvcPostgresApp.Models;
using MyMvcPostgresApp.ViewModels;

namespace MyMvcPostgresApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProjectMembersController : Controller
    {
        private readonly AppDbContext _context;

        public ProjectMembersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ProjectMembers/Manage/5
        [HttpGet]
        public async Task<IActionResult> Manage(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                TempData["Error"] = "Nie znaleziono projektu";
                return RedirectToAction("Index", "Projects");
            }

            // Pobierz członków projektu
            var members = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == id)
                .OrderBy(pm => pm.User.Login)
                .ToListAsync();

            // Pobierz użytkowników, którzy NIE są w projekcie
            var existingUserIds = members.Select(m => m.UserId).ToList();
            var availableUsers = await _context.Users
                .Where(u => !existingUserIds.Contains(u.Id))
                .OrderBy(u => u.Login)
                .ToListAsync();

            var viewModel = new ManageProjectMembersViewModel
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                Members = members,
                AvailableUsers = availableUsers
            };

            return View(viewModel);
        }

        // POST: ProjectMembers/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddProjectMemberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Nieprawidłowe dane";
                return RedirectToAction("Manage", new { id = model.ProjectId });
            }

            // Sprawdź czy projekt istnieje
            var project = await _context.Projects.FindAsync(model.ProjectId);
            if (project == null)
            {
                TempData["Error"] = "Nie znaleziono projektu";
                return RedirectToAction("Index", "Projects");
            }

            // Sprawdź czy użytkownik istnieje
            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null)
            {
                TempData["Error"] = "Nie znaleziono użytkownika";
                return RedirectToAction("Manage", new { id = model.ProjectId });
            }

            // Sprawdź czy użytkownik nie jest już przypisany
            var exists = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == model.ProjectId && pm.UserId == model.UserId);

            if (exists)
            {
                TempData["Error"] = "Ten użytkownik jest już przypisany do projektu";
                return RedirectToAction("Manage", new { id = model.ProjectId });
            }

            // Dodaj członka
            var member = new ProjectMember
            {
                ProjectId = model.ProjectId,
                UserId = model.UserId,
                Role = model.MemberRole,
                AssignedAt = DateTime.UtcNow
            };

            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Użytkownik {user.Login} został dodany do projektu";
            return RedirectToAction("Manage", new { id = model.ProjectId });
        }

        // POST: ProjectMembers/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id, int projectId)
        {
            var member = await _context.ProjectMembers
                .Include(pm => pm.User)
                .FirstOrDefaultAsync(pm => pm.Id == id);

            if (member == null)
            {
                TempData["Error"] = "Nie znaleziono przypisania";
                return RedirectToAction("Manage", new { id = projectId });
            }

            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Użytkownik {member.User.Login} został usunięty z projektu";
            return RedirectToAction("Manage", new { id = projectId });
        }

        // POST: ProjectMembers/UpdateRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(int id, int projectId, string role)
        {
            var member = await _context.ProjectMembers.FindAsync(id);
            if (member == null)
            {
                TempData["Error"] = "Nie znaleziono przypisania";
                return RedirectToAction("Manage", new { id = projectId });
            }

            member.Role = role;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rola użytkownika została zaktualizowana";
            return RedirectToAction("Manage", new { id = projectId });
        }
    }
}