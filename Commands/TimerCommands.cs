using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.VoiceNext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PomodoroBot.Commands
{
    public class AlarmData
    {
        public DateTime AlarmTime;
    }
    public class TimerCommands : BaseCommandModule
    {
        [Command("timerset")]
        [Description("UPDATEME DESCRIPTION FOR TIMER.\n" +
            "Example: timerset 1h15m30s")]
        public async Task PomoTimer(CommandContext context, TimeSpan duration)
        {
            // Connects to the command user's current voice channel.
            // If they are not in one, notify them with a message in chat.
            var vstate = context.Member?.VoiceState;
            if (vstate == null || vstate.Channel == null)
            {
                await context.Channel.SendMessageAsync("You are not connected to a voice channel.");
                return;
            }
            var interactivity = context.Client.GetInteractivity();
            var checkmarkEmoji = DiscordEmoji.FromName(context.Client, ":white_check_mark:");

            // Creates the embed to send the message in
            var displayEmbed = new DiscordEmbedBuilder()
            {
                Title = "Pomodoro Bot Timer",
                Description = $"Timer set for **{duration}**\nReact to confirm.",
                Color = new DiscordColor(238, 64, 54)
            };

            // Sends the embed and waits for a reaction to the checkmark from the user who send the command
            var message = await context.Channel.SendMessageAsync(embed: displayEmbed).ConfigureAwait(false);
            await message.CreateReactionAsync(checkmarkEmoji);

            var reactionResult = await interactivity.WaitForReactionAsync(x =>
                x.Message == message
                && x.User == context.User
                && x.Emoji == checkmarkEmoji,
                TimeSpan.FromSeconds(15)).ConfigureAwait(false);

            if (reactionResult.TimedOut)
                await context.Channel.SendMessageAsync("Timed out. Timer has been cancelled.").ConfigureAwait(false);

            // If the user reacts to the message
            else if (reactionResult.Result.Emoji == checkmarkEmoji)
            {                               
                await Join(context, vstate.Channel);
                                   
                DateTime curTime = DateTime.Now;

                // This variable is curTime, but only up to the closest second.
                DateTime newCurTime = new DateTime(
                    curTime.Year, curTime.Month, curTime.Day, curTime.Hour, curTime.Minute, curTime.Second);

                // The target time of when the timer will stop.
                DateTime waitingUntil = newCurTime.Add(duration);

                await context.Channel.SendMessageAsync($"Timer confirmed for **{duration}**.").ConfigureAwait(false);

                // Continuously compares the current time with the target time until closest second
                // and stops when the time has been reached.
                while ((DateTime.Compare(new DateTime(
                    DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                    DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second), waitingUntil)) < 0)
                {
                    await Task.Delay(750);
                }

                // Announces in the chat when target time has been reached and leaves.
                if ((DateTime.Compare(new DateTime(
                    DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                    DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second), waitingUntil)) == 0)
                {
                    // TODO: Play a sound when the timer has been reached.
                    await context.Channel.SendMessageAsync($"@everyone Time has been reached!");
                    await Task.Delay(5000);
                    await Leave(context);
                }
            }
            else
                await context.Channel.SendMessageAsync("Timer has been cancelled.").ConfigureAwait(false);
        }

        [Command("join")]
        [Description("Joins a voice channel.\n" +
            "Example: join")]
        public async Task Join(CommandContext ctx, DiscordChannel chn)
        {
            // check whether VNext is enabled
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                await ctx.Channel.SendMessageAsync("VNext is not enabled or configured.");
                return;
            }

            // check whether we aren't already connected
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc != null)
            {
                // already connected
                await ctx.Channel.SendMessageAsync("Already connected to this channel.");
                return;
            }

            // get member's voice state
            var vstat = ctx.Member?.VoiceState;
            if (vstat?.Channel == null && chn == null)
            {
                // they did not specify a channel and are not in one
                await ctx.Channel.SendMessageAsync("You are not in a voice channel.");
                return;
            }

            // channel not specified, use user's
            if (chn == null)
                chn = vstat.Channel;

            // connect
            vnc = await vnext.ConnectAsync(chn);
            //await ctx.Channel.SendMessageAsync($"Connected to `{chn.Name}`");
        }

        [Command("leave")]
        [Description("Leaves a voice channel.\n" +
            "Example: leave")]
        public async Task Leave(CommandContext ctx)
        {
            // check whether VNext is enabled
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                await ctx.Channel.SendMessageAsync("VNext is not enabled or configured.");
                return;
            }

            // check whether we are connected
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            {
                // not connected
                await ctx.Channel.SendMessageAsync("Not connected to this channel.");
                return;
            }

            // disconnect
            vnc.Disconnect();
            //await ctx.Channel.SendMessageAsync("Disconnected");
        }
    }

}
