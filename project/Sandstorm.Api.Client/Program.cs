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
		Console.Title = "SandstormApiClient";
		ILogger logger = new NativeLogger();
		LogBase.Add(logger);
		LogBase.Info("Insurgency: Sandstorm Service Emulator");

		if (args.Length < 1)
		{
			LogBase.Error("Too few arguments.");
		}

		string input = args[0];
		string[] inputArgs = args.Skip(1).ToArray();
		if (input == "getmod")
		{
			await GetMod(inputArgs);
		}

		if (input == "build")
		{
			CreateAuthModel(inputArgs);
		}
	}

	private static async Task GetMod(string[] args)
	{
		if (args.Length < 3)
		{ 
			LogBase.Error("Usage: SandstormApiClient getmod <gameId> <modId> <apiKey>");
			LogBase.Error("Too few arguments.");
		}

		int gameId = int.Parse(args[0]);
		int modId = int.Parse(args[1]);
		string apiKey = args[2];
		string res = await HttpProvider.Get($"{Constants.ModIOApiUrlBase}/v1/games/{gameId}/mods/{modId}?api_key={apiKey}");

		if (res != string.Empty)
		{
			FsProvider.WriteFile($"{Constants.SandstormDataPath}/{gameId}/Mods", $"Mod_{modId}.json", res);
			LogBase.Info($"Writing Data:\nGameId: {gameId}\nModId: {modId}");
		}
	}

	private static void CreateAuthModel(string[] args)
	{
		if (args.Length < 1)
		{
			LogBase.Error("Usage: SandstormApiClient build <gameId>");
			LogBase.Error("Too few arguments.");
		}

		int gameId = int.Parse(args[0]);
		string modioDataPath = $"{Constants.SandstormDataPath}/{gameId}/";
		List<string> modioData = FsProvider.GetFiles(modioDataPath + "Mods/", "*.json");
		List<object> modioDataObjects = new List<object>();
		foreach (string jsonFile in modioData)
		{
			string jsonString = File.ReadAllText(jsonFile);
			object modioDataObject = JsonSerializer.Deserialize<object>(jsonString);
			modioDataObjects.Add(modioDataObject);
		}

		JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };
		ModIOAuthObject modIOAuthObject = new()
		{
			Data = modioDataObjects.ToArray(),
			ResultCount = modioDataObjects.Count,
			ResultOffset = 0,
			ResultLimit = 100,
			ResultTotal = modioDataObjects.Count
		};

		string outputJson = JsonSerializer.Serialize(modIOAuthObject, options);
		FsProvider.WriteFile(modioDataPath, "Subscription.json", outputJson);
	}
}
