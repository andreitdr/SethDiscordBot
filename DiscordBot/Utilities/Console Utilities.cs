using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DiscordBotCore.Online;
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

    public static async Task ExecuteTaskWithBuiltInProgress(Func<IProgress<float>, Task> method, string taskMessage)
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
                                 await method(progress);
                                 task.Value = 100;
                             }
                         );

    }

    public static async Task ExecuteParallelDownload(Func<HttpClient, string, string, IProgress<float>, Task> method, HttpClient client,
        List<Tuple<string, string>> parameters, string taskMessage)
    {
        await AnsiConsole.Progress().AutoClear(false).HideCompleted(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
            .StartAsync(async ctx =>
            {
                var tasks = new List<Task>();
                foreach (var (location, url) in parameters)
                {
                    var task = ctx.AddTask(taskMessage + " " + url);
                    IProgress<float> progress = new Progress<float>(x => task.Value = x * 100);
                    tasks.Add(method(client, url, location, progress));
                }
                
                await Task.WhenAll(tasks);
            });
    }
}