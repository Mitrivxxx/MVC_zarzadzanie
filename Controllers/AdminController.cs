using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMvcPostgresApp.Data;

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

        [HttpGet]
        public async Task<IActionResult> UsersList()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Login,
                    u.Role,
                    CreatedAt = u.CreatedAt.ToString("dd.MM.yyyy")
                })
                .ToListAsync();

            ViewBag.Users = users;
            return View();
        }
    }
}
