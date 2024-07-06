using Discord;
using Discord.WebSocket;

namespace WheelchairBot;

public class Program
{
    private static DiscordSocketClient client = new(new()
    {
        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
    });

    private static CommandHandler commandHandler = new(client);

    public static async Task Main(string[] args)
    {
        client.Log += Log;

        commandHandler.SetupCommandsAsync();

        await client.LoginAsync(TokenType.Bot, File.ReadAllText("TOKEN"));
        await client.StartAsync();

        await client.SetStatusAsync(UserStatus.DoNotDisturb);

        await Task.Delay(-1);
    }

    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg);
        return Task.CompletedTask;
    }
}
