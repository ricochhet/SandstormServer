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
using Sandstorm.Core.Configuration.Helpers;
using Sandstorm.Core.Configuration.Models;

namespace Sandstorm;

internal class Program
{
	private static Proxy.Proxy proxy;

	private static void Main(string[] args)
	{
		Console.Title = "SandstormProxy";
		ILogger logger = new Logger();
		LogBase.Add(logger);
		LogBase.Info("Insurgency: Sandstorm Service Emulator");
		ConfigurationHelper.CheckFirstRun();
		ConfigurationModel configurationModel = ConfigurationHelper.Read();
		if (configurationModel == null)
		{
			LogBase.Error("Could not read configuration file.");
			return;
		}

        string modioAuthObject;
        if (FsProvider.Exists(configurationModel.SubscriptionObjectPath))
		{
			try
			{
				modioAuthObject = FsProvider.ReadAllText(configurationModel.SubscriptionObjectPath);
			} 
			catch (IOException e)
			{
				LogBase.Error($"An error occurred while reading the auth object: {e.Message}");
				modioAuthObject = string.Empty;
			}
		}
		else
		{
			LogBase.Error($"Could not find auth object at path: {configurationModel.SubscriptionObjectPath}");
			return;
		}

		try
		{
			proxy = new Proxy.Proxy(modioAuthObject);
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
		LogBase.Warn("WARNING: DO NOT MANUALLY CLOSE THIS WINDOW! If you do and your internet breaks clear your proxy settings and restart your computer.");
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
