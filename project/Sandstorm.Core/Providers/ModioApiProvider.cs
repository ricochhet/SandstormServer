using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Models;
using Sandstorm.Core.Providers;

namespace Sandstorm.Core.Helpers;

public static class ModioApiProvider
{
    public static async Task Subscribe(SettingsModel settings)
    {
        if (IsSettingsInvalid(settings))
        {
            LogBase.Warn("SandstormServer is not configured to subscribe to new mods automatically.");
            return;
        }

        int newModsCount = 0;
        string folderPath = Path.Combine(SettingsProvider.SandstormDataPath, settings.GameId.ToString(), "Mods");
        List<string> existingModFiles = FsProvider.GetFiles(folderPath, "*.json", true);
        List<string> modIdFiles = settings.AddToSubscription.Where(p => !existingModFiles.Any(l => l == p)).ToList();
        if (!string.IsNullOrEmpty(settings.ApiKey) && modIdFiles.Count != 0)
        {
            foreach (string modIdFile in modIdFiles)
            {
                int modId = int.Parse(modIdFile);
                string response = await Get(settings, modId);
                if (!string.IsNullOrEmpty(response))
                {
                    WriteFile(settings, modId, response);
                    newModsCount += 1;
                }
            }
        }

        if (newModsCount != 0)
        {
            LogBase.Info($"Adding {newModsCount} new mods to Subscription.json");
            Write(settings);
        }
    }

    public static async Task Add(SettingsModel settings, int modId)
    {
        string response = await Get(settings, modId);
        if (!string.IsNullOrEmpty(response))
        {
            WriteFile(settings, modId, response);
        }
    }

    private static async Task<string> Get(SettingsModel settings, int modId)
    {
        string url = $"{settings.ApiUrlBase}/v1/games/{settings.GameId}/mods/{modId}?api_key={settings.ApiKey}";
        return await ApiFetchHelper.Get(url);
    }

    private static void WriteFile(SettingsModel settings, int modId, string response)
    {
        string folderPath = Path.Combine(SettingsProvider.SandstormDataPath, settings.GameId.ToString(), "Mods");
        JsonSerializerOptions options = new() { WriteIndented = true };
        JsonHelper.Write(folderPath, $"{modId}.json", response, options);
        LogBase.Info($"Saved mod data to: {folderPath}/{modId}.json");
    }

    private static bool IsSettingsInvalid(SettingsModel settings)
    {
        return settings.AddToSubscription.Count == 0
            || settings.GameId == -1
            || string.IsNullOrEmpty(SettingsProvider.SandstormDataPath)
            || string.IsNullOrEmpty(settings.ApiKey);
    }

    public static void Write(SettingsModel settings)
    {
        string sandstormDataPath = Path.Combine(SettingsProvider.SandstormDataPath, settings.GameId.ToString());
        List<string> dataFiles = FsProvider.GetFiles(Path.Combine(sandstormDataPath, "Mods"), "*.json");
        List<object> modObjects = new();
        foreach (string data in dataFiles)
        {
            if (!settings.DoNotAddToSubscription.Contains(Path.GetFileNameWithoutExtension(data)))
            {
                modObjects.Add(JsonHelper.Read<object>(data));
            }
        }

        ModObjectModel modObject =
            new()
            {
                Data = modObjects.ToArray(),
                ResultCount = modObjects.Count,
                ResultOffset = 0,
                ResultLimit = 100,
                ResultTotal = modObjects.Count
            };

        JsonSerializerOptions options = new() { WriteIndented = true };
        JsonHelper.Write(sandstormDataPath, SettingsProvider.ModObjectFileName, modObject, options);
    }
}
