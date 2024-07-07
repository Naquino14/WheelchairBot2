using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WheelchairBot;

public partial class CommandHandler(DiscordSocketClient client)
{
    private readonly DiscordSocketClient client = client;

    [GeneratedRegex(@"^j!\w.*$")]
    private static partial Regex CommandRegex();

    private Dictionary<string, Func<CommandContext, string[], Task>> commands = [];

    private void RegisterCommand(Func<CommandContext, string[], Task> command, string name)
        => this.commands.Add(name.ToLower(), command);

    public void SetupCommandsAsync()
    {
        client.MessageReceived += HandleCommandsAsync;

        foreach (var method in typeof(Commands).GetMethods(BindingFlags.Public | BindingFlags.Static))
            RegisterCommand(method.CreateDelegate<Func<CommandContext, string[], Task>>(), method.Name.ToLower());
    }

    private async Task HandleCommandsAsync(SocketMessage messageParam)
    {
        if (messageParam is not SocketUserMessage message)
            return;

        if (message.Author.IsBot || !CommandRegex().IsMatch(message.Content))
            return;

        var args = message.Content.Split(' ');
        args[0] = args[0][2..];

        if (!commands.TryGetValue(args[0].ToLower(), out Func<CommandContext, string[], Task>? command))
        {
            Console.WriteLine($"Command {args[0]} does not exist.");
            return;
        }

        var context = new CommandContext(client, message);
        await command.Invoke(context, args);
    }
}
