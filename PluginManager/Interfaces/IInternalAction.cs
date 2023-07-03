using System.Threading.Tasks;
using PluginManager.Others;

namespace PluginManager.Interfaces;

public interface ICommandAction
{
    public string ActionName { get; }

    public string? Description { get; }

    public string? Usage { get; }

    public InternalActionRunType RunType { get; }

    public Task Execute(string[]? args);
}
