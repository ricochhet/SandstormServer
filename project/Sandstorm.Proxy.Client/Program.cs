using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Sandstorm.Core.Configuration;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;
using Sandstorm.Proxy;
using System.Threading.Tasks;

namespace Sandstorm;

internal class Program
{
	private static Proxy.Proxy proxy;

	private static async Task Main(string[] args)
	{
		Console.Title = "SandstormServer";
		ILogger logger = new NativeLogger();
		LogBase.Add(logger);
		LogBase.Info("Insurgency: Sandstorm Service Emulator");

		if (args.Length < 1)
		{
			LogBase.Error("Too few arguments.");
		}

		string input = args[0];
		string[] inputArgs = args.Skip(1).ToArray();
		if (input == "start")
		{
			Proxy();
		}

		if (input == "getmod")
		{
			await GetMod(inputArgs);
		}
	}

	private static async Task GetMod(string[] args)
	{
		if (args.Length < 2) 
		{ 
			LogBase.Error("Too few arguments."); 
		}

		int gameId = int.Parse(args[0]);
		int modId = int.Parse(args[1]);
		string res = await Api.Api.MakeGetRequest($"{Constants.ModIOApiUrlBase}/games/254/mods/{150867}");

		if (res != string.Empty)
		{
			FsProvider.WriteFile($"./SandstormServerData/{gameId}", $"Mod_{modId}.json", res);
			LogBase.Info($"Writing Data:\nGameId: {gameId}\nModId: {modId}");
		}
	}

	private static void Proxy()
	{
		try
		{
			proxy = new Proxy.Proxy();
		}
		catch (Exception ex)
		{
			LogBase.Error("Error while initializing proxy.");
			LogBase.Error("==============================");
			LogBase.Error(ex.Message);
			LogBase.Error(ex.StackTrace);
			LogBase.Error("==============================");
			LogBase.Error(ex.InnerException.Message);
			LogBase.Error(ex.InnerException.StackTrace);
			while (Console.ReadKey(intercept: true).Key != ConsoleKey.F)
			{
			}
			return;
		}

		proxy.StartProxy();
		LogBase.Info("WARNING: DO NOT MANUALLY CLOSE THIS WINDOW! If you do and your internet breaks clear your proxy settings and restart your computer.\n");
		LogBase.Info("==============================");
		LogBase.Info("Intercepting connections... Now run Insurgency: Sandstorm!");
		LogBase.Info("Press \"F\" to safely close the server.");
		LogBase.Info("==============================");
		
		if (Console.ReadKey(intercept: true).Key == ConsoleKey.F)
		{
			LogBase.Info("Exiting...");
			proxy.Stop();
		}
	}
}
