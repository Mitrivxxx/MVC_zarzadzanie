using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMvcPostgresApp.Data;
using MyMvcPostgresApp.Models;
using MyMvcPostgresApp.ViewModels;
using System.Security.Cryptography;
using System.Text;

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

        // GET: Admin/Users - z paginacją i filtrowaniem
        public async Task<IActionResult> Users(string? login, string? role, int page = 1)
        {
            const int pageSize = 10;

            // Pobierz wszystkich użytkowników
            var usersQuery = _context.Users.AsQueryable();

            // Filtrowanie po loginie
            if (!string.IsNullOrWhiteSpace(login))
            {
                usersQuery = usersQuery.Where(u => u.Login.Contains(login));
                ViewData["LoginFilter"] = login;
            }

            // Filtrowanie po roli
            if (!string.IsNullOrWhiteSpace(role))
            {
                usersQuery = usersQuery.Where(u => u.Role == role);
                ViewData["RoleFilter"] = role;
            }

            // Oblicz całkowitą liczbę użytkowników
            var totalUsers = await usersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            // Pobierz użytkowników dla aktualnej strony
            var users = await usersQuery
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Pobierz wszystkie dostępne role
            var roles = await _context.Users
                .Select(u => u.Role)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            ViewBag.Roles = roles;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalUsers = totalUsers;

            return View("UsersList", users);
        }

        // GET: Admin/CreateUser
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View("CreateUser");
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("CreateUser", model);
            }

            // Sprawdź czy użytkownik już istnieje
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == model.Login);
            if (existingUser != null)
            {
                ModelState.AddModelError("Login", "Użytkownik o takim loginie już istnieje");
                return View("CreateUser", model);
            }

            // Utwórz nowego użytkownika
            var user = new User
            {
                Login = model.Login,
                Password = HashPassword(model.Password),
                Role = model.Role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Użytkownik {user.Login} został pomyślnie utworzony";
            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Nie znaleziono użytkownika";
                return RedirectToAction(nameof(Users));
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Login = user.Login,
                Role = user.Role
            };

            return View(model);
        }

        // POST: Admin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FindAsync(model.Id);
            if (user == null)
            {
                TempData["Error"] = "Nie znaleziono użytkownika";
                return RedirectToAction(nameof(Users));
            }

            // Sprawdź czy nowy login nie jest już zajęty przez innego użytkownika
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == model.Login && u.Id != model.Id);

            if (existingUser != null)
            {
                ModelState.AddModelError("Login", "Użytkownik o takim loginie już istnieje");
                return View(model);
            }

            user.Login = model.Login;
            user.Role = model.Role;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Dane użytkownika zostały zaktualizowane";
            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Nie znaleziono użytkownika";
                return RedirectToAction(nameof(Users));
            }

            // Nie pozwól usunąć samego siebie
            var currentUserLogin = User.Identity?.Name;
            if (user.Login == currentUserLogin)
            {
                TempData["Error"] = "Nie możesz usunąć swojego własnego konta";
                return RedirectToAction(nameof(Users));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Użytkownik {user.Login} został usunięty";
            return RedirectToAction(nameof(Users));
        }

        // Metoda hashowania hasła - taka sama jak w AccountController
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}