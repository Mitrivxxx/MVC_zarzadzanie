using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMvcPostgresApp.Data;
using MyMvcPostgresApp.Models;
using MyMvcPostgresApp.ViewModels;

namespace MyMvcPostgresApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Users?login=...&role=...
        public async Task<IActionResult> Users(string? login, string? role)
        {
            // Lista dostępnych ról do selecta
            var roles = await _context.Users
                .Select(u => u.Role)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            ViewBag.Roles = roles;
            ViewData["LoginFilter"] = login;
            ViewData["RoleFilter"] = role;

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(login))
            {
                // Postgres ILIKE dla case-insensitive contains
                query = query.Where(u => EF.Functions.ILike(u.Login, $"%{login}%"));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            var users = await query
                .OrderBy(u => u.Login)
                .ToListAsync();

            return View("UsersList", users);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                Login = user.Login,
                Role = user.Role
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FindAsync(model.Id);
            if (user == null) return NotFound();

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == model.Login && u.Id != model.Id);

            if (existingUser != null)
            {
                ModelState.AddModelError("Login", "Ten login jest już zajęty");
                return View(model);
            }

            user.Login = model.Login;
            user.Role = model.Role;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Dane użytkownika zostały zaktualizowane.";
            return RedirectToAction("Users");
        }
    }
}