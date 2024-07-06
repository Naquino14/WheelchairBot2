using Discord;
using Discord.WebSocket;

namespace WheelchairBot
{
    public class Program
    {
        private static DiscordSocketClient client;

        public static async Task Main(string[] args)
        {
            client = new(new() 
            { 
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent 
            });

            client.Log += Log;

            client.MessageReceived += OnMessageRecieved;

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

        private static async Task OnMessageRecieved(SocketMessage msg)
        {
            Console.WriteLine($"{msg.Author.Username}: {msg.Content}");
        }
    }
}
