using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using DSharpPlus;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using PomodoroBot.Commands;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;
using DSharpPlus.CommandsNext.Exceptions;

namespace DiscordBot
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }    

        public static readonly EventId BotEventId = new EventId(42, "PomodoroBot");
        public InteractivityExtension Interactivity { get; private set; }
        public VoiceNextExtension Voice { get; set; }
     
        public static async Task MainAsync()
        {
            // Discord Client
            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = "YOUR_TOKEN_HERE",
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged
            });

           
            var interactivity = discord.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            // Set command prefix as ";" and register commands from Module.cs in Commands Folder.
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { ";" },
                EnableDms = false,
                EnableMentionPrefix = true,
                DmHelp = false,
            });

            commands.RegisterCommands<Module>();
            commands.RegisterCommands<TimerCommands>();
            commands.SetHelpFormatter<CustomHelpFormatter>();

            var voice = discord.UseVoiceNext(new VoiceNextConfiguration());

            await discord.ConnectAsync(new DiscordActivity(";help to get started"));
            await KeepHeartbeatAlive();
            await Task.Delay(-1);
        }

        private async Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            // let's log the error details
            e.Context.Client.Logger.LogError(BotEventId, $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            // let's check if the error is a result of lack
            // of required permissions
            if (e.Exception is ChecksFailedException ex)
            {
                // yes, the user lacks required permissions, 
                // let them know

                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                // let's wrap the response into an embed
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have the permissions required to execute this command.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.RespondAsync(embed);
            }
        }

        public static async Task KeepHeartbeatAlive()
        {
            int counter = 0;
            while (true)
            {
                counter++;
                await Task.Delay(1000);
                if (counter % 25 == 0)
                {
                    Console.WriteLine("Heartbeat");
                }
            }
        }
    }
}
