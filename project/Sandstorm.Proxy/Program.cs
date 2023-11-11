using System;
using System.IO;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;
using Sandstorm.Core.Configuration.Helpers;
using Sandstorm.Core.Configuration.Models;
using Sandstorm.Core.Helpers;
using Sandstorm.Proxy.Helpers;
using Sandstorm.Proxy.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sandstorm;

internal class Program
{
    private static ProxyProvider proxy;

    private static async Task Main()
    {
        Console.Title = "SandstormProxy";
        ILogger logger = new NativeLogger();
        LogBase.Add(logger);
        LogBase.Info("Insurgency: Sandstorm Service Emulator");

        ConfigurationHelper.CheckFirstRun();
        ConfigurationModel configuration = ConfigurationHelper.Read();
        if (configuration == null)
        {
            LogBase.Error("Could not read configuration file.");
            CommandLineHelper.Pause();
            return;
        }

        await ModioRequestHelper.Get(configuration);
        ModioRequestHelper.Build(configuration);

        try
        {
            string modioModObject;
            if (FsProvider.Exists(configuration.SubscriptionObjectPath))
            {
                try
                {
                    modioModObject = FsProvider.ReadAllText(configuration.SubscriptionObjectPath);
                }
                catch (IOException e)
                {
                    LogBase.Error($"An error occurred while reading the auth object: {e.Message}");
                    modioModObject = string.Empty;
                }
            }
            else
            {
                LogBase.Error($"Could not find auth object at path: {configuration.SubscriptionObjectPath}");
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
