using System;
using System.IO;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;
using Sandstorm.Core.Configuration.Helpers;
using Sandstorm.Core.Configuration.Models;
using Sandstorm.Core.Helpers;
using Sandstorm.Proxy.Helpers;
using Sandstorm.Proxy.Providers;
using System.Threading.Tasks;

namespace Sandstorm;

internal class Program
{
    private static ProxyProvider proxy;

    private static async Task Main(string[] args)
    {
        Console.Title = "SandstormProxy";
        LogBase.Add(new NativeLogger());
        LogBase.Add(new FileStreamLogger());
        LogBase.Info("Insurgency: Sandstorm Service Emulator");

        ConfigurationHelper.CheckFirstRun();
        ConfigurationModel configuration = ConfigurationHelper.Read();
        if (configuration == null)
        {
            LogBase.Error("SandstormServer.json could not be read, make sure it is located in \"SandstormServer_Data\" and try again.");
            CommandLineHelper.Pause();
            return;
        }

        if (args.Length != 0)
        {
            int manualGameId = Array.IndexOf(args, "--gameid");
            if (manualGameId != -1)
                configuration.ModioGameId = int.Parse(args[manualGameId + 1]);

            int manualSubscribe = Array.IndexOf(args, "--subscribe");
            if (manualSubscribe != -1)
                await ModioRequestHelper.AddAsync(configuration, int.Parse(args[manualSubscribe + 1]));

            int manualBuild = Array.IndexOf(args, "--build");
            if (manualBuild != -1)
                ModioRequestHelper.BuildModSubscription(configuration);
        }

        if (configuration.ModioGameId == -1)
        {
            LogBase.Warn("The game id has not been set in \"SandstormServer.json\".");
            CommandLineHelper.Pause();
            return;
        }

        if (configuration.AddToSubscription.Count == 0)
        {
            LogBase.Warn("The mod subscriptions have not been set in \"SandstormServer.json\".");
            CommandLineHelper.Pause();
            return;
        }

        if (configuration.ModioApiKey == "PLACE_API_KEY_HERE" || string.IsNullOrEmpty(configuration.ModioApiKey))
        {
            LogBase.Warn("The mod.io API key has not been set in \"SandstormServer.json\".");
            CommandLineHelper.Pause();
            return;
        }

        try
        {
            await ModioRequestHelper.SubscribeAsync(configuration);
            string subscriptionFilePath = Path.Combine(ConfigurationHelper.ConfigFilePath, configuration.ModioGameId.ToString(), "Subscription.json");
            if (!FsProvider.Exists(subscriptionFilePath))
            {
                LogBase.Error($"Could not find the mod subscription data at: {subscriptionFilePath}");
                CommandLineHelper.Pause();
                return;
            }

            string modioModObject;
            try
            {
                string fileData = FsProvider.ReadAllText(subscriptionFilePath);
                if (string.IsNullOrEmpty(fileData))
                {
                    LogBase.Error("The mod subscription data is either null or empty.");
                    CommandLineHelper.Pause();
                    return;
                }
                modioModObject = fileData;
            }
            catch (IOException e)
            {
                LogBase.Error($"An error occurred while reading the mod subscription data: {e.Message}");
                modioModObject = string.Empty;
            }

            if (string.IsNullOrEmpty(modioModObject))
                return;

            proxy = new ProxyProvider(configuration.ModioGameId, modioModObject, WindowsAdminHelper.IsAdmin());
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
            CommandLineHelper.Pause();
            return;
        }

        proxy.StartProxy();
        LogBase.Warn("DO NOT MANUALLY CLOSE THIS WINDOW! If you do and your internet breaks clear your proxy settings and restart your computer.");
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
