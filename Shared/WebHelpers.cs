using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;
using Shared.Logger;

namespace Shared;

public static class WebHelpers
{
    public static bool TrySetUserAgent(HttpClient client, string? userAgent)
    {
        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

            return true;
        }

        return false;
    }

    public static async Task<bool> IsUrlValidAsync(
        string url, string? userAgent = null, IEnumerable<string>? mime = null)
    {
        bool valid = false;

        using var httpClient = new HttpClient();

        TrySetUserAgent(httpClient, userAgent);

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(
                new(HttpMethod.Head, url));

            if (response.IsSuccessStatusCode)
            {
                if (mime == null || !mime.Any() || IsMimeValid(response, mime))
                {
                    valid = true;
                }
            }
            else
            {
                Log.Information("URL '{0}' return respone code {1}.", url, response.StatusCode.ToString());
            }
        }
        catch (HttpRequestException ex)
        {
            Log.Information("Failed to load URL '{0}'.", url);
            LogHelpers.LogMessage(ex, LogKind.Information);
        }

        return valid;

        static bool IsMimeValid(HttpResponseMessage response, IEnumerable<string> mime)
        {
            string? mimeResponse = response?.Content?.Headers?.ContentType?.MediaType;

            return mime.Any(m =>
                string.Equals(m, mimeResponse, StringComparison.OrdinalIgnoreCase));
        }
    }
}
