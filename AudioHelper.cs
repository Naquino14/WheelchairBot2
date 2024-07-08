using Discord.Audio;
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

    internal static async Task SendAudioStreamAsync(IAudioClient client, string path)
    {
        using var ffmpeg = CreateAudioProcess(path);
        using var output = (ffmpeg ?? throw new NullReferenceException("Could not spawn ffmpeg process")).StandardOutput.BaseStream;
        using var discord = client.CreatePCMStream(AudioApplication.Mixed);
        try
        {
            await output.CopyToAsync(discord);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception occured when sending audio stream to discord: {e.Message}");
        }
    }
}
