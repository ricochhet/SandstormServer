using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandstorm.Core.Configuration.Helpers;
using Sandstorm.Core.Configuration.Models;
using Sandstorm.Core.Helpers;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;
using Sandstorm.Proxy.Models;

namespace Sandstorm.Proxy.Helpers;

public static class ModioRequestHelper
{
    public static async Task SubscribeAsync(ConfigurationModel configuration)
    {
        if (IsSubscriptionConfigurationInvalid(configuration))
        {
            LogBase.Warn("SandstormServer is not configured to subscribe to new mods automatically.");
            return;
        }

        int newModsCount = 0;
        string folderPath = Path.Combine(ConfigurationHelper.SandstormDataPath, configuration.ModioGameId.ToString(), "Mods");
        List<string> existingModioModFiles = FsProvider.GetFiles(folderPath, "*.json", true);
        List<string> modioModIdFiles = configuration.AddToSubscription.Where(p => !existingModioModFiles.Any(l => l == p)).ToList();
        if (!string.IsNullOrEmpty(configuration.ModioApiKey) && modioModIdFiles.Count != 0)
        {
            foreach (string modioModIdFile in modioModIdFiles)
            {
                int modioModId = int.Parse(modioModIdFile);
                string response = await GetModioApiResponseAsync(configuration, modioModId);
                if (!string.IsNullOrEmpty(response))
                {
                    WriteModToFile(configuration, modioModId, response);
                    newModsCount += 1;
                }
            }
        }

        if (newModsCount != 0)
        {
            LogBase.Info($"Adding {newModsCount} new mods to Subscription.json");
            BuildModSubscription(configuration);
        }
    }

    public static async Task AddAsync(ConfigurationModel configuration, int modId)
    {
        string response = await GetModioApiResponseAsync(configuration, modId);
        if (!string.IsNullOrEmpty(response))
        {
            WriteModToFile(configuration, modId, response);
        }
    }

    private static async Task<string> GetModioApiResponseAsync(ConfigurationModel configuration, int modId)
    {
        string url = $"{configuration.ModioApiUrlBase}/v1/games/{configuration.ModioGameId}/mods/{modId}?api_key={configuration.ModioApiKey}";
        return await HttpProvider.Get(url);
    }

    private static void WriteModToFile(ConfigurationModel configuration, int modId, string response)
    {
        string folderPath = Path.Combine(ConfigurationHelper.SandstormDataPath, configuration.ModioGameId.ToString(), "Mods");
        FsProvider.WriteFile(folderPath, $"{modId}.json", response);
        LogBase.Info($"Saving mod: {modId}");
    }

    private static bool IsSubscriptionConfigurationInvalid(ConfigurationModel configuration)
    {
        return configuration.AddToSubscription.Count == 0
            || configuration.ModioGameId == -1
            || string.IsNullOrEmpty(ConfigurationHelper.SandstormDataPath)
            || string.IsNullOrEmpty(configuration.ModioApiKey);
    }

    public static void BuildModSubscription(ConfigurationModel configuration)
    {
        string sandstormDataPath = Path.Combine(ConfigurationHelper.SandstormDataPath, configuration.ModioGameId.ToString());
        List<string> modioDataFiles = FsProvider.GetFiles(sandstormDataPath + "Mods/", "*.json");
        List<object> modioModObjects = new();
        foreach (string data in modioDataFiles)
        {
            if (!configuration.DoNotAddToSubscription.Contains(Path.GetFileNameWithoutExtension(data)))
            {
                modioModObjects.Add(JsonHelper.Read<object>(data));
            }
        }

        ModioModObjectModel modioModObject =
            new()
            {
                Data = modioModObjects.ToArray(),
                ResultCount = modioModObjects.Count,
                ResultOffset = 0,
                ResultLimit = 100,
                ResultTotal = modioModObjects.Count
            };

        JsonSerializerOptions options = new() { WriteIndented = true };
        JsonHelper.Write(sandstormDataPath, "Subscription.json", modioModObject, options);
    }
}
