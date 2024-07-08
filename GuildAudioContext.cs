using Discord;
using Discord.Audio;
using System.Diagnostics;

namespace WheelchairBot;

internal class GuildAudioContext
{
    internal GuildAudioContext(ulong guildId, IAudioClient audioClient, IVoiceChannel connectedChannel, Process? streamingThread = null, List<string> queue = null!, bool isConnected = false, bool isPlaying = false)
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
    internal Process? StreamingThread { get; set; }
    internal List<string> Queue { get; set; }
    internal bool IsConnected { get; set; }
    internal bool IsPlaying { get; set; }
}
