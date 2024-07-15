using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Diagnostics;

namespace WheelchairBot;

internal static class MusicFetchHelper
{
    private static readonly string TOKEN = File.ReadAllText("YT_TOKEN");
    internal static async Task<List<(string name, string id)>> Search(string query)
    {
        var service = new YouTubeService(new BaseClientService.Initializer() { 
            ApiKey = TOKEN,
            ApplicationName = "WheelchairBot"
        });

        var request = service.Search.List("snippet");
        request.Q = query;
        request.MaxResults = 6;

        var response = await request.ExecuteAsync();

        var results = new List<(string name, string id)>();
        foreach (var result in response.Items)
            if (result.Id.Kind == "youtube#video")
                results.Add((result.Snippet.Title, result.Id.VideoId));

        return results;
    }

    internal static async Task DownloadSong(string id, GuildAudioContext audioContext) {
        string url = $"https://www.youtube.com/watch?v={id}";

        string destination = @$"queue\{audioContext.GuildId}";

        if (!Directory.Exists(destination))
            Directory.CreateDirectory(destination);

        // download the song in this thread
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "yt-dlp",
            Arguments = $"-x -f bestaudio --audio-format m4a -o \"{destination}\\%(title)s.%(ext)s\" {url}",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        })!;
        await process.WaitForExitAsync();
        var output = process.StandardOutput.ReadToEnd();

        Console.WriteLine($"Song download out: {output}");

        // get the list of downloaded files
        string[] downloadedVideos = Directory.GetFiles(destination, "*.m4a");
        DateTime latest = DateTime.MinValue;
        int latestIndex = 0;

        // get the video we just downloaded
        for (int i = 0; i < downloadedVideos.Length; i++)
            if (File.GetCreationTime(downloadedVideos[i]) > latest)
            {
                latest = File.GetCreationTime($@"{destination}\{downloadedVideos[i]}");
                latestIndex = i;
            }

        string videoName = Path.GetFileNameWithoutExtension(downloadedVideos[latestIndex]) ?? throw new FileNotFoundException($"Could not find {downloadedVideos[latestIndex]}");

        // rename that file to the video id
        string videoReName = Path.Combine(destination, $"{id}.m4a");

        if (!File.Exists(videoReName))
            File.Move(@$"{destination}\{videoName}.m4a", videoReName);

        File.Delete($@"{destination}\{videoName}.m4a");

        audioContext.Queue.Add(new(videoName, id));
    }
}
