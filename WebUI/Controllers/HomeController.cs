using DiscordBotCore.Bot;
using DiscordBotCore.PluginManagement.Loading;
using Microsoft.AspNetCore.Mvc;
using ILogger = DiscordBotCore.Logging.ILogger;

namespace WebUI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger _logger;
    private readonly IDiscordBotApplication _discordBotApplication;
    private readonly IPluginLoader _pluginLoader;

    public HomeController(ILogger logger, IDiscordBotApplication discordBotApplication, IPluginLoader pluginLoader)
    {
        _logger = logger;
        _discordBotApplication = discordBotApplication;
        _pluginLoader = pluginLoader;
    }

    [HttpGet]
    public IActionResult Index()
    {
        _logger.Log("Index page loaded", this);
        ViewBag.IsRunning = _discordBotApplication.IsReady;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> StartApplication()
    {
        if (_discordBotApplication.IsReady)
        {
            _logger.Log("Application already started", this);
            return RedirectToAction("Index");
        }

        await _discordBotApplication.StartAsync();
        return RedirectToAction("Index");
    }
    
    [HttpPost]
    public async Task<IActionResult> StopApplication()
    {
        if (!_discordBotApplication.IsReady)
        {
            _logger.Log("Application already stopped", this);
            return RedirectToAction("Index");
        }
        
        await _discordBotApplication.StopAsync();
        return RedirectToAction("Index");
    }
    
    [HttpGet]
    public JsonResult GetLogs()
    {
        var logText = _logger.GetLogsHistory();
        var logs = logText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        return Json(logs);
    }
    
    [HttpPost]
    public async Task<IActionResult> LoadPlugins()
    {
        _logger.Log("Loading plugins", this);
        await _pluginLoader.LoadPlugins();
        //_logger.Log("Plugins loaded", this);
        return RedirectToAction("Index");
    }
}