using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace DiscordBot.Utilities;

public static class ConsoleUtilities
{
    public static async Task<T> ExecuteWithProgressBar<T>(Task<T> function, string message)
    {
        T result = default;
        await AnsiConsole.Progress()
            .AutoClear(true)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
            .StartAsync(
                async ctx =>
                {
                    var task = ctx.AddTask(message);
                    task.IsIndeterminate = true;
                    result = await function;
                    task.Increment(100);
                }
            );


        return result;
    }
}