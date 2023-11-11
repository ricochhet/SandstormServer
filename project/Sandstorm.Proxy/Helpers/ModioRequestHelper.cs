using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandstorm.Core.Configuration.Models;
using Sandstorm.Core.Helpers;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;
using Sandstorm.Proxy.Providers;

namespace Sandstorm.Proxy.Helpers;

public static class ModioRequestHelper
{
    public static async Task Get(ConfigurationModel configuration)
    {
        List<string> existingModioMods = new();
        if (
            configuration.AddToSubscription.Count == 0
            || configuration.ModioGameId == -1
            || configuration.SandstormDataPath == string.Empty
            || configuration.SandstormDataPath == null
            || configuration.ModioApiKey == string.Empty
            || configuration.ModioApiKey == null
        )
            return;

        existingModioMods = FsProvider.GetFiles(
            $"{configuration.SandstormDataPath}/{configuration.ModioGameId}/Mods/",
            "*.json",
            true
        );

        if (
            configuration.ModioGameId != -1
            && configuration.ModioApiKey != string.Empty
            && configuration.ModioApiKey != null
        )
        {
            List<string> modioModIds = configuration.AddToSubscription
                .Where(p => !existingModioMods.Any(l => l == p))
                .ToList();
            foreach (string modioModId in modioModIds)
            {
                int modioModIdAsInt = int.Parse(modioModId);
                string res = await HttpProvider.Get(
                    $"{configuration.ModioApiUrlBase}/v1/games/{configuration.ModioGameId}/mods/{modioModIdAsInt}?api_key={configuration.ModioApiKey}"
                );

                if (res != string.Empty)
                {
                    FsProvider.WriteFile(
                        $"{configuration.SandstormDataPath}/{configuration.ModioGameId}/Mods",
                        $"{modioModIdAsInt}.json",
                        res
                    );
                    LogBase.Info(
                        $"Writing: {configuration.ModioGameId} {modioModIdAsInt}"
                    );
                }
            }
        }
    }

    public static void Build(ConfigurationModel configuration)
    {
        string sandstormDataPath = $"{configuration.SandstormDataPath}/{configuration.ModioGameId}/";
        List<string> modioDataFiles = FsProvider.GetFiles(
            sandstormDataPath + "Mods/",
            "*.json"
        );
        List<object> modioModObjects = new();
        foreach (string data in modioDataFiles)
        {
            if (
                !configuration.DoNotAddToSubscription.Contains(
                    Path.GetFileNameWithoutExtension(data)
                )
            )
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
        JsonHelper.Write(
            sandstormDataPath,
            "Subscription.json",
            modioModObject,
            options
        );
    }
}
