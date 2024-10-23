using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DiscordBot;

public static class Entry
{
    /// <summary>
    /// Some startup actions that can are executed when the console first starts. This actions are invoked externally at application launch
    /// </summary>
    private static readonly List<IStartupAction> StartupActions = [
        new StartupAction("/purge_plugins", () => {
            foreach (var plugin in Directory.GetFiles("./Data/Plugins", "*.dll", SearchOption.AllDirectories))
            {
                File.Delete(plugin);
            }

            File.Delete("./Data/Resources/plugins.json");
            Directory.Delete("./Libraries/", true);
        }),

        new StartupAction("--update-cleanup", () => {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles("./"));

            foreach (var file in files)
            {
                if (file.EndsWith(".bak"))
                    File.Delete(file);
            }

            Directory.Delete("temp");
        })
    ];

    private static readonly string logo = 
@"

   _____      _   _        _____  _                       _    ____        _   
  / ____|    | | | |      |  __ \(_)                     | |  |  _ \      | |  
 | (___   ___| |_| |__    | |  | |_ ___  ___ ___  _ __ __| |  | |_) | ___ | |_ 
  \___ \ / _ \ __| '_ \   | |  | | / __|/ __/ _ \| '__/ _` |  |  _ < / _ \| __|
  ____) |  __/ |_| | | |  | |__| | \__ \ (_| (_) | | | (_| |  | |_) | (_) | |_ 
 |_____/ \___|\__|_| |_|  |_____/|_|___/\___\___/|_|  \__,_|  |____/ \___/ \__|  
                                                                               (Console Application)
                                                                                                                                                   
";
    public static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            
            StartupActions.FirstOrDefault(action => action.Command == args[0], null)?.RunAction(args[1..]);
        }

        Console.Clear();

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine(logo);
        Console.ResetColor();


        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.AssemblyResolve += LoadFromSameFolder;

        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            string requestingAssembly = args.RequestingAssembly?.GetName().Name;
            var    folderPath         = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, $"Libraries/{requestingAssembly}");
            var    assemblyName       = new AssemblyName(args.Name).Name + ".dll";
            var    assemblyPath       = Path.Combine(folderPath, assemblyName);
            
            if (File.Exists(assemblyPath))
            {
                var fileAssembly = Assembly.LoadFrom(assemblyPath);
                return fileAssembly;
            }
            
            return null;
        }

        Program.Startup(args).Wait();

    }


}
