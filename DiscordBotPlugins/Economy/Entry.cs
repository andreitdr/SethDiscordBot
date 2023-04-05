using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PluginManager.Interfaces;
using PluginManager.Database;

namespace Economy
{
    public class EntryEvent : DBEvent
    {
        public string Name => "Economy Plugin Engine";

        public string Description => "The economy plugin main engine";

        public async void Start(global::Discord.WebSocket.DiscordSocketClient client)
        {
            Console.WriteLine("Economy Plugin Engine Started");
            Directory.CreateDirectory(PluginManager.Others.Functions.dataFolder + "/Economy");
            Engine.Database = new SqlDatabase(PluginManager.Others.Functions.dataFolder + "/Economy/Economy.db");
            await Engine.Database.Open();
            await Engine.Database.CreateTableAsync("UserBank", "UserID INT", "Balance FLOAT");

            client.Disconnected += (e) =>
            {
                Engine.Database.Stop();
                return Task.CompletedTask;
            };
        }
    }
}