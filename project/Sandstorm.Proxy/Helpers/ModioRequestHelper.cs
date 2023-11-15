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
        if (IsConfigurationInvalid(configuration))
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
                string response = await RequestAsync(configuration, modioModId);
                if (!string.IsNullOrEmpty(response))
                {
                    WriteFile(configuration, modioModId, response);
                    newModsCount += 1;
                }
            }
        }

        if (newModsCount != 0)
        {
            LogBase.Info($"Adding {newModsCount} new mods to Subscription.json");
            WriteSubscription(configuration);
        }
    }

    public static async Task AddAsync(ConfigurationModel configuration, int modId)
    {
        string response = await RequestAsync(configuration, modId);
        if (!string.IsNullOrEmpty(response))
        {
            WriteFile(configuration, modId, response);
        }
    }

    private static async Task<string> RequestAsync(ConfigurationModel configuration, int modId)
    {
        string url = $"{configuration.ModioApiUrlBase}/v1/games/{configuration.ModioGameId}/mods/{modId}?api_key={configuration.ModioApiKey}";
        return await HttpProvider.Get(url);
    }

    private static void WriteFile(ConfigurationModel configuration, int modId, string response)
    {
        string folderPath = Path.Combine(ConfigurationHelper.SandstormDataPath, configuration.ModioGameId.ToString(), "Mods");
        JsonSerializerOptions options = new() { WriteIndented = true };
        JsonHelper.Write(folderPath, $"{modId}.json", response, options);
        LogBase.Info($"Saved mod data to: {folderPath}/{modId}.json");
    }

    private static bool IsConfigurationInvalid(ConfigurationModel configuration)
    {
        return configuration.AddToSubscription.Count == 0
            || configuration.ModioGameId == -1
            || string.IsNullOrEmpty(ConfigurationHelper.SandstormDataPath)
            || string.IsNullOrEmpty(configuration.ModioApiKey);
    }

    public static void WriteSubscription(ConfigurationModel configuration)
    {
        string sandstormDataPath = Path.Combine(ConfigurationHelper.SandstormDataPath, configuration.ModioGameId.ToString());
        List<string> modioDataFiles = FsProvider.GetFiles(Path.Combine(sandstormDataPath, "Mods"), "*.json");
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
