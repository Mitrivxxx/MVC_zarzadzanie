using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMvcPostgresApp.Data;
using MyMvcPostgresApp.Models;
using MyMvcPostgresApp.ViewModels;

namespace MyMvcPostgresApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProjectsController : Controller
    {
        private readonly AppDbContext _context;

        public ProjectsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Projects
        public async Task<IActionResult> Index(string? name, string? status, int page = 1)
        {
            const int pageSize = 10;

            var projectsQuery = _context.Projects.AsQueryable();

            // Filtrowanie po nazwie
            if (!string.IsNullOrWhiteSpace(name))
            {
                projectsQuery = projectsQuery.Where(p => p.Name.Contains(name));
                ViewData["NameFilter"] = name;
            }

            // Filtrowanie po statusie
            if (!string.IsNullOrWhiteSpace(status))
            {
                projectsQuery = projectsQuery.Where(p => p.Status == status);
                ViewData["StatusFilter"] = status;
            }

            // Oblicz całkowitą liczbę projektów
            var totalProjects = await projectsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalProjects / (double)pageSize);

            // Pobierz projekty dla aktualnej strony
            var projects = await projectsQuery
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Pobierz wszystkie dostępne statusy
            var statuses = await _context.Projects
                .Select(p => p.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            ViewBag.Statuses = statuses;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalProjects = totalProjects;

            return View(projects);
        }

        // GET: Projects/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProjectViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Walidacja dat
            if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Data zakończenia nie może być wcześniejsza niż data rozpoczęcia");
                return View(model);
            }

            var project = new Project
            {
                Name = model.Name,
                Description = model.Description,
                // Konwersja dat do UTC jeśli istnieją
                StartDate = model.StartDate.HasValue
                    ? DateTime.SpecifyKind(model.StartDate.Value, DateTimeKind.Utc)
                    : null,
                EndDate = model.EndDate.HasValue
                    ? DateTime.SpecifyKind(model.EndDate.Value, DateTimeKind.Utc)
                    : null,
                Status = model.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Projekt '{project.Name}' został pomyślnie utworzony";
            return RedirectToAction(nameof(Index));
        }

        // GET: Projects/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                TempData["Error"] = "Nie znaleziono projektu";
                return RedirectToAction(nameof(Index));
            }

            var model = new EditProjectViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status
            };

            return View(model);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProjectViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var project = await _context.Projects.FindAsync(model.Id);
            if (project == null)
            {
                TempData["Error"] = "Nie znaleziono projektu";
                return RedirectToAction(nameof(Index));
            }

            // Walidacja dat
            if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Data zakończenia nie może być wcześniejsza niż data rozpoczęcia");
                return View(model);
            }

            project.Name = model.Name;
            project.Description = model.Description;
            // Konwersja dat do UTC jeśli istnieją
            project.StartDate = model.StartDate.HasValue
                ? DateTime.SpecifyKind(model.StartDate.Value, DateTimeKind.Utc)
                : null;
            project.EndDate = model.EndDate.HasValue
                ? DateTime.SpecifyKind(model.EndDate.Value, DateTimeKind.Utc)
                : null;
            project.Status = model.Status;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Dane projektu zostały zaktualizowane";
            return RedirectToAction(nameof(Index));
        }

        // POST: Projects/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                TempData["Error"] = "Nie znaleziono projektu";
                return RedirectToAction(nameof(Index));
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Projekt '{project.Name}' został usunięty";
            return RedirectToAction(nameof(Index));
        }

        // GET: Projects/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                TempData["Error"] = "Nie znaleziono projektu";
                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }
    }
}