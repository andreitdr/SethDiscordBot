using System;
using System.Threading.Tasks;
using DiscordBotCore.Updater.Application;
using Spectre.Console;

namespace DiscordBot.Utilities;

internal static class ConsoleUtilities
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

    public static async Task ExecuteTaskWithBuiltInProgress<T>(Func<T, IProgress<float>, Task> method, T parameter, string taskMessage)
    {
        await AnsiConsole.Progress()
                         .AutoClear(false)     // Do not remove the task list when done
                         .HideCompleted(false) // Hide tasks as they are completed
                         .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
                         .StartAsync(
                             async ctx =>
                             {
                                 var task = ctx.AddTask(taskMessage);
                                 IProgress<float> progress = new Progress<float>(x => task.Value = x);
                                 await method(parameter, progress);
                                 task.Value = 100;
                             }
                         );

    }

}