using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandstorm.Core.Configuration.Models;
using Sandstorm.Core.Helpers;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;

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

        int newModsAdded = 0;
        List<string> existingModioMods = FsProvider.GetFiles($"{configuration.SandstormDataPath}/{configuration.ModioGameId}/Mods/", "*.json", true);
        List<string> modioModIds = configuration.AddToSubscription.Where(p => !existingModioMods.Any(l => l == p)).ToList();
        if (!string.IsNullOrEmpty(configuration.ModioApiKey) && modioModIds.Count != 0)
        {
            foreach (string modioModId in modioModIds)
            {
                int modioModIdAsInt = int.Parse(modioModId);
                string response = await GetModioApiResponseAsync(configuration, modioModIdAsInt);
                if (!string.IsNullOrEmpty(response))
                {
                    WriteModToFile(configuration, modioModIdAsInt, response);
                    newModsAdded += 1;
                }
            }
        }

        if (newModsAdded != 0)
        {
            LogBase.Info($"Adding {newModsAdded} new mods to Subscription.json");
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
        FsProvider.WriteFile($"{configuration.SandstormDataPath}/{configuration.ModioGameId}/Mods", $"{modId}.json", response);
        LogBase.Info($"Saving mod: {modId}");
    }

    private static bool IsSubscriptionConfigurationInvalid(ConfigurationModel configuration)
    {
        return configuration.AddToSubscription.Count == 0
            || configuration.ModioGameId == -1
            || string.IsNullOrEmpty(configuration.SandstormDataPath)
            || string.IsNullOrEmpty(configuration.ModioApiKey);
    }

    public static void BuildModSubscription(ConfigurationModel configuration)
    {
        string sandstormDataPath = $"{configuration.SandstormDataPath}/{configuration.ModioGameId}/";
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
