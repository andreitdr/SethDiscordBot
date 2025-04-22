using System.ComponentModel.DataAnnotations;

namespace WebUI_OLD.Models;

public class SettingsViewModel
{
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; }

    [Required(ErrorMessage = "Prefix is required.")]
    public string Prefix { get; set; }

    [Required(ErrorMessage = "Server IDs are required.")]
    public List<ulong> ServerIds { get; set; } = new List<ulong>();
}