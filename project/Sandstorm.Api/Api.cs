using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sandstorm.Api;

public class Api
{
    public static async Task<string> MakeGetRequest(string apiUrl)
    {
        using HttpClient client = new();
        try
        {
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            Console.WriteLine(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                Console.WriteLine($"Failed to retrieve data. Status Code: {response.StatusCode}");
                return string.Empty;
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            return string.Empty;
        }
    }
}