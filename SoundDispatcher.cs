namespace WheelchairBot;

internal class SoundDispatcher
{
    internal Dictionary<ulong, GuildAudioContext> AudioClients { get; set; }

    private Thread CheckThread { get; set; }

    internal SoundDispatcher(Dictionary<ulong, GuildAudioContext> audioClients)
    {
        AudioClients = audioClients;
        CheckThread = new Thread(async () => await Dispatch());
        CheckThread.Start();
    }

    internal async Task Dispatch()
    {
        for (; ; )
        {
            foreach (var key in AudioClients.Keys)
            {
                var client = AudioClients[key];
                if (client.IsReady && !client.IsPlaying && client.Queue.Count != 0)
                {
                    if (client.Queue.Count == 1)
                        client.IsReady = false;
                    var nextSong = client.Queue.First();
                    client.Queue.RemoveAt(0);
                    client.NowPlaying = nextSong;
                    if (client.ContextTextChannel is not null)
                        await client.ContextTextChannel.SendMessageAsync($"Now playing: {nextSong.Name}");
                    AudioHelper.SendAudioStream(client.AudioClient, $@"queue\{client.GuildId}\{nextSong.ID}.m4a", client);
                }
            }
            Thread.Sleep(500);
        }
    }
}
