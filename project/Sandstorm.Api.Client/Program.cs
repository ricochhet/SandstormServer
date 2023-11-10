using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Sandstorm.Core.Configuration;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sandstorm.Api.Providers;
using Sandstorm.Core.Configuration.Helpers;
using Sandstorm.Core.Configuration.Models;
using Sandstorm.Api.Configuration.Models;
using Sandstorm.Api.Configuration.Helpers;

namespace Sandstorm;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.Title = "SandstormApi";
        ILogger nativeLogger = new NativeLogger();
        ILogger fileStreamLogger = new FileStreamLogger();
        LogBase.Add(nativeLogger);
        LogBase.Add(fileStreamLogger);
        LogBase.Info("Insurgency: Sandstorm Service Emulator");

        ConfigurationHelper.CheckFirstRun();
        ConfigurationModel configurationModel = ConfigurationHelper.Read();
        if (configurationModel == null)
        {
            LogBase.Error("Could not read configuration file.");
            PauseAndWarn();
            return;
        }

        ApiSubcsriptionConfigHelper.CheckFirstRun();
        ApiSubscriptionConfigModel apiSubscriptionConfigModel = ApiSubcsriptionConfigHelper.Read();
        if (apiSubscriptionConfigModel == null)
        {
            LogBase.Error("Could not read api subscription configuration file.");
            PauseAndWarn();
            return;
        }

        if (args.Length < 1)
        {
            LogBase.Error("Usage: SandstormApi <add/build> <args>");
            LogBase.Error("Too few arguments.");
            return;
        }

        string input = args[0];
        string[] inputArgs = args.Skip(1).ToArray();
        if (input == "add")
        {
            await Add(inputArgs, configurationModel);
        }

        if (input == "build")
        {
            Build(inputArgs, configurationModel, apiSubscriptionConfigModel);
        }
    }

    private static async Task Add(
        string[] args,
        ConfigurationModel configurationModel
    )
    {
        if (args.Length < 3)
        {
            LogBase.Error("Usage: SandstormApi add <gameId> <modId> <apiKey>");
            LogBase.Error("Too few arguments.");
            return;
        }

        int gameId = int.Parse(args[0]);
        int modId = int.Parse(args[1]);
        string apiKey = args[2];
        string res = await HttpProvider.Get(
            $"{configurationModel.ModIOApiUrlBase}/v1/games/{gameId}/mods/{modId}?api_key={apiKey}"
        );

        if (res != string.Empty)
        {
            FsProvider.WriteFile(
                $"{configurationModel.SandstormDataPath}/{gameId}/Mods",
                $"{modId}.json",
                res
            );
            LogBase.Info($"Writing: {gameId} {modId}");
        }
    }

    private static void Build(
        string[] args,
        ConfigurationModel configurationModel,
        ApiSubscriptionConfigModel apiSubscriptionConfigModel
    )
    {
        if (args.Length < 1)
        {
            LogBase.Error("Usage: SandstormApi build <gameId>");
            LogBase.Error("Too few arguments.");
            return;
        }

        int gameId = int.Parse(args[0]);
        string sandstormDataPath =
            $"{configurationModel.SandstormDataPath}/{gameId}/";
        List<string> modioDataFiles = FsProvider.GetFiles(
            sandstormDataPath + "Mods/",
            "*.json"
        );
        List<object> modioDataObjects = new();
        foreach (string jsonFile in modioDataFiles)
        {
            if (!apiSubscriptionConfigModel.DoNotAddToSubscription.Contains(Path.GetFileNameWithoutExtension(jsonFile)))
            {
                string jsonString = FsProvider.ReadAllText(jsonFile);
                object modioDataObject = JsonSerializer.Deserialize<object>(
                    jsonString
                );
                modioDataObjects.Add(modioDataObject);
            }
        }

        JsonSerializerOptions options = new() { WriteIndented = true };
        ModIOAuthObject modIOAuthObject =
            new()
            {
                Data = modioDataObjects.ToArray(),
                ResultCount = modioDataObjects.Count,
                ResultOffset = 0,
                ResultLimit = 100,
                ResultTotal = modioDataObjects.Count
            };

        string outputJson = JsonSerializer.Serialize(modIOAuthObject, options);
        FsProvider.WriteFile(
            sandstormDataPath,
            "Subscription.json",
            outputJson
        );
    }

    private static void PauseAndWarn()
    {
        LogBase.Warn("Press \"F\" to safely exit.");
        while (Console.ReadKey(intercept: true).Key != ConsoleKey.F) { }
        return;
    }
}
