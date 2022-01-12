﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Interfaces;

namespace CMD_Utils
{
    public class Poll : DBCommand
    {
        public string Command => "poll";

        public string Description => "Create a poll with options";

        public string Usage => "poll [This-is-question] [This-is-answer-1] [This-is-answer-2] ... ";

        public bool canUseDM => false;

        public bool canUseServer => true;

        public bool requireAdmin => true;

        public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            if (isDM) return;
            string question = message.Content.Split(' ')[1].Replace('-', ' ');
            string[] answers = PluginManager.Others.Functions.MergeStrings(message.Content.Split(' '), 2).Split(' ');
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Title = question;
            int len = answers.Length;
            for (int i = 0; i < len; i++)
                embedBuilder.AddField($"Answer {i + 1}", answers[i].Replace('-', ' '), true);
            var msg = await context.Channel.SendMessageAsync(embed: embedBuilder.Build());

            List<IEmote> emotes = new List<IEmote>();
            emotes.Add(Emoji.Parse(":one:"));
            emotes.Add(Emoji.Parse(":two:"));
            emotes.Add(Emoji.Parse(":three:"));
            emotes.Add(Emoji.Parse(":four:"));
            emotes.Add(Emoji.Parse(":five:"));
            emotes.Add(Emoji.Parse(":six:"));

            for (int i = 0; i < len; i++)
                await msg.AddReactionAsync(emotes[i]);
        }
    }
}
