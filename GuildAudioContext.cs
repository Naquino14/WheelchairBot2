using Discord;
using Discord.Audio;
using System.Diagnostics;

namespace WheelchairBot;

internal class GuildAudioContext
{
    internal GuildAudioContext(
            ulong guildId, 
            IAudioClient audioClient, 
            IVoiceChannel connectedChannel, 
            IMessageChannel? textChannel = null, 
            Thread? streamingThread = null, 
            List<VideoInfo> queue = null!, 
            bool isConnected = false, bool 
            isPlaying = false, 
            bool isReady = false
        )
    {
        GuildId = guildId;
        AudioClient = audioClient;
        ConnectedChannel = connectedChannel;
        StreamingThread = streamingThread;
        Queue = queue ?? [];
        IsConnected = isConnected;
        IsPlaying = isPlaying;
    }

    internal ulong GuildId { get; set; }

    internal IAudioClient AudioClient { get; set; }
    internal IVoiceChannel ConnectedChannel { get; set; }
    internal IMessageChannel? ContextTextChannel { get; set; }
    internal Thread? StreamingThread { get; set; }
    internal List<VideoInfo> Queue { get; set; }
    internal bool IsConnected { get; set; }
    internal bool IsPlaying { get; set; }
    internal bool IsReady { get; set; }
}

internal class VideoInfo
{
    internal VideoInfo(string name, string id)
    {
        Name = name;
        ID = id;
    }

    internal string Name { get; set; }
    internal string ID { get; set; }
}