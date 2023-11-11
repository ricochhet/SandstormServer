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
            LogBase.Error("Could not read configuration file.");
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
            return;

        try
        {
            await ModioRequestHelper.SubscribeAsync(configuration);
            string modioModObject;
            if (FsProvider.Exists($"{configuration.SandstormDataPath}/{configuration.ModioGameId}/Subscription.json"))
            {
                try
                {
                    modioModObject = FsProvider.ReadAllText($"{configuration.SandstormDataPath}/{configuration.ModioGameId}/Subscription.json");
                }
                catch (IOException e)
                {
                    LogBase.Error($"An error occurred while reading the auth object: {e.Message}");
                    modioModObject = string.Empty;
                }
            }
            else
            {
                LogBase.Error($"Could not find auth object at path: {configuration.SandstormDataPath}/{configuration.ModioGameId}/Subscription.json");
                CommandLineHelper.Pause();
                return;
            }

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
