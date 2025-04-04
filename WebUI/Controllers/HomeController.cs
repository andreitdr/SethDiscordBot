using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using ILogger = DiscordBotCore.Logging.ILogger;

namespace WebUI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger _logger;
    public HomeController(ILogger logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        _logger.Log("Index page loaded", this);
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
}