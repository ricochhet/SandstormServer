using System;
using System.IO;
using System.Linq;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using Sandstorm.Api.Providers;
using Sandstorm.Core.Configuration.Helpers;
using Sandstorm.Core.Configuration.Models;

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
        ConfigurationModel configuration = ConfigurationHelper.Read();
        if (configuration == null)
        {
            LogBase.Error("Could not read configuration file.");
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
            await Add(inputArgs, configuration);
        }

        if (input == "build")
        {
            Build(inputArgs, configuration);
        }
    }

    private static async Task Add(string[] args, ConfigurationModel config)
    {
        if (args.Length < 3)
        {
            LogBase.Error("Usage: SandstormApi add <gameId> <modId>");
            LogBase.Error("Too few arguments.");
            return;
        }

        if (config.ModioApiKey == null || config.ModioApiKey == string.Empty)
        {
            LogBase.Error(
                $"SandstormApi requires a valid mod.io API key. Generate one here: https://mod.io/me/access, and place it in {ConfigurationHelper.ConfigurationPath}."
            );
            return;
        }

        int gameId = int.Parse(args[0]);
        int modId = int.Parse(args[1]);
        string res = await HttpProvider.Get(
            $"{config.ModioApiUrlBase}/v1/games/{gameId}/mods/{modId}?api_key={config.ModioApiKey}"
        );

        if (res != string.Empty)
        {
            FsProvider.WriteFile(
                $"{config.SandstormDataPath}/{gameId}/Mods",
                $"{modId}.json",
                res
            );
            LogBase.Info($"Writing: {gameId} {modId}");
        }
    }

    private static void Build(string[] args, ConfigurationModel config)
    {
        if (args.Length < 1)
        {
            LogBase.Error("Usage: SandstormApi build <gameId>");
            LogBase.Error("Too few arguments.");
            return;
        }

        int gameId = int.Parse(args[0]);
        string sandstormDataPath = $"{config.SandstormDataPath}/{gameId}/";
        List<string> modioDataFiles = FsProvider.GetFiles(
            sandstormDataPath + "Mods/",
            "*.json"
        );
        List<object> modioDataObjects = new();
        foreach (string data in modioDataFiles)
        {
            if (
                !config.DoNotAddToSubscription.Contains(
                    Path.GetFileNameWithoutExtension(data)
                )
            )
            {
                string jsonString = FsProvider.ReadAllText(data);
                object modioDataObject = JsonSerializer.Deserialize<object>(
                    jsonString
                );
                modioDataObjects.Add(modioDataObject);
            }
        }

        JsonSerializerOptions options = new() { WriteIndented = true };
        ModioModObjectModel modioModObject =
            new()
            {
                Data = modioDataObjects.ToArray(),
                ResultCount = modioDataObjects.Count,
                ResultOffset = 0,
                ResultLimit = 100,
                ResultTotal = modioDataObjects.Count
            };

        string outputJson = JsonSerializer.Serialize(modioModObject, options);
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
