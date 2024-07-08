using Discord;
using Discord.Commands;

namespace WheelchairBot;

public static class Commands
{
    private static Dictionary<ulong, GuildAudioContext> audioClients = [];

    private static readonly ulong JOE_ID = 879175038874554379ul;
    private static readonly int JOIN_LEAVE_TIMEOUT_MS = 8000;

    public static async Task Say(CommandContext context, string[] args)
    {
        await context.Message.DeleteAsync();
        await context.Channel.SendMessageAsync(string.Join(" ", args.Skip(1)));
    }

    public static readonly Func<CommandContext, string[], Task> Say2 = async (context, args) =>
    {
        await context.Message.DeleteAsync();
        await context.Channel.SendMessageAsync(string.Join(" ", args.Skip(1)));
    };

    public static async Task Join(CommandContext context, string[] args)
    {
        if (args.Length > 1)
            return;

        var channel = (context.User as IGuildUser)?.VoiceChannel;

        if (channel is null)
        {
            await context.Message.ReplyAsync(Responses.join_UserNotInChannel);
            return;
        }

        // check if a guild audio context already exists
        if (audioClients.TryGetValue(context.Guild.Id, out var guildAudioContext))
        {
            if (guildAudioContext.ConnectedChannel.Id == channel.Id)
            {
                // disconnect joe and rejoin
                if (!await DisconnectFromChannel(context, channel))
                    return;

                // left sucessfully, clear server context entry, and continue as normal
                audioClients.Remove(context.Guild.Id);

            } else if (guildAudioContext.ConnectedChannel.Id != channel.Id)
            {
                await context.Message.ReplyAsync(string.Format(Responses.join_JoeInDifferentChannel, guildAudioContext.ConnectedChannel.Name));
                return;
            }                 
        }

        if (!await ConnectToChannel(context, channel))
            return;

        await context.Message.ReplyAsync(string.Format(Responses.join_JoinSuccess, channel.Name));
    }

    public static async Task Leave(CommandContext context, string[] args)
    {
        if (args.Length > 1)
            return;


        // check if joe is already in a channel
        if (!audioClients.ContainsKey(context.Guild.Id))
        {
            await context.Message.ReplyAsync(string.Format(Responses.leave_JoeNotInChannel, context.Message.Author));
            return;
        } 

        var channel = (context.User as IGuildUser)?.VoiceChannel;
        
        // check if user is in channel
        if (channel is null)
        {
            await context.Message.ReplyAsync(Responses.leave_UserNotInChannel);
            return;
        }

        if (!await DisconnectFromChannel(context, channel))
            return;

        await context.Channel.SendMessageAsync(Responses.leave_Left);
        await context.Channel.SendMessageAsync(Responses.leave_LeftGif);
    }

    private static async Task<bool> ConnectToChannel(CommandContext context, IVoiceChannel channel)
    {
        // spawn new thread to connect
        var connectTread = new Thread(async () => {
            audioClients.Add(context.Guild.Id, new(context.Guild.Id, await channel!.ConnectAsync(), channel));
        });
        connectTread.Start();
        bool connectTimedOut = !connectTread.Join(JOIN_LEAVE_TIMEOUT_MS);
        if (connectTimedOut)
        {
            await context.Message.ReplyAsync(Responses.join_Timeout);
            return false;
        }
        return true;
    }

    private static async Task<bool> DisconnectFromChannel(CommandContext context, IVoiceChannel channel)
    {
        // spawn new thread to disconnect
        var disconnectThread = new Thread(async () => {
            await audioClients[context.Guild.Id].AudioClient.StopAsync();
            await channel.DisconnectAsync();
            audioClients.Remove(context.Guild.Id);
        });
        disconnectThread.Start();
        bool disconnecttimedOut = !disconnectThread.Join(JOIN_LEAVE_TIMEOUT_MS);
        if (disconnecttimedOut)
        {
            await context.Message.ReplyAsync(Responses.leave_Timeout);
            return false;
        }
        return true;
    }
}
