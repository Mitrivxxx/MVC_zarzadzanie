using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyMvcPostgresApp.Models;
using Npgsql;

namespace MyMvcPostgresApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _config;

    public HomeController(ILogger<HomeController> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    public IActionResult TestDb()
    {
        string connString = _config.GetConnectionString("DefaultConnection");

        try
        {
            using var conn = new NpgsqlConnection(connString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT NOW();", conn);
            var result = cmd.ExecuteScalar();

            return Content($"Po??czenie OK! Serwer PostgreSQL zwraca: {result}");
        }
        catch (Exception ex)
        {
            return Content($"B??d po??czenia: {ex.Message}");
        }

    }
}
