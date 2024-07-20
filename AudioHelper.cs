using Discord.Audio;
using MediaInfo.DotNetWrapper.Enumerations;
using System.Diagnostics;

namespace WheelchairBot;

internal class AudioHelper
{
    internal static Process? CreateAudioProcess(string path) => Process.Start(new ProcessStartInfo()
    {
        FileName = "ffmpeg",
        Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
        UseShellExecute = false,
        RedirectStandardOutput = true
    });

    internal static void SendAudioStream(IAudioClient client, string path, GuildAudioContext audioContext)
    {
        // create new thread
        audioContext.StreamingThread = new Thread(async () => {
            int duration = GetDuration(path) + 1;
            using var ffmpeg = CreateAudioProcess(path);
            using var output = (ffmpeg ?? throw new NullReferenceException("Could not spawn ffmpeg process")).StandardOutput.BaseStream;
            using var discord = client.CreatePCMStream(AudioApplication.Mixed);
            audioContext.IsPlaying = true;
            audioContext.StreamingThreadCancellationToken = new();

            try
            {
                await output.CopyToAsync(discord, audioContext.StreamingThreadCancellationToken.Token);
            } 
            catch (OperationCanceledException)
            {
                Console.WriteLine("Audio stream cancelled.");
            } 
            catch (Exception e)
            {
                Console.WriteLine($"Exception occured when sending audio stream to discord: {e.Message}");
            } finally
            {
                audioContext.IsPlaying = false;
            }
        });
        audioContext.StreamingThread.Start();
    }

    private static int GetDuration(string path)
    {
        using var mediaInfo = new MediaInfo.DotNetWrapper.MediaInfo();
        mediaInfo.Open(path);

        string durationStr = mediaInfo.Get(StreamKind.Audio, 0, "Duration");
        // Convert duration to double (in milliseconds)
        if (double.TryParse(durationStr, out double durationMs))
        {
            // Convert ms to s
            return (int)(durationMs / 1000.0);
        } else
            throw new Exception("Failed to retrieve duration.");
    }
}
