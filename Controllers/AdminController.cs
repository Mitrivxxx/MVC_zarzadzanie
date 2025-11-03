using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMvcPostgresApp.Data;
using MyMvcPostgresApp.Models;
using MyMvcPostgresApp.ViewModels; // ← DODAJ TO

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

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .OrderBy(u => u.Login)
                .ToListAsync();

            return View("UsersList", users);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Konwertuj na ViewModel (bez hasła)
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

            // Sprawdź czy login nie jest już zajęty przez innego użytkownika
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == model.Login && u.Id != model.Id);

            if (existingUser != null)
            {
                ModelState.AddModelError("Login", "Ten login jest już zajęty");
                return View(model);
            }

            // Aktualizuj tylko login i rolę (NIE hasło!)
            user.Login = model.Login;
            user.Role = model.Role;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Dane użytkownika zostały zaktualizowane.";
            return RedirectToAction("Users");
        }
    }
}