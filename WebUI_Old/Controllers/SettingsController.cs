using Microsoft.AspNetCore.Mvc;
using WebUI_OLD.Models;
using IConfiguration = DiscordBotCore.Configuration.IConfiguration;
using ILogger = DiscordBotCore.Logging.ILogger;

namespace WebUI_OLD.Controllers;

public class SettingsController : Controller
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    public SettingsController(ILogger logger, IConfiguration configuration)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        _logger.Log("Getting settings page", this);
        SettingsViewModel model = new SettingsViewModel
        {
            Token = _configuration.Get<string>("token", string.Empty),
            Prefix = _configuration.Get<string>("prefix", string.Empty),
            ServerIds = _configuration.Get<List<ulong>>("ServerIds", new List<ulong>()),
        };
        
        return View(model);
    }
    
    [HttpPost]
    public async Task<IActionResult> SaveSettings([FromForm] SettingsViewModel model)
    {
        _logger.Log("Saving settings", this);
        
        if (string.IsNullOrEmpty(model.Token))
        {
            ModelState.AddModelError("Token", "Token cannot be empty");
        }
        
        if (string.IsNullOrEmpty(model.Prefix))
        {
            ModelState.AddModelError("Prefix", "Prefix cannot be empty");
        }
        
        if (model.ServerIds == null || model.ServerIds.Count == 0)
        {
            ModelState.AddModelError("ServerIds", "At least one Server ID must be provided");
        }
        
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }
        
        _configuration.Set("token", model.Token);
        _configuration.Set("prefix", model.Prefix);
        _configuration.Set("serverIds", model.ServerIds);
        
        await _configuration.SaveToFile();
        _logger.Log("Settings saved successfully", this);
        
        TempData["Notification"] = "Settings saved successfully!";
        
        return RedirectToAction("Index", "Home");
    }

    
}