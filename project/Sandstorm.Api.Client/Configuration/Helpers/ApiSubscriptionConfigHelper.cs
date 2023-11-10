using System.Text.Json;
using Sandstorm.Api.Configuration.Models;
using Sandstorm.Core.Configuration.Models;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;

namespace Sandstorm.Api.Configuration.Helpers;

public static class ApiSubscriptionConfigHelper
{
    private static string ConfigurationFileName
    {
        get { return "SandstormApiSubscriptionCfg.json"; }
    }

    public static string ConfigurationPath
    {
        get { return $"./{ConfigurationFileName}"; }
    }

    public static void CheckFirstRun()
    {
        if (!FsProvider.Exists(ConfigurationPath))
        {
            LogBase.Info(
                "Could not find api subscription configuration file. Creating..."
            );
            JsonSerializerOptions options = new() { WriteIndented = true };
            ApiSubscriptionConfigModel configurationModel =
                new() { DoNotAddToSubscription = { "" } };
            string outputJson = JsonSerializer.Serialize(
                configurationModel,
                options
            );
            FsProvider.WriteFile("./", ConfigurationFileName, outputJson);
        }
    }

    public static ApiSubscriptionConfigModel Read()
    {
        if (FsProvider.Exists(ConfigurationPath))
        {
            string fileData = FsProvider.ReadAllText(ConfigurationPath);
            ApiSubscriptionConfigModel inputJson =
                JsonSerializer.Deserialize<ApiSubscriptionConfigModel>(
                    fileData
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
