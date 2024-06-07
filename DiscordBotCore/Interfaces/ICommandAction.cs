using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;

namespace DiscordBotCore.Interfaces;

public interface ICommandAction
{
    public string ActionName { get; }

    public string? Description { get; }

    public string? Usage { get; }

    public IEnumerable<InternalActionOption> ListOfOptions { get; }

    public InternalActionRunType RunType { get; }

    public Task Execute(string[]? args);

    public void ExecuteStartup() {  }
}
