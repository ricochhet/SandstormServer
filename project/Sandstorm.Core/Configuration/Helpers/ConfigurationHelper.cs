using System.Collections.Generic;
using System.Text.Json;
using Sandstorm.Core.Configuration.Models;
using Sandstorm.Core.Helpers;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;

namespace Sandstorm.Core.Configuration.Helpers;

public static class ConfigurationHelper
{
    private static string ConfigurationFileName
    {
        get { return "SandstormServerCfg.json"; }
    }

    public static string ConfigurationPath
    {
        get { return $"./{ConfigurationFileName}"; }
    }

    public static void CheckFirstRun()
    {
        if (!FsProvider.Exists(ConfigurationPath))
        {
            LogBase.Info("Could not find configuration file. Creating...");
            ConfigurationModel configurationModel =
                new()
                {
                    ModioGameId = 0,
                    ModioApiKey = "PLACE_API_KEY_HERE",
                    SubscriptionObjectPath = "./Subscription.json",
                    SandstormDataPath = "./SandstormServerData",
                    ModioApiUrlBase = "https://api.mod.io",
                    LoggerOutputStreamPath = "./SandstormServer.log",
                    AddToSubscription = new List<string>(),
                    DoNotAddToSubscription = new List<string>()
                };

            JsonSerializerOptions options = new() { WriteIndented = true };
            JsonHelper.Write(
                "./",
                ConfigurationFileName,
                configurationModel,
                options
            );
        }
    }

    public static ConfigurationModel Read()
    {
        if (FsProvider.Exists(ConfigurationPath))
        {
            ConfigurationModel inputJson = JsonHelper.Read<ConfigurationModel>(
                ConfigurationPath
            );
            if (inputJson != null)
            {
                return inputJson;
            }

            return null;
        }

        return null;
    }
}
