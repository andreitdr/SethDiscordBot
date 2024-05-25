using Discord.WebSocket;

using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;

using DiscordBotUI_Windows.WindowsForms;

namespace DiscordBotUI
{

    public class DiscordEventUI : DBEvent
    {
        public string Name => "DiscordUI";

        public bool RequireOtherThread => true;
        public string Description => "Discord UI desc";

        public void Start(DiscordSocketClient client)
        {
            Thread thread = new Thread(() =>
            {
                Application.Run(new MainWindow());
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }

    public class ConsoleStartAction : ICommandAction
    {
        public string ActionName => "ui";

        public string? Description => "UI Set of actions";

        public string? Usage => "ui <option>";

        public IEnumerable<InternalActionOption> ListOfOptions => [
                new InternalActionOption("start", "Starts the UI")
        ];

        public InternalActionRunType RunType => InternalActionRunType.ON_CALL;

        public Task Execute(string[]? args)
        {
            if(args == null || args.Length == 0)
            {
                Console.WriteLine("Please provide an option");
                return Task.CompletedTask;
            }

            if(args[0] == "start")
            {
                Thread thread = new Thread(() =>
                {
                    Application.Run(new MainWindow());
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }

            return Task.CompletedTask;
        }
    }
}