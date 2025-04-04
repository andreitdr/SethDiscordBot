using DiscordBotCore.PluginManagement;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using ILogger = DiscordBotCore.Logging.ILogger;

namespace WebUI.Controllers;

public class PluginsController : Controller
{
    private readonly ILogger _logger;
    private readonly IPluginManager _pluginManager;
    
    public PluginsController(ILogger logger, IPluginManager pluginManager)
    {
        _logger = logger;
        _pluginManager = pluginManager;
    }
    
    [HttpGet]
    public async Task<IActionResult> OnlinePlugins()
    {
        _logger.Log("Getting plugins page", this);
        var plugins = await _pluginManager.GetPluginsList();
        
        _logger.Log($"{plugins.Count} Plugins loaded", this);
        List<OnlinePluginViewModel> pluginViewModels = new List<OnlinePluginViewModel>();
        foreach (var plugin in plugins)
        {
            OnlinePluginViewModel pluginViewModel = new OnlinePluginViewModel();
            pluginViewModel.Name = plugin.PluginName;
            pluginViewModel.Description = plugin.PluginDescription;
            pluginViewModel.Author = plugin.PluginAuthor;
            pluginViewModel.Version = plugin.LatestVersion;
            pluginViewModel.DownloadUrl = plugin.PluginLink;
        }
        return View(pluginViewModels);
    }
}