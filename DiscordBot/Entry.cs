using PluginManager.Others;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordBot
{

    public class Entry
    {
        internal static StartupArguments startupArguments;
        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);

            static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
            {
                string folderPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "./Libraries");
                string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
                if (!File.Exists(assemblyPath)) return null;
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                return assembly;
            }

            Task.Run(async () => {
                if (!File.Exists(Functions.dataFolder + "loader.json"))
                {
                    startupArguments = new StartupArguments();
                    await Functions.SaveToJsonFile(Functions.dataFolder + "loader.json", startupArguments);
                }
                else
                    startupArguments = await Functions.ConvertFromJson<StartupArguments>(Functions.dataFolder + "loader.json");
                }).Wait();
            Program.Startup(args.Concat(startupArguments.runArgs.Split(' ')).ToArray());

        }
    }
}
