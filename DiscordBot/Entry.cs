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
        new StartupAction("/purge_plugins", (args) => {
            foreach (var plugin in Directory.GetFiles("./Data/Plugins", "*.dll", SearchOption.AllDirectories))
            {
                File.Delete(plugin);
            }
        })
    ];

    private static readonly string logo =
#if DEBUG
@"

   _____      _   _       _____  _                       _   ____        _   
  / ____|    | | | |     |  __ \(_)                     | | |  _ \      | |  
 | (___   ___| |_| |__   | |  | |_ ___  ___ ___  _ __ __| | | |_) | ___ | |_ 
  \___ \ / _ \ __| '_ \  | |  | | / __|/ __/ _ \| '__/ _` | |  _ < / _ \| __|
  ____) |  __/ |_| | | | | |__| | \__ \ (_| (_) | | | (_| | | |_) | (_) | |_ 
 |_____/ \___|\__|_| |_| |_____/|_|___/\___\___/|_|  \__,_| |____/ \___/ \__|  
                                                                               (Debug)
                                                                                                                                                   
";
#else
@"

   _____      _   _       _____  _                       _   ____        _   
  / ____|    | | | |     |  __ \(_)                     | | |  _ \      | |  
 | (___   ___| |_| |__   | |  | |_ ___  ___ ___  _ __ __| | | |_) | ___ | |_ 
  \___ \ / _ \ __| '_ \  | |  | | / __|/ __/ _ \| '__/ _` | |  _ < / _ \| __|
  ____) |  __/ |_| | | | | |__| | \__ \ (_| (_) | | | (_| | | |_) | (_) | |_ 
 |_____/ \___|\__|_| |_| |_____/|_|___/\___\___/|_|  \__,_| |____/ \___/ \__|  
                                                                               
                                                                                                                                                   
";
#endif
    public static void Main(string[] args)
    {
#if DEBUG
        if (args.Length > 0)
        {
            StartupActions.FirstOrDefault(action => action.Command == args[0], null)?.RunAction(args[..1]);
        }

#endif

        Console.Clear();

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine(logo);
        Console.ResetColor();


        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.AssemblyResolve += LoadFromSameFolder;

        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            var folderPath   = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "./Libraries");
            var assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            var assembly = Assembly.LoadFrom(assemblyPath);

            return assembly;
        }

        Program.Startup(args).Wait();
    }
}
