using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Models;
using Sandstorm.Core.MiniCommon;

namespace Sandstorm.Core.Helpers;

public static class SettingsProvider
{
    public const string SandstormDataPath = "./SandstormServer_Data";
    public const string SettingsFileName = "SandstormServer.json";
    public const string ModObjectFileName = "Subscription.json";
    public const string ApiKeyDefault = "PLACE_API_KEY_HERE";
    public static string LogFilePath
    {
        get { return Path.Combine(SandstormDataPath, LogFileName); }
    }

    private const string LogFileName = "SandstormServer.log";
    private static readonly string ApiUrlBaseDefault = "https://api.mod.io";
    private static readonly string SettingsFilePath = Path.Combine(SandstormDataPath, SettingsFileName);

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
            Json.Write(SandstormDataPath, SettingsFileName, settings, options);
        }
    }

    public static SettingsModel Read()
    {
        if (FsProvider.Exists(SettingsFilePath))
        {
            SettingsModel inputJson = Json.Read<SettingsModel>(SettingsFilePath);
            if (inputJson != null)
            {
                return inputJson;
            }

            return null;
        }

        return null;
    }
}
