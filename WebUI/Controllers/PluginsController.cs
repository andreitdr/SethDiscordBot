using DiscordBotCore.PluginManagement;
using DiscordBotCore.PluginManagement.Models;
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
            pluginViewModel.Name = plugin.Name;
            pluginViewModel.Description = plugin.Description;
            pluginViewModel.Author = plugin.Author;
            pluginViewModel.Version = plugin.Version;
            pluginViewModel.DownloadUrl = plugin.DownloadLink;
            pluginViewModel.Id = plugin.Id;
            
            pluginViewModels.Add(pluginViewModel);
        }
        
        return View(pluginViewModels);
    }

    [HttpGet]
    public async Task<IActionResult> InstalledPlugins()
    {
        _logger.Log("Getting plugins page", this);
        var plugins = await _pluginManager.GetInstalledPlugins();
        _logger.Log($"{plugins.Count} Plugins loaded", this);
        List<InstalledPluginViewModel> pluginViewModels = new List<InstalledPluginViewModel>();
        foreach (var plugin in plugins)
        {
            InstalledPluginViewModel pluginViewModel = new InstalledPluginViewModel();
            pluginViewModel.Name = plugin.PluginName;
            pluginViewModel.Version = plugin.PluginVersion;
            pluginViewModel.IsOfflineAdded = plugin.IsOfflineAdded;
            
            pluginViewModels.Add(pluginViewModel);
        }
        
        return View(pluginViewModels);
    }

    [HttpPost]
    public async Task<IActionResult> DeletePlugin(string pluginName)
    {
        _logger.Log($"Deleting plugin {pluginName}", this);
        //TODO: Implement delete plugin
        return RedirectToAction("InstalledPlugins");
    }

    [HttpPost]
    public async Task<IActionResult> InstallPlugin(int pluginId)
    {
        var pluginData = await _pluginManager.GetPluginDataById(pluginId);
        if (pluginData is null)
        {
            _logger.Log($"Plugin with ID {pluginId} not found", this);
            return RedirectToAction("OnlinePlugins");
        }

        IProgress<float> progress = new Progress<float>(f => _logger.Log($"Installing: {f}"));
        
        await _pluginManager.InstallPlugin(pluginData, progress);
        
        _logger.Log($"Plugin {pluginData.Name} installed", this);
        return RedirectToAction("OnlinePlugins");
    }
}