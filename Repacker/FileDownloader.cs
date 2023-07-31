using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Configuration.Models;
using Serilog;
using Shared;
using Shared.Logger;

namespace RepackerRoot;

public class FileDownloader
{
    private readonly string _tempDir;
    private readonly string? _userAgent;

    public FileDownloader(TempDirConfig tempDirConfig, WebConfig webConfig)
    {
        tempDirConfig.Bind();
        _tempDir = tempDirConfig.Path!;

        webConfig.Bind();
        _userAgent = webConfig.UserAgent;
    }

    public async Task<string> DownloadAsync(string url)
    {
        string savePath = string.Empty;

        using HttpClient httpClient = new();

        WebHelpers.TrySetUserAgent(httpClient, _userAgent);

        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            using HttpContent content = response.Content;

            savePath = PathHelpers.PickRandomFilePath(
                _tempDir, GetFileExtension(response));

            using var fileStream = new FileStream(
                savePath, FileMode.Create, FileAccess.Write);

            await content.CopyToAsync(fileStream);
        }
        catch (Exception ex)
        {
            Log.Fatal("Failed to download file '{0}'.", url);
            LogHelpers.LogMessage(ex, LogKind.Fatal);
            throw;
        }

        return savePath;

        static string? GetFileExtension(HttpResponseMessage response)
        {
            return Path.GetExtension(response.RequestMessage?.RequestUri?.AbsoluteUri);
        }
    }
}
