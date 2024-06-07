using Discord.WebSocket;

using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;

using DiscordBotUI_Windows;
using DiscordBotUI_Windows.WindowsForms;

namespace DiscordBotUI
{
    public class DiscordUICommand : ICommandAction
    {
        public string ActionName => "ui";

        public string? Description => "UI Set of actions";

        public string? Usage => "ui <option>";

        public IEnumerable<InternalActionOption> ListOfOptions => [
                new InternalActionOption("start", "Starts the UI")
        ];
         
        public InternalActionRunType RunType => InternalActionRunType.BOTH;

        public async void ExecuteStartup()
        {
            Directory.CreateDirectory(Path.Combine(DiscordBotCore.Application.CurrentApplication.DataFolder, "DiscordBotUI"));
            await Execute(["start"]);
        }

        public async Task Execute(string[]? args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("Please provide an option");
                return;
            }

            if (args[0] == "theme")
            {
                if (args.Length == 1)
                {
                    Console.WriteLine("Please provide a theme name");
                    return;
                }

                if (args[1] == "save")
                {
                    if (args.Length == 2)
                    {
                        Console.WriteLine("Please provide a theme name");
                        return;
                    }

                    await Config.ThemeManager.SaveThemeToFile(args[2]);
                    Console.WriteLine("Theme saved");
                }

                if (args[1] == "set")
                {
                    if (args.Length == 2)
                    {
                        Console.WriteLine("Please provide a theme name");
                        return;
                    }

                    Config.ThemeManager.SetTheme(args[2]);
                }

                if (args[1] == "list")
                {
                    foreach (var theme in Config.ThemeManager._InstalledThemes)
                    {
                        Console.WriteLine(theme.Name);
                    }
                }

                return;
            }

            if (args[0] == "start")
            {
                await StartUI();
            }
        }


        private async Task StartUI()
        {
            await Config.ApplicationSettings.LoadFromFile();

            await Config.ThemeManager.LoadThemesFromThemesFolder();

            if (Config.ApplicationSettings.ContainsKey("AppTheme"))
            {
                Config.ThemeManager.SetTheme(Config.ApplicationSettings["AppTheme"]);
            }
            else Config.ApplicationSettings.Add("AppTheme", "Default");

            await Config.ApplicationSettings.SaveToFile();

            Thread thread = new Thread(() =>
            {
                MainWindow mainWindow = new MainWindow();
                Config.ThemeManager.SetFormTheme(Config.ThemeManager.CurrentTheme, mainWindow);
                Application.Run(mainWindow);

            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }
    public class DiscordEventUI2 : DBEvent
    {
        public string Name => "DiscordUI";

        public bool RequireOtherThread => true;
        public string Description => "Discord UI desc";

        public async void Start(DiscordSocketClient client)
        {

        }
    }
}