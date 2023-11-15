using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Sandstorm.Core.Configuration.Models;
using Sandstorm.Core.Helpers;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;

namespace Sandstorm.Core.Configuration.Helpers;

public static class ConfigurationHelper
{
    public static string SandstormDataPath
    {
        get { return "./SandstormServer_Data"; }
    }

    public static string ConfigFileName
    {
        get { return "SandstormServer.json"; }
    }

    public static string ConfigFilePath
    {
        get { return Path.Combine(SandstormDataPath, ConfigFileName); }
    }

    private static string LogFileName
    {
        get { return "SandstormServer.log"; }
    }

    public static string LogFilePath
    {
        get { return Path.Combine(SandstormDataPath, LogFileName); }
    }

    public static string ModioApiKeyDefault
    {
        get { return "PLACE_API_KEY_HERE"; }
    }

    private static string ModioApiUrlBaseDefault
    {
        get { return "https://api.mod.io"; }
    }

    public static string ModioModObjectFileName
    {
        get { return "Subscription.json"; }
    }

    public static void Write()
    {
        if (!FsProvider.Exists(ConfigFilePath))
        {
            LogBase.Info("Setup: Creating configuration file...");
            ConfigurationModel configurationModel =
                new()
                {
                    ModioGameId = -1,
                    ModioApiKey = ModioApiKeyDefault,
                    ModioApiUrlBase = ModioApiUrlBaseDefault,
                    AddToSubscription = new List<string>(),
                    DoNotAddToSubscription = new List<string>()
                };

            JsonSerializerOptions options = new() { WriteIndented = true };
            JsonHelper.Write(SandstormDataPath, ConfigFileName, configurationModel, options);
        }
    }

    public static ConfigurationModel Read()
    {
        if (FsProvider.Exists(ConfigFilePath))
        {
            ConfigurationModel inputJson = JsonHelper.Read<ConfigurationModel>(ConfigFilePath);
            if (inputJson != null)
            {
                return inputJson;
            }

            return null;
        }

        return null;
    }
}
