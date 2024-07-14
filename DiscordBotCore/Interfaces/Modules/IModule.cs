using System.Threading.Tasks;

namespace DiscordBotCore.Interfaces.Modules
{
    public interface IModule<T> where T : IBaseModule
    {
        public string Name { get; }
        public T Module { get; }
        public Task Initialize();
    }
}
