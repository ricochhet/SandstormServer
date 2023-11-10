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
using Sandstorm.Core.Configuration.Helpers;
using Sandstorm.Core.Configuration.Models;

namespace Sandstorm;

public class ModIOAuthObject
{
    [JsonPropertyName("data")]
    public object[] Data { get; set; }

    [JsonPropertyName("result_count")]
    public int ResultCount { get; set; }

    [JsonPropertyName("result_offset")]
    public int ResultOffset { get; set; }

    [JsonPropertyName("result_limit")]
    public int ResultLimit { get; set; }

    [JsonPropertyName("result_total")]
    public int ResultTotal { get; set; }
}

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.Title = "SandstormApi";
        ILogger logger = new Logger();
        LogBase.Add(logger);
        LogBase.Info("Insurgency: Sandstorm Service Emulator");
        ConfigurationHelper.CheckFirstRun();
        ConfigurationModel configurationModel = ConfigurationHelper.Read();
        if (configurationModel == null)
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
            await Add(inputArgs, configurationModel);
        }

        if (input == "build")
        {
            Build(inputArgs, configurationModel);
        }
    }

    private static async Task Add(
        string[] args,
        ConfigurationModel configurationModel
    )
    {
        if (args.Length < 3)
        {
            LogBase.Error("Usage: SandstormApi get <gameId> <modId> <apiKey>");
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
                $"Mod_{modId}.json",
                res
            );
            LogBase.Info($"Writing: {gameId} {modId}");
        }
    }

    private static void Build(
        string[] args,
        ConfigurationModel configurationModel
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
            string jsonString = FsProvider.ReadAllText(jsonFile);
            object modioDataObject = JsonSerializer.Deserialize<object>(
                jsonString
            );
            modioDataObjects.Add(modioDataObject);
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
