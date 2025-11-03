using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMvcPostgresApp.Data;
using MyMvcPostgresApp.Models;
using MyMvcPostgresApp.ViewModels;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MyMvcPostgresApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var hashedPassword = HashPassword(model.Password);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == model.Login && u.Password == hashedPassword);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Nieprawidłowy login lub hasło");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == model.Login);

            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Użytkownik o tym loginie już istnieje");
                return View(model);
            }

            var user = new User
            {
                Login = model.Login,
                Password = HashPassword(model.Password),
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rejestracja zakończona pomyślnie. Możesz się teraz zalogować.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        [HttpGet]
        public IActionResult Details()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login");

            return View();
        }
      

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var login = User.Identity!.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);

            if (user == null)
            {
                TempData["Error"] = "Nie znaleziono użytkownika.";
                return View(model);
            }

            var currentHashed = HashPassword(model.CurrentPassword);
            if (user.Password != currentHashed)
            {
                TempData["Error"] = "Aktualne hasło jest nieprawidłowe.";
                return View(model);
            }

            user.Password = HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Hasło zostało zmienione pomyślnie.";
            return RedirectToAction("Details");
        }
    }
}
