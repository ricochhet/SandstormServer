using System.Net.Http;
using System.Threading.Tasks;
using Sandstorm.Core.Logger;

namespace Sandstorm.Core.Providers;

public class HttpProvider
{
    public static async Task<string> Get(string apiUrl, bool shouldInfoLog = false)
    {
        using HttpClient client = new();
        try
        {
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            if (shouldInfoLog)
                LogBase.Info($"Fetching response from {apiUrl}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            
            LogBase.Error($"Failed to retrieve data. Status Code: {response.StatusCode}");
            return string.Empty;
        }
        catch (HttpRequestException ex)
        {
            LogBase.Error($"Exception: {ex.Message}");
            return string.Empty;
        }
    }
}
