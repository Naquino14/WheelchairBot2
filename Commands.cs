using Discord;
using Discord.Commands;
using System;
using System.Text.RegularExpressions;

namespace WheelchairBot;

public static class Commands
{
    private static readonly Dictionary<ulong, GuildAudioContext> audioClients = [];

    private static readonly ulong JOE_ID = 879175038874554379ul;
    private static readonly int JOIN_LEAVE_TIMEOUT_MS = 8000;

    private static SoundDispatcher SoundDispatcher { get; set; }

    static Commands()
        => SoundDispatcher = new(audioClients);

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

    public static async Task Queue(CommandContext context, string[] args)
    {
        string queue = "";
        if (!audioClients.ContainsKey(context.Guild.Id))
        {
            await context.Message.ReplyAsync(Responses.queue_NotInChannel);
            return;
        }
        var audioClient = audioClients[context.Guild.Id];
        if (audioClient.Queue.Count == 0)
        {
            await context.Message.ReplyAsync(Responses.queue_Empty);
            return;
        }

        for (int i = 0; i < audioClient.Queue.Count; i++)
            queue += $"{i + 1:00}: {audioClient.Queue[i].Name}\n";
        await context.Message.ReplyAsync(queue);
    }

    public static async Task NowPlaying(CommandContext context, string[] args)
    {
        if (!audioClients.ContainsKey(context.Guild.Id))
        {
            await context.Message.ReplyAsync(Responses.nowplaying_NotInChannel);
            return;
        }
        var audioClient = audioClients[context.Guild.Id];
        if (audioClient.NowPlaying is null)
        {
            await context.Message.ReplyAsync(Responses.nowplaying_None);
            return;
        }
        await context.Message.ReplyAsync($"Now playing: {audioClient.NowPlaying.Name}\nhttps://youtube.com/watch?v={audioClient.NowPlaying.ID}");
    }

    public static async Task Remove(CommandContext context, string[] args)
    {
        if (args.Length != 2 || !int.TryParse(args[1], out int index) || index <= 0)
        {
            await context.Message.ReplyAsync(Responses.remove_BadArg);
            return;
        }
        index--;

        // check if context exists
        if (!audioClients.ContainsKey(context.Guild.Id))
        {
            await context.Message.ReplyAsync(Responses.remove_NoContext);
            return;
        }
        var audioClient = audioClients[context.Guild.Id];
        
        // check if user is in channel
        var channel = (context.User as IGuildUser)?.VoiceChannel;
        if (channel is null)
        {
            await context.Message.ReplyAsync(Responses.remove_UserNotInChannel);
            return;
        }

        // check if queue is empty
        if (audioClient.Queue.Count == 0)
        {
            await context.Message.ReplyAsync(Responses.remove_EmptyQueue);
            return;
        }

        // get song to remove
        if (audioClient.Queue.Count < index)
        {
            await context.Message.ReplyAsync(Responses.remove_BadArg);
            return;
        }

        // remove song
        var removedSong = audioClient.Queue[index];
        audioClient.Queue.RemoveAt(index);

        await context.Message.ReplyAsync($"Removed {removedSong.Name} at position {index} from queue.");

        if (audioClient.Queue.Count == 0)
            audioClient.IsReady = false; // set is ready false for dispatch
    }

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

        // delete all songs in queue directory
        if (Directory.Exists($@"queue/{context.Guild.Id}"))
            Directory.Delete($@"queue/{context.Guild.Id}", true);

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

        // delete all songs in queue directory
        if (Directory.Exists($@"queue/{context.Guild.Id}"))
            Directory.Delete($@"queue/{context.Guild.Id}", true);

        await context.Channel.SendMessageAsync(Responses.leave_Left);
        await context.Channel.SendMessageAsync(Responses.leave_LeftGif);
    }

    public static async Task Fart(CommandContext context, string[] args)
    {
        AudioHelper.SendAudioStream(audioClients[context.Guild.Id].AudioClient, "fart.mp3", audioClients[context.Guild.Id]);
        await context.Message.DeleteAsync();
    }

    private enum ParamType
    {
        query, 
        link
    }

    public static async Task Play(CommandContext context, string[] args)
    {
        var combinedArgs = string.Join(" ", args.Skip(1));
        var paramType = Regex.Match(combinedArgs, @"^(https:\/\/)?((www|music).)?youtube.com\/watch\?v=\w+(&list=\w+)?[^ ]$").Success ? ParamType.link : ParamType.query;
        string? videoID = null;
        string? videoName = null;
        
        switch (paramType)
        {
            case ParamType.query:
                var result = await MusicFetchHelper.Search(combinedArgs);
                /// TODO: for now only use the top result
                if (result.Count == 0)
                    await context.Channel.SendMessageAsync(string.Format(Responses.play_SearchEmpty, combinedArgs));
                videoName = result[0].name;
                videoID = result[0].id;
                break;
            case ParamType.link:
                // extract video id
                videoID = Regex.Match(combinedArgs, @"\?v=\w+").Value[3..];
                break;
        }

        // check if context exists
        if (!audioClients.ContainsKey(context.Guild.Id))
        {
            await context.Message.ReplyAsync(Responses.remove_NoContext);
            return;
        }

        var guildAudioContext = audioClients[context.Guild.Id];
        // check if user is in channel
        var channel = (context.User as IGuildUser)?.VoiceChannel;

        // check if user is in channel
        if (channel is null)
        {
            await context.Message.ReplyAsync(string.Format(Responses.play_UserNotInChannel, guildAudioContext.ConnectedChannel.Name));
            return;
        }

        using (context.Channel.EnterTypingState())
        {
            await context.Message.ReplyAsync("Downloading song, please wait...");

            await MusicFetchHelper.DownloadSong(videoID!, guildAudioContext);
            var videoInfo = guildAudioContext.Queue.Last();
            videoName = videoInfo.Name;
        }

        await context.Channel.SendMessageAsync($"Added {videoName} to position {guildAudioContext.Queue.Count}");

        if (!guildAudioContext.IsReady)
            guildAudioContext.IsReady = true;
    }

    public static async Task Restart(CommandContext context, string[] args)
    {
        // check if context exists
        if (!audioClients.ContainsKey(context.Guild.Id))
        {
            await context.Message.ReplyAsync(Responses.skip_NoContext);
            return;
        }
        var audioClient = audioClients[context.Guild.Id];

        // check if user is in channel
        var channel = (context.User as IGuildUser)?.VoiceChannel;
        if (channel is null)
        {
            await context.Message.ReplyAsync(Responses.remove_UserNotInChannel);
            return;
        }

        // check if something is playing
        if (!audioClient.IsPlaying)
        {
            await context.Message.ReplyAsync(Responses.skip_NothingPlaying);
            return;
        }

        // add now playing to the beginning of the queue
        audioClient.Queue.Insert(0, audioClient.NowPlaying!);

        // stop streaming thread
        audioClient.StreamingThreadCancellationToken?.Cancel();

        await context.Message.ReplyAsync($"Restarted {audioClient.NowPlaying!.Name}");
    }

    public static async Task Skip(CommandContext context, string[] args)
    {
        // check if context exists
        if (!audioClients.ContainsKey(context.Guild.Id))
        {
            await context.Message.ReplyAsync(Responses.skip_NoContext);
            return;
        }
        var audioClient = audioClients[context.Guild.Id];

        // check if user is in channel
        var channel = (context.User as IGuildUser)?.VoiceChannel;
        if (channel is null)
        {
            await context.Message.ReplyAsync(Responses.remove_UserNotInChannel);
            return;
        }

        // check if something is playing
        if (!audioClient.IsPlaying)
        {
            await context.Message.ReplyAsync(Responses.skip_NothingPlaying);
            return;
        }

        // stop streaming thread
        var nowPlaying = audioClient.NowPlaying;
        audioClient.StreamingThreadCancellationToken?.Cancel();

        await context.Message.ReplyAsync($"Skipped {nowPlaying!.Name}");
    }

    #region Private Helpers

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

    #endregion
}
