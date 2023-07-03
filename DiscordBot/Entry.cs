using System;
using System.IO;
using System.Reflection;

namespace DiscordBot;

public class Entry
{
    public static void Main(string[] args)
    {
        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.AssemblyResolve += LoadFromSameFolder;

        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            var folderPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                          "./Libraries");
            var assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            var assembly = Assembly.LoadFrom(assemblyPath);

            return assembly;
        }

        Program.Startup(args);
    }
}
