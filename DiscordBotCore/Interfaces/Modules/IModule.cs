using System.Threading.Tasks;

namespace DiscordBotCore.Interfaces.Modules
{
    
    public enum ModuleType
    {
        Logger,
        Other
    }
    
    /// <summary>
    /// Define a module.
    /// </summary>
    public interface IModule
    {
        public ModuleType ModuleType { get; }
        public string Name { get; }
        public Task Initialize();
    }
}
