using Discord.WebSocket;
using Discord;
using PluginManager.Interfaces;
namespace Holiday_events
{
    public class Holiday : DBEvent
    {
        public string name => "Holiday Events";

        public string description => "Happy Holiday";

        public async void Start(DiscordSocketClient client)
        {
            while(true)
            {
                if (DateTime.Today.Hour == 0 && DateTime.Today.Minute == 0)
                    await VerificareData(client);
                await Task.Delay(1000 * 60-1);
            }
        }

        private async Task VerificareData(DiscordSocketClient client)
        {
            string day = DateTime.Now.Day.ToString();
            string month = DateTime.Now.Month.ToString();

            if (day == "1" && month == "1")
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.Title = "Happy New Year!";
                builder.Description = $"Make way for {DateTime.Now.Year}!\nNew adventures are around the corner.";
                builder.ImageUrl = "https://i.imgur.com/AWhxExZ.jpg";
                builder.Color = Color.Gold;
                await client.GetGuild(client.Guilds.First().Id).DefaultChannel.SendMessageAsync(embed: builder.Build());

            }
            else if (day == "1" && month == "5")
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.Title = "Happy May Day!";
                builder.Description = " You have worked very hard throughout the year to meet all your goals. Now it is a day to relax and rejoice.\nSending you warm wishes on International Worker’s Day.";
                builder.ImageUrl = "https://i.imgur.com/SIIwelU.jpeg";
                builder.Color = Color.LightOrange;
                await client.GetGuild(client.Guilds.First().Id).DefaultChannel.SendMessageAsync(embed: builder.Build());
            }
            else if (day == "25" && month == "12")
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.Title = "Happy May Day!";
                builder.Description = "Wishing you and your family health, happiness, peace and prosperity this Christmas and in the coming New Year.";
                builder.ImageUrl = "https://i.imgur.com/qsDOI4t.jpg";
                builder.Color = Color.Red;
                await client.GetGuild(client.Guilds.First().Id).DefaultChannel.SendMessageAsync(embed: builder.Build());

            }
            else if (day =="1" && month == "12")
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.Title = "Romania National Day";
                builder.Description = "I wish the people of Romania a happy national day and peace and prosperity in the year ahead.";
                builder.ImageUrl = "https://i.imgur.com/vHQnFHp.jpg";
                builder.Color = Color.Blue;
                await client.GetGuild(client.Guilds.First().Id).DefaultChannel.SendMessageAsync(embed: builder.Build());
            }

            else if (day == "8" && month == "3")
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.Title = "National Womens Day";
                builder.Description = "Today we celebrate every woman on the planet. You bring so much love and beauty into our world just by being in it, and it makes everyone a little bit happier. The Sun shines brighter when you smile, ladies, so keep smiling! Happy Woman’s Day!";
                builder.ImageUrl = "https://i.imgur.com/dVzQ3rp.jpg";
                builder.Color = Color.Red;
                await client.GetGuild(client.Guilds.First().Id).DefaultChannel.SendMessageAsync(embed: builder.Build());
            }

            else if (day == "31" && month == "10")
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.Title = "Happy Halloween";
                builder.Description = "This October, may your treats be many and your tricks be few. Hope you have a sweet Halloween.";
                builder.ImageUrl = "https://i.imgur.com/cJf6EgI.jpg";
                builder.Color = Color.Orange;
                await client.GetGuild(client.Guilds.First().Id).DefaultChannel.SendMessageAsync(embed: builder.Build());
            }
        }
        
    }
}