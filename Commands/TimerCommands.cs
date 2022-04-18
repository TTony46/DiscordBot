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
    // Default Pomodoro timer settings
    public class AlarmData
    {
        public DateTime AlarmTime;
        public static TimeSpan AlarmDuration = TimeSpan.FromMinutes(1);
        public static TimeSpan AlarmShortBreak = TimeSpan.FromMinutes(1);
        public static TimeSpan AlarmLongBreak = TimeSpan.FromMinutes(1);        
        public static string AlarmFilePathFFMPEG = @"C:\Users\Tony\Desktop\PomodoroBot\ffmpeg\bin\ffmpeg.exe";
        public static string AlarmFilePathMP3 = @"C:\Users\Tony\Desktop\PomodoroBot\AlarmSound.mp3";
        public static bool AlarmOn = true;
    }
    public class TimerCommands : BaseCommandModule
    {
        [Command("settings")]
        [Description("Change the time interval settings for the Pomodoro Timer\n" +
            "Example: settings")]
        public async Task ChangeSettings(CommandContext context)
        {
            var interactivity = context.Client.GetInteractivity();
            var redSquareEmoji = DiscordEmoji.FromName(context.Client, ":red_square:");
            var blueSquareEmoji = DiscordEmoji.FromName(context.Client, ":blue_square:");
            var greenSquareEmoji = DiscordEmoji.FromName(context.Client, ":green_square:");
            var greenCheckmarkEmoji = DiscordEmoji.FromName(context.Client, ":white_check_mark:");
            var redXMarkEmoji = DiscordEmoji.FromName(context.Client, ":x:");

            bool userContinue;
            do
            {
                userContinue = true;
                var displayEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Current Settings",
                    Description =
                        $"{redSquareEmoji} Work Interval: **{AlarmData.AlarmDuration}**\n\n" +
                        $"{blueSquareEmoji} Short Break: **{AlarmData.AlarmShortBreak}**\n\n" +
                        $"{greenSquareEmoji} Long Break: **{AlarmData.AlarmLongBreak}**\n\n" +
                        "React to the corresponding emoji to change the setting",
                    Color = new DiscordColor(238, 64, 54)
                };

                var message = await context.Channel.SendMessageAsync(embed: displayEmbed).ConfigureAwait(false);
                await message.CreateReactionAsync(redSquareEmoji);
                await Task.Delay(250);
                await message.CreateReactionAsync(blueSquareEmoji);
                await Task.Delay(250);
                await message.CreateReactionAsync(greenSquareEmoji);

                TimeSpan secondsUntilTimedOut = TimeSpan.FromSeconds(10);

                var reactionResult = await interactivity.WaitForReactionAsync(x =>
                    x.Message == message
                    && x.User == context.User
                    && (x.Emoji == redSquareEmoji
                    || x.Emoji == blueSquareEmoji
                    || x.Emoji == greenSquareEmoji),
                    secondsUntilTimedOut).ConfigureAwait(false);

                if (reactionResult.TimedOut)
                {
                    await context.Channel.SendMessageAsync("**Setting change has timed out.**").ConfigureAwait(false);
                    TimeSpan timeToResetReaction = TimeSpan.FromSeconds(1);
                    secondsUntilTimedOut = timeToResetReaction;
                    return;
                }

                bool isValid = false;
                if (reactionResult.Result.Emoji == null) return;

                isValid = (reactionResult.Result.Emoji == redSquareEmoji
                            || reactionResult.Result.Emoji == blueSquareEmoji
                            || reactionResult.Result.Emoji == greenSquareEmoji);

                string intervalType = "";
                if (reactionResult.Result.Emoji == redSquareEmoji) intervalType = "Work Interval";
                else if (reactionResult.Result.Emoji == blueSquareEmoji) intervalType = "Short Break";
                else if (reactionResult.Result.Emoji == greenSquareEmoji) intervalType = "Long Break";

                TimeSpan newUserTime = TimeSpan.FromMinutes(0);

                if (isValid)
                {
                    await context.Channel.SendMessageAsync(
                        $"How many minutes did you want to set the {intervalType} as? i.e : 45").ConfigureAwait(false); ;
                    var response = await interactivity.WaitForMessageAsync(x =>
                        x.Channel == context.Channel).ConfigureAwait(false);

                    int result = Convert.ToInt32(response.Result.Content);
                    newUserTime = TimeSpan.FromMinutes(result);
                }

                // If the user reacts to the message
                if (reactionResult.Result.Emoji == redSquareEmoji)
                {
                    AlarmData.AlarmDuration = newUserTime;
                    await context.Channel.SendMessageAsync($"{intervalType} is now: " +
                        $"**{AlarmData.AlarmDuration}**.").ConfigureAwait(false);

                    var continueMsg = await context.Channel.SendMessageAsync("Do you want to change any other settings?");
                    await continueMsg.CreateReactionAsync(greenCheckmarkEmoji);
                    await Task.Delay(250);
                    await continueMsg.CreateReactionAsync(redXMarkEmoji);
                    await Task.Delay(250);

                    var continueResult = await interactivity.WaitForReactionAsync(x =>
                        x.Message == continueMsg
                        && x.User == context.User
                        && (x.Emoji == greenCheckmarkEmoji
                        || x.Emoji == redXMarkEmoji),
                        secondsUntilTimedOut).ConfigureAwait(false);

                    if (continueResult.TimedOut)
                    {
                        await context.Channel.SendMessageAsync("**Setting change has timed out.**").ConfigureAwait(false);
                        TimeSpan timeoutSeconds = TimeSpan.FromSeconds(1);
                        secondsUntilTimedOut = timeoutSeconds;
                        return;
                    }

                    if (continueResult.Result.Emoji == greenCheckmarkEmoji) userContinue = true;
                    else if (continueResult.Result.Emoji == redXMarkEmoji)
                    {
                        userContinue = false;
                        TimeSpan timeToResetReaction = TimeSpan.FromMilliseconds(250);
                        secondsUntilTimedOut = timeToResetReaction;
                        await context.Channel.SendMessageAsync("**Settings have been confirmed.**").ConfigureAwait(false);
                        return;
                    }
                }
                else if (reactionResult.Result.Emoji == blueSquareEmoji)
                {
                    AlarmData.AlarmShortBreak = newUserTime;
                    await context.Channel.SendMessageAsync($"{intervalType} is now: " +
                        $"**{AlarmData.AlarmShortBreak}**.").ConfigureAwait(false);

                    var continueMsg = await context.Channel.SendMessageAsync("Do you want to change any other settings?");
                    await continueMsg.CreateReactionAsync(greenCheckmarkEmoji);
                    await Task.Delay(250);
                    await continueMsg.CreateReactionAsync(redXMarkEmoji);
                    await Task.Delay(250);

                    var continueResult = await interactivity.WaitForReactionAsync(x =>
                        x.Message == continueMsg
                        && x.User == context.User
                        && (x.Emoji == greenCheckmarkEmoji
                        || x.Emoji == redXMarkEmoji),
                        secondsUntilTimedOut).ConfigureAwait(false);

                    if (continueResult.TimedOut)
                    {
                        await context.Channel.SendMessageAsync("**Setting change has timed out.**").ConfigureAwait(false);
                        TimeSpan timeoutSeconds = TimeSpan.FromSeconds(1);
                        secondsUntilTimedOut = timeoutSeconds;
                        return;
                    }

                    if (continueResult.Result.Emoji == greenCheckmarkEmoji) userContinue = true;
                    else if (continueResult.Result.Emoji == redXMarkEmoji)
                    {
                        userContinue = false;
                        TimeSpan timeToResetReaction = TimeSpan.FromMilliseconds(250);
                        secondsUntilTimedOut = timeToResetReaction;
                        await context.Channel.SendMessageAsync("**Settings have been confirmed.**").ConfigureAwait(false);
                        return;
                    }
                }
                else if (reactionResult.Result.Emoji == greenSquareEmoji)
                {
                    AlarmData.AlarmLongBreak = newUserTime;
                    await context.Channel.SendMessageAsync($"{intervalType} is now: " +
                        $"**{AlarmData.AlarmLongBreak}**.").ConfigureAwait(false);

                    var continueMsg = await context.Channel.SendMessageAsync("Do you want to change any other settings?");
                    await continueMsg.CreateReactionAsync(greenCheckmarkEmoji);
                    await Task.Delay(250);
                    await continueMsg.CreateReactionAsync(redXMarkEmoji);
                    await Task.Delay(250);

                    var continueResult = await interactivity.WaitForReactionAsync(x =>
                        x.Message == continueMsg
                        && x.User == context.User
                        && (x.Emoji == greenCheckmarkEmoji
                        || x.Emoji == redXMarkEmoji),
                        secondsUntilTimedOut).ConfigureAwait(false);

                    if (continueResult.TimedOut)
                    {
                        await context.Channel.SendMessageAsync("**Setting change has timed out.**").ConfigureAwait(false);
                        TimeSpan timeoutSeconds = TimeSpan.FromSeconds(1);
                        secondsUntilTimedOut = timeoutSeconds;
                        return;
                    }
                    if (continueResult.Result.Emoji == greenCheckmarkEmoji) userContinue = true;
                    else if (continueResult.Result.Emoji == redXMarkEmoji)
                    {
                        userContinue = false;
                        TimeSpan timeToResetReaction = TimeSpan.FromMilliseconds(250);
                        secondsUntilTimedOut = timeToResetReaction;
                        await context.Channel.SendMessageAsync("**Settings have been confirmed.**").ConfigureAwait(false);
                        return;
                    }
                }
                else
                {
                    await context.Channel.SendMessageAsync("**Settings change cancelled.**").ConfigureAwait(false);
                    userContinue = false;
                }
            } while (userContinue);
        }

        [Command("pomodoro")]
        [Description("UPDATEME DESCRIPTION FOR TIMER.\n" +
            "Example: pomodoro")]
        
        public async Task Pomodoro(CommandContext context)
        {
            AlarmData.AlarmOn = true;
            while (AlarmData.AlarmOn)
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
                var redXMarkEmoji = DiscordEmoji.FromName(context.Client, ":x:");
                var duration = AlarmData.AlarmDuration;

                // Creates the embed to send the message in
                var displayEmbed = new DiscordEmbedBuilder()
                {
                    Title = "Pomodoro Bot Timer",
                    Description =
                        $"Timer set with settings:\n\n" +
                        $"Work Interval: **{AlarmData.AlarmDuration}**\n\n" +
                        $"Short Break: **{AlarmData.AlarmShortBreak}**\n\n" +
                        $"Long Break: **{AlarmData.AlarmLongBreak}**\n\n" +
                        $"React to confirm.",
                    Color = new DiscordColor(238, 64, 54)
                };

                var message = await context.Channel.SendMessageAsync(embed: displayEmbed).ConfigureAwait(false);
                await message.CreateReactionAsync(checkmarkEmoji);
                await Task.Delay(250);
                await message.CreateReactionAsync(redXMarkEmoji);

                TimeSpan secondsUntilTimedOut = TimeSpan.FromSeconds(10);

                var reactionResult = await interactivity.WaitForReactionAsync(x =>
                    x.Message == message
                    && x.User == context.User
                    && (x.Emoji == checkmarkEmoji
                    || x.Emoji == redXMarkEmoji),
                    secondsUntilTimedOut).ConfigureAwait(false);

                if (reactionResult.TimedOut)
                {
                    await context.Channel.SendMessageAsync(
                        "**Timed out. Timer has been cancelled.**").ConfigureAwait(false);
                    TimeSpan timeToResetReaction = TimeSpan.FromSeconds(1);
                    secondsUntilTimedOut = timeToResetReaction;
                    return;
                }

                // If the user reacts to the message
                else if (reactionResult.Result.Emoji == checkmarkEmoji)
                {
                    // Sets an active Pomodoro session.
                    await context.Channel.SendMessageAsync("**Timer confirmed.**").ConfigureAwait(false);

                    // User might want to stop the timer midway anytime, hence the many
                    // checkpoints that check if the timer is still active. 
                    while (AlarmData.AlarmOn)
                    {
                        // Four Pomodoros before a large break
                        for (int i = 1; i <= 4 && AlarmData.AlarmOn; i++)
                        {
                            await context.Channel.SendMessageAsync(
                                $"Pomodoro **{i}** has started.").ConfigureAwait(false);

                            await Timer(context, duration, vstate.Channel);

                            if (!AlarmData.AlarmOn) { return; }
                            // Announces and sets a short break when work time is finished
                            string shortBreakString = TimeToStringFormatted(AlarmData.AlarmShortBreak);

                            await context.Channel.SendMessageAsync(
                                $"Short Break: Take a break for **{shortBreakString}**.").ConfigureAwait(false);

                            await Timer(context, AlarmData.AlarmShortBreak, vstate.Channel);
                        }

                        if (!AlarmData.AlarmOn) { return; }

                        //After 4 work intervals, display and set a long break
                        string longBreakString = TimeToStringFormatted(AlarmData.AlarmLongBreak);

                        await context.Channel.SendMessageAsync(
                                $"Long Break: Take a break for **{longBreakString}**.").ConfigureAwait(false);

                        await Timer(context, AlarmData.AlarmLongBreak, vstate.Channel);
                    }
                }

                else if (reactionResult.Result.Emoji == redXMarkEmoji)
                {
                    await context.Channel.SendMessageAsync("**Timer cancelled.**").ConfigureAwait(false);

                    TimeSpan timeoutSeconds = TimeSpan.FromSeconds(1);
                    secondsUntilTimedOut = timeoutSeconds;
                    return;
                }
                else
                    await context.Channel.SendMessageAsync("**Timer has been cancelled.**").ConfigureAwait(false);
            }            
        }

        [Command("timerset")]
        [Description("UPDATEME DESCRIPTION FOR TIMER.\n" +
            "Example: timerset 1h15m30s ChannelName")]
        public async Task Timer(CommandContext context, TimeSpan duration, DiscordChannel channel)
        {
            if (AlarmData.AlarmOn)
            {
                await Join(context, channel);

                DateTime curTime = DateTime.Now;

                // This variable is curTime, but only up to the closest second.
                DateTime newCurTime = new DateTime(
                    curTime.Year, curTime.Month, curTime.Day, curTime.Hour, curTime.Minute, curTime.Second);

                // The target time of when the timer will stop.
                DateTime waitingUntil = newCurTime.Add(duration);

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
                    DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second), waitingUntil)) == 0
                    && AlarmData.AlarmOn)
                {
                    await context.Channel.SendMessageAsync($"@everyone Time has been reached!");
                    await Play(context, AlarmData.AlarmFilePathMP3);
                }
            }            
        }

        [Command("play")]
        [Description("Plays an audio file.")]
        public async Task Play(CommandContext ctx, [RemainingText, Description("Full path to the file to play.")] string filename)
        {
            // check whether VNext is enabled
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }

            // check whether we aren't already connected
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            {
                // already connected
                await ctx.RespondAsync("Not connected in this guild.");
                return;
            }

            // check if file exists
            if (!File.Exists(filename))
            {
                // file does not exist
                await ctx.RespondAsync($"File `{filename}` does not exist.");
                return;
            }

            // wait for current playback to finish
            while (vnc.IsPlaying)
                await vnc.WaitForPlaybackFinishAsync();

            // play
            Exception exc = null;
            //await ctx.Message.RespondAsync($"Playing `{filename}`");

            try
            {
                await vnc.SendSpeakingAsync(true);

                var psi = new ProcessStartInfo
                {
                    FileName = AlarmData.AlarmFilePathFFMPEG,
                    Arguments = $@"-i ""{filename}"" -ac 2 -f s16le -ar 48000 pipe:1 -loglevel quiet",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var ffmpeg = Process.Start(psi);
                var ffout = ffmpeg.StandardOutput.BaseStream;

                var txStream = vnc.GetTransmitSink();
                await ffout.CopyToAsync(txStream);
                await txStream.FlushAsync();
                await vnc.WaitForPlaybackFinishAsync();
            }
            catch (Exception ex) { exc = ex; }
            finally
            {
                await vnc.SendSpeakingAsync(false);
                //await ctx.Message.RespondAsync($"Finished playing `{filename}`");
            }

            if (exc != null)
                await ctx.RespondAsync($"An exception occured during playback: `{exc.GetType()}: {exc.Message}`");
        }

        [Command("stoptimer")]
        [Description("Leaves the voice channel.\n" +
            "Example: stoptimer")]
        public async Task StopTimer(CommandContext ctx)
        {
            //turns the alarm off.
            AlarmData.AlarmOn = false;

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
            await ctx.Channel.SendMessageAsync("Timer has been stopped.");
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

        public string TimeToStringFormatted(TimeSpan time)
        {
            string timeFormattedHoursMinutes = time.ToString(@"hh\:mm");
            string hours = "";
            string minutes = "";
            

            if (timeFormattedHoursMinutes[0] != '0')
            {
                hours =
                    timeFormattedHoursMinutes.Substring(0, 2) + " hours";
            }

            // If the first digit (hours) of the time is 0, but the second digit (hours) is not zero, 
            // hoursFromLongBreal something like "5 hours".
            if (timeFormattedHoursMinutes[0] == '0' &&
                timeFormattedHoursMinutes[1] != '0')
            {
                // for the case of only 1 hour, hours-> hour . 
                if (timeFormattedHoursMinutes[1] == '1')
                {
                    hours =
                        timeFormattedHoursMinutes[1] + " hour";
                }
                else
                    hours =
                        timeFormattedHoursMinutes[1] + " hours";
            }

            //MINUTES from format 20:22 with 20 hours and 22 minutes
            if (timeFormattedHoursMinutes[3] != '0')
            {
                minutes =
                    timeFormattedHoursMinutes.Substring(3, 2) + " minutes";
            }

            if (timeFormattedHoursMinutes[3] == '0' &&
                timeFormattedHoursMinutes[4] != '0')
            {
                if (timeFormattedHoursMinutes[4] == '1')
                {
                    minutes =
                        timeFormattedHoursMinutes[4] + " minute";
                }
                else
                    minutes =
                        timeFormattedHoursMinutes[4] + " minutes";
            }

            // adds to the string depending on if there are hours and/or minutes
            string concatenatedTimeString = "";
            bool hoursAreGiven = (hours != "");
            bool minutesAreGiven = (minutes != "");

            if (hoursAreGiven && minutesAreGiven)
            {
                concatenatedTimeString = hours + " and " + minutes;
            }
            else if ((!hoursAreGiven) && minutesAreGiven)
            {
                concatenatedTimeString = minutes;
            }
            else if (hoursAreGiven && (!minutesAreGiven))
            {
                concatenatedTimeString = hours;
            }

            return concatenatedTimeString;
        }
    }

}
