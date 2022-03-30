using System;
using System.Collections.Generic;
using DSharpPlus;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using PomodoroBot.Commands;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace DiscordBot
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }
        public InteractivityExtension Interactivity { get; private set; }
        static async Task MainAsync()
        {
            // Discord Client
            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = "INSERT TOKEN HERE",
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
                DmHelp = true,
            });

            

            commands.RegisterCommands<Module>();
            commands.SetHelpFormatter<CustomHelpFormatter>();

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        // https://dsharpplus.github.io/articles/commands/intro.html
    }
}
