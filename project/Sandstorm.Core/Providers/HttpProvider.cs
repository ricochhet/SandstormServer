using System;
using System.Net.Http;
using System.Threading.Tasks;
using Sandstorm.Core.Logger;

namespace Sandstorm.Core.Providers;

public class HttpProvider
{
    public static async Task<string> Get(string apiUrl)
    {
        using HttpClient client = new();
        try
        {
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            LogBase.Info($"Fetching response from {apiUrl}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                LogBase.Info(
                    $"Failed to retrieve data. Status Code: {response.StatusCode}"
                );
                return string.Empty;
            }
        }
        catch (HttpRequestException ex)
        {
            LogBase.Info($"Exception: {ex.Message}");
            return string.Empty;
        }
    }
}
