using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore.Interfaces;
using PollMaker.Internal;

namespace PollMaker.SlashCommands;

public class MakePoll : IDbSlashCommand
{
    public string Name            => "make-poll";
    public string Description     => "Create an interactive poll (2-25 answers, optional timer)";
    public bool   CanUseDm        => false;
    public bool   HasInteraction  => true;

    // ─────────── slash-command schema ───────────
    public List<SlashCommandOptionBuilder> Options =>
    [
        new SlashCommandOptionBuilder
        {
            Name        = "question",
            Description = "The poll question",
            Type        = ApplicationCommandOptionType.String,
            IsRequired  = true
        },
        new SlashCommandOptionBuilder
        {
            Name        = "answers",
            Description = "Answers separated with ';' (min 2, max 25)",
            Type        = ApplicationCommandOptionType.String,
            IsRequired  = true
        },
        new SlashCommandOptionBuilder
        {
            Name        = "timed",
            Description = "Close the poll automatically after a given duration",
            Type        = ApplicationCommandOptionType.Boolean,
            IsRequired  = false
        },
        new SlashCommandOptionBuilder
        {
            Name        = "duration",
            Description = "Duration in **hours** (1-168) – required if timed = true",
            Type        = ApplicationCommandOptionType.Integer,
            MinValue    = 1,
            MaxValue    = 168,
            IsRequired  = false
        }
    ];

    // ─────────── in-memory cache ───────────
    private static readonly ConcurrentDictionary<ulong, PollState> Polls = new();

    // ─────────── slash-command handler ───────────
    public async void ExecuteServer(ILogger log, SocketSlashCommand ctx)
    {
        string q   = ctx.Data.Options.First(o => o.Name == "question").Value!.ToString()!.Trim();
        string raw = ctx.Data.Options.First(o => o.Name == "answers" ).Value!.ToString()!;
        
        bool  timed     = ctx.Data.Options.FirstOrDefault(o => o.Name == "timed")?.Value is bool b && b;
        int   hours     = ctx.Data.Options.FirstOrDefault(o => o.Name == "duration")?.Value is long l ? (int)l : 0;
        
        if (timed && hours == 0)
        {
            await ctx.RespondAsync("❗ When `timed` is **true**, you must supply a `duration` (1-168 hours).",
                                   ephemeral: true);
            return;
        }

        var opts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries)
                      .Select(a => a.Trim())
                      .Where(a => a.Length > 0)
                      .Distinct()
                      .ToArray();

        if (opts.Length < 2 || opts.Length > 25)
        {
            await ctx.RespondAsync($"❗ You must supply **2-25** answers; you supplied {opts.Length}.",
                                   ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"📊 {q}")
            .WithDescription(string.Join('\n', opts.Select((o,i) => $"{i+1}. {o}")))
            .WithColor(Color.Purple)
            .WithFooter(timed
                ? $"Click a button to vote • click again to un-vote • closes in {hours} h"
                : "Click a button to vote • click again to un-vote")
            .Build();

        var cb = new ComponentBuilder();
        for (int i = 0; i < opts.Length; i++)
            cb.WithButton(label: $"{i+1}",
                          customId: $"poll:{ctx.Id}:{i}",  // poll:{slashId}:{idx}
                          style: ButtonStyle.Secondary,
                          row: i / 5);

        await ctx.RespondAsync(embed: embed, components: cb.Build());
        var msg = await ctx.GetOriginalResponseAsync();

        var state = new PollState(q, opts);
        Polls[msg.Id] = state;
        
        if (timed)
            _ = ClosePollLaterAsync(log, msg, state, hours);
    }
    
    public async Task ExecuteInteraction(ILogger log, SocketInteraction interaction)
    {
        if (interaction is not SocketMessageComponent btn || !btn.Data.CustomId.StartsWith("poll:"))
            return;

        if (!Polls.TryGetValue(btn.Message.Id, out var poll))
        {
            await btn.RespondAsync("This poll is no longer active.", ephemeral: true);
            return;
        }

        if (!poll.IsOpen)
        {
            await btn.RespondAsync("The poll has already closed.", ephemeral: true);
            return;
        }

        var optionIdx = int.Parse(btn.Data.CustomId.Split(':')[2]);

        poll.ToggleVote(optionIdx, btn.User.Id);

        var embed = new EmbedBuilder()
            .WithTitle($"📊 {poll.Question}")
            .WithDescription(string.Join('\n',
                poll.Options.Select((o,i) => $"{i+1}. {o} — **{poll.Votes[i].Count}**")))
            .WithColor(Color.Purple)
            .WithFooter("Click a button to vote • click again to un-vote")
            .Build();

        await btn.Message.ModifyAsync(m => m.Embed = embed);
        await btn.DeferAsync();
    }
    private static async Task ClosePollLaterAsync(ILogger log, IUserMessage msg, PollState poll, int hours)
    {
        try
        {
            await Task.Delay(TimeSpan.FromHours(hours));

            poll.Close();

            var closedEmbed = new EmbedBuilder()
                .WithTitle($"📊 {poll.Question} — closed")
                .WithDescription(string.Join('\n',
                    poll.Options.Select((o,i) => $"{i+1}. {o} — **{poll.Votes[i].Count}**")))
                .WithColor(Color.DarkGrey)
                .WithFooter($"Poll closed after {hours} h • thanks for voting!")
                .Build();
            
            await msg.ModifyAsync(m =>
            {
                m.Embed       = closedEmbed;
                m.Components  = new ComponentBuilder().Build();
            });

            Polls.TryRemove(msg.Id, out _);
        }
        catch (Exception ex)
        {
            log.LogException(ex, typeof(MakePoll));
        }
    }
}
