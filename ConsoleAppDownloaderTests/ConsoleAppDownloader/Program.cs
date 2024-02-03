using ConsoleAppDownloader;
using YoutubeExplode;

string destinationPath = @"D:\Filaupe\Documents\GitHub\Easy-Downloader\ConsoleAppDownloaderTests\ConsoleAppDownloader\bin\Debug\net7.0\downloads";

var youtube = new YoutubeClient();
var video = await youtube.Videos.GetAsync("https://youtu.be/2BdZ1wFTnGg?si=SHyGdEVxgKEa6tJT");

string sanitizedTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));

var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
var muxedStreams = streamManifest.GetMuxedStreams().OrderByDescending(s => s.VideoQuality).ToList();

DateTime startDownload = DateTime.Now;
long bytesRead = 0;
long fileSize = 0;

string ToDecimal(double value) => $"{value:0.00}";
//string ToDecimal(double value) => $"{value:0.00}";

double ToMB(long size) => size / 1024F / 1024F;
double CalcPercent(long value1, long value2) => value1 * 100F / value2;
double CalcMBs(long value, DateTime inicial, DateTime actual) => ToMB(value) / (actual - inicial).TotalSeconds;

void DisplayProgress()
{
    Console.WriteLine($"Baixado: {ToDecimal(ToMB(bytesRead))} MB de {ToDecimal(ToMB(fileSize))} MB - {CalcMBs(bytesRead, startDownload, DateTime.Now):0.00} MB/s - {ToDecimal(CalcPercent(bytesRead, fileSize))}%");
}

if (muxedStreams.Any())
{
    var streamInfo = muxedStreams.First();
    using var httpClient = new HttpClient();

    using var client = new HttpClient(new ProgressMessageHandler());
    using var clientHeader = new HttpClient();
    try
    {
        using var responseSize = await clientHeader.SendAsync(new HttpRequestMessage(HttpMethod.Head, streamInfo.Url));
        using var response = await client.GetAsync(streamInfo.Url, HttpCompletionOption.ResponseHeadersRead);

        if (responseSize.IsSuccessStatusCode)
        {
            if (responseSize.Content.Headers.ContentLength.HasValue)
            {
                fileSize = responseSize.Content.Headers.ContentLength.Value;
                Console.WriteLine($"Tamanho do arquivo: {ToMB(fileSize)} MB");
            }
            else
            {
                Console.WriteLine("Não foi possível obter o tamanho do arquivo.");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            string outputFilePath = Path.Combine(destinationPath, $"{sanitizedTitle}.{streamInfo.Container}");
            using var outputStream = File.Create(outputFilePath);
            byte[] buffer = new byte[8192];

            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += (sender, e) => DisplayProgress();
            timer.Start();

            while (true)
            {
                int read = await stream.ReadAsync(buffer);
                if (read <= 0)
                {
                    timer.Stop();
                    break;
                }
                bytesRead += read;
                await outputStream.WriteAsync(buffer.AsMemory(0, read));
            }

            Console.WriteLine("Download concluído.");
        }
        else
        {
            Console.WriteLine($"Falha na requisição. Código de status: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro: {ex.Message}");
    }
}
else
{
    Console.WriteLine($"No suitable video stream found for {video.Title}.");
}