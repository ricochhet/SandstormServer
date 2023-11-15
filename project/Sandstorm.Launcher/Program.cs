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

namespace Sandstorm.Launcher;

internal class Program
{
    private static ProxyProvider proxy;
    private static string ProcessFileName = null;

    private static async Task Main(string[] args)
    {
        Console.Title = "SandstormProxy";
        LogBase.Add(new NativeLogger());
        LogBase.Add(new FileStreamLogger());
        LogBase.Info("Insurgency: Sandstorm Service Emulator");

        ConfigurationHelper.Write();
        ConfigurationModel configuration = ConfigurationHelper.Read();
        if (configuration == null)
        {
            LogBase.Error($"{ConfigurationHelper.ConfigFileName} could not be read, make sure it is located in \"SandstormServer_Data\" and try again.");
            CommandLineHelper.Pause();
            return;
        }

        if (args.Length != 0)
        {
            CommandLineHelper.ProcessArgument(args, "--gameid", (int value) => configuration.ModioGameId = value);
            CommandLineHelper.ProcessArgument(args, "--subscribe", async (int value) => await ModioRequestHelper.AddAsync(configuration, value));
            CommandLineHelper.ProcessArgument(args, "--build", () => ModioRequestHelper.BuildModSubscription(configuration));
            CommandLineHelper.ProcessArgument(args, "--launch", (string value) => ProcessFileName = value);
        }

        if (configuration.ModioGameId == -1)
        {
            LogBase.Warn($"The game id has not been set in \"{ConfigurationHelper.ConfigFileName}\".");
            CommandLineHelper.Pause();
            return;
        }

        if (configuration.AddToSubscription.Count == 0)
        {
            LogBase.Warn($"The mod subscriptions have not been set in \"{ConfigurationHelper.ConfigFileName}\".");
            CommandLineHelper.Pause();
            return;
        }

        if (configuration.ModioApiKey == ConfigurationHelper.ModioApiKeyDefault || string.IsNullOrEmpty(configuration.ModioApiKey))
        {
            LogBase.Warn($"The mod.io API key has not been set in \"{ConfigurationHelper.ConfigFileName}\".");
            CommandLineHelper.Pause();
            return;
        }

        try
        {
            await ModioRequestHelper.SubscribeAsync(configuration);
            string subscriptionFilePath = Path.Combine(ConfigurationHelper.SandstormDataPath, configuration.ModioGameId.ToString(), "Subscription.json");
            if (!FsProvider.Exists(subscriptionFilePath))
            {
                LogBase.Error($"Could not find the mod subscription data at: {subscriptionFilePath}");
                CommandLineHelper.Pause();
                return;
            }

            string responseObject;
            try
            {
                string fileData = FsProvider.ReadAllText(subscriptionFilePath);
                if (string.IsNullOrEmpty(fileData))
                {
                    LogBase.Error("The mod subscription data is either null or empty.");
                    CommandLineHelper.Pause();
                    return;
                }
                responseObject = fileData;
            }
            catch (IOException e)
            {
                LogBase.Error($"An error occurred while reading the mod subscription data: {e.Message}");
                responseObject = string.Empty;
            }

            if (string.IsNullOrEmpty(responseObject))
                return;

            proxy = new ProxyProvider(configuration.ModioGameId, responseObject, WindowsAdminHelper.IsAdmin());
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

        if (!string.IsNullOrEmpty(ProcessFileName))
        {
            LogBase.Info($"Attempting to start process: {ProcessFileName}");
            ProcessHelper.RunProcess(ProcessFileName);
        }

        if (Console.ReadKey(intercept: true).Key == ConsoleKey.F)
        {
            LogBase.Info("Exiting...");
            proxy.Stop();
        }
    }
}
