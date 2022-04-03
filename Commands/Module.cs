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
    public class Module : BaseCommandModule
    {

        // Description examples do not include prefixes, it is added when outputting the 'help' command.

        [Command("greet")]
        [Description("Greets the user that called this command.\n" +
            "Example: greet")]
        public async Task GreetCommand(CommandContext context, [RemainingText] string name)
        {
            if (name == null)
            {
                await context.Channel.SendMessageAsync("Hello!");
            }
            else
            {
                await context.Channel.SendMessageAsync($"Hello {name}!");
            }
        }

        [Command("greet")]
        [Description("Greets a mentioned user when used with a mention.\n" +
            "Example: greet @user")]
        public async Task GreetCommand(CommandContext context, DiscordMember member)
        {
            await context.Channel.SendMessageAsync($"Hello, {member.Mention}!");
        }

        [Command("random")]
        [Description("Returns a random number with two bounds(inclusive) that the user gives.\n" +
            "Example: random 1 10")]
        public async Task RandomCommand(CommandContext context, int min, int max)
        {
            var random = new Random();

            await context.Channel.SendMessageAsync($"Your number is: {random.Next(min, max + 1)}");
        }

        [Command("time")]
        [Description("Returns the user's local time.\n" +
            "Example: time")]
        public async Task TimeCommand(CommandContext context)
        {
            var time = DateTime.Now;

            await context.Channel.SendMessageAsync($"The time is {time.ToShortTimeString()}");
        }

        [Command("respondemoji")]
        [Description("Waits for a user to react to the command message with an emoji, then replies with the emoji.\n" +
            "Example: respondemoji")]
        public async Task RespondEmojiCommand(CommandContext context)
        {
            var interactivity = context.Client.GetInteractivity();

            var message = await interactivity.WaitForReactionAsync(x => x.Channel == context.Channel).ConfigureAwait(false);

            await context.Channel.SendMessageAsync(message.Result.Emoji);
        }

        [Command("poll")]
        [Description("Creates an emoji poll with parameters time duration(h, m, s) and emoji options.\n" +
            "Example: poll 20s :sob: :smile: :skull:")]
        public async Task Poll(CommandContext context, TimeSpan duration, params DiscordEmoji[] emojiOptions)
        {
            var interactivity = context.Client.GetInteractivity();
            var options = emojiOptions.Select(x => x.ToString());

            var pollEmbed = new DiscordEmbedBuilder()
            {
                Title = "Poll",
                Description = string.Join(" ", options)
            };

            var pollMessage = await context.Channel.SendMessageAsync(embed: pollEmbed).ConfigureAwait(false);

            foreach (var option in emojiOptions)
            {
                await pollMessage.CreateReactionAsync(option).ConfigureAwait(false);
            }
            var result = await interactivity.CollectReactionsAsync(pollMessage, duration).ConfigureAwait(false);
            var results = result.Select(x => $"{x.Emoji}: {x.Total}");

            await context.Channel.SendMessageAsync(string.Join("\n", results)).ConfigureAwait(false);
        }
    }

    public class CustomHelpFormatter : DefaultHelpFormatter
    {
        protected DiscordEmbedBuilder _embed;
        protected StringBuilder _strBuilder;
        public CustomHelpFormatter(CommandContext context) : base(context)
        {
            _embed = new DiscordEmbedBuilder();
            _strBuilder = new StringBuilder();
        }

        public string AddPrefixToDesc(string description)
        {
            string prefix = ";";
            int idxColon = description.IndexOf(":");
            if (idxColon == -1) return description;

            string descPt1 = description.Substring(0, idxColon + 2);
            string descPt2 = description.Substring(idxColon + 2);
            string fullDesc = descPt1 + "`" + prefix + descPt2 + "`";

            return fullDesc;
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            // To insert prefix into the Description for the 'help' command
            string desc = AddPrefixToDesc(command.Description);
            _embed.AddField(command.Name, desc);

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> cmds)
        {
            foreach (var cmd in cmds)
            {
                string desc = AddPrefixToDesc(description: cmd.Description);
                _embed.AddField(cmd.Name, desc);
            }
            return this;
        }

        public override CommandHelpMessage Build()
        {
            _embed.Color = new DiscordColor(238, 64, 54);
            return new CommandHelpMessage(embed: _embed);
        }
    }


}
