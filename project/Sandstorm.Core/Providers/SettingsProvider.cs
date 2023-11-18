using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Sandstorm.Core.Helpers;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Models;

namespace Sandstorm.Core.Providers;

public static class SettingsProvider
{
    public static string SandstormDataPath
    {
        get { return "./SandstormServer_Data"; }
    }

    public static string SettingsFileName
    {
        get { return "SandstormServer.json"; }
    }

    public static string SettingsFilePath
    {
        get { return Path.Combine(SandstormDataPath, SettingsFileName); }
    }

    private static string LogFileName
    {
        get { return "SandstormServer.log"; }
    }

    public static string LogFilePath
    {
        get { return Path.Combine(SandstormDataPath, LogFileName); }
    }

    public static string ApiKeyDefault
    {
        get { return "PLACE_API_KEY_HERE"; }
    }

    private static string ApiUrlBaseDefault
    {
        get { return "https://api.mod.io"; }
    }

    public static string ModObjectFileName
    {
        get { return "Subscription.json"; }
    }

    public static void Write()
    {
        if (!FsProvider.Exists(SettingsFilePath))
        {
            LogBase.Info("Setup: Creating settings file...");
            SettingsModel settings =
                new()
                {
                    GameId = -1,
                    ApiKey = ApiKeyDefault,
                    ApiUrlBase = ApiUrlBaseDefault,
                    AddToSubscription = new List<string>(),
                    DoNotAddToSubscription = new List<string>()
                };

            JsonSerializerOptions options = new() { WriteIndented = true };
            JsonHelper.Write(SandstormDataPath, SettingsFileName, settings, options);
        }
    }

    public static SettingsModel Read()
    {
        if (FsProvider.Exists(SettingsFilePath))
        {
            SettingsModel inputJson = JsonHelper.Read<SettingsModel>(SettingsFilePath);
            if (inputJson != null)
            {
                return inputJson;
            }

            return null;
        }

        return null;
    }
}
