using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMvcPostgresApp.Data;
using System.Security.Claims;

namespace MyMvcPostgresApp.Controllers
{
    [Authorize]
    public class MyProjectsController : Controller
    {
        private readonly AppDbContext _context;

        public MyProjectsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: MyProjects
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var myProjects = await _context.ProjectMembers
                .Include(pm => pm.Project)
                .Where(pm => pm.UserId == userId)
                .OrderByDescending(pm => pm.AssignedAt)
                .ToListAsync();

            return View(myProjects);
        }

        // GET: MyProjects/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Sprawdź czy użytkownik ma dostęp do projektu
            var member = await _context.ProjectMembers
                .Include(pm => pm.Project)
                .FirstOrDefaultAsync(pm => pm.ProjectId == id && pm.UserId == userId);

            if (member == null)
            {
                TempData["Error"] = "Nie masz dostępu do tego projektu";
                return RedirectToAction(nameof(Index));
            }

            // Pobierz wszystkich członków projektu
            var allMembers = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == id)
                .OrderBy(pm => pm.User.Login)
                .ToListAsync();

            ViewBag.AllMembers = allMembers;
            ViewBag.MyRole = member.Role;

            return View(member.Project);
        }
    }
}