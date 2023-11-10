using System.Text.Json;
using Sandstorm.Core.Configuration.Models;
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
            JsonSerializerOptions options = new() { WriteIndented = true };
            ConfigurationModel configurationModel =
                new()
                {
                    SubscriptionObjectPath = "./Subscription.json",
                    SandstormDataPath = "./SandstormServerData",
                    ModIOApiUrlBase = "https://api.mod.io",
                    LoggerOutputStreamPath = "./SandstormServer.log",
                };
            string outputJson = JsonSerializer.Serialize(
                configurationModel,
                options
            );
            FsProvider.WriteFile("./", ConfigurationFileName, outputJson);
        }
    }

    public static ConfigurationModel Read()
    {
        if (FsProvider.Exists(ConfigurationPath))
        {
            string fileData = FsProvider.ReadAllText(ConfigurationPath);
            ConfigurationModel inputJson =
                JsonSerializer.Deserialize<ConfigurationModel>(fileData);

            if (inputJson != null)
            {
                return inputJson;
            }

            return null;
        }

        return null;
    }
}
