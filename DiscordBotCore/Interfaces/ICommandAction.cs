using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;

namespace DiscordBotCore.Interfaces;

public interface ICommandAction
{
    /// <summary>
    /// The name of the action. It is also used to call the action
    /// </summary>
    public string ActionName { get; }

    /// <summary>
    /// The description of the action
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// An example or a format of how to use the action
    /// </summary>
    public string? Usage { get; }

    /// <summary>
    /// Some parameter descriptions. The key is the parameter name and the value is the description. Supports nesting. Only for the Help command
    /// </summary>
    public IEnumerable<InternalActionOption> ListOfOptions { get; }

    /// <summary>
    /// The type of the action. It can be a startup action, a normal action (invoked) or both
    /// </summary>
    public InternalActionRunType RunType { get; }
    
    /// <summary>
    /// Specifies if the action requires another thread to run. This is useful for actions that are blocking the main thread.
    /// </summary>
    public bool RequireOtherThread { get; }

    /// <summary>
    /// The method that is invoked when the action is called.
    /// </summary>
    /// <param name="args">The parameters. Its best practice to reflect the parameters described in <see cref="ListOfOptions"/></param>
    public Task Execute(string[]? args);
}
