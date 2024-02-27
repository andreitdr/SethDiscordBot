using System.Threading.Tasks;
using Spectre.Console;

namespace PluginManager.UX.Other;

internal class Console: IOutputModel
{

    public Task ShowMessageBox(string title, string message, MessageBoxType type)
    {
        AnsiConsole.Markup(title);
        AnsiConsole.Markup(message);


        return Task.CompletedTask;
    }

    public Task<string> ShowInputBox(string title, string message)
    {
        AnsiConsole.MarkupLine(title);

        var input = AnsiConsole.Ask<string>(message);

        return Task.FromResult(input);
    }

    public Task ShowMessageBox(string message)
    {
        AnsiConsole.MarkupLine(message);
        return Task.CompletedTask;
    }

    public Task<int> ShowMessageBox(string title, string message, MessageBoxButtons buttons, bool isWarning)
    {
        AnsiConsole.MarkupLine(title);
        AnsiConsole.MarkupLine(message);

        return Task.FromResult(0);
    }

    public Task ShowNotification(string title, string message, int timeout_seconds = 5)
    {
        return Task.CompletedTask;
    }
}
