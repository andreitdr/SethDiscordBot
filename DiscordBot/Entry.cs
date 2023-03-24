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

            Program.Startup(args);

        }
    }
}
