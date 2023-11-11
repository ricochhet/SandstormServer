using System;
using System.IO;
using System.Runtime.InteropServices;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;
using Sandstorm.Core.Configuration.Helpers;
using Sandstorm.Core.Configuration.Models;
using System.Security.Principal;

namespace Sandstorm;

internal class Program
{
    private static Proxy.ProxyProvider proxy;

    private static void Main()
    {
        Console.Title = "SandstormProxy";
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

        string modioModObject;
        if (FsProvider.Exists(configuration.SubscriptionObjectPath))
        {
            try
            {
                modioModObject = FsProvider.ReadAllText(
                    configuration.SubscriptionObjectPath
                );
            }
            catch (IOException e)
            {
                LogBase.Error(
                    $"An error occurred while reading the auth object: {e.Message}"
                );
                modioModObject = string.Empty;
            }
        }
        else
        {
            LogBase.Error(
                $"Could not find auth object at path: {configuration.SubscriptionObjectPath}"
            );
            PauseAndWarn();
            return;
        }

        try
        {
            proxy = new Proxy.ProxyProvider(
                configuration.SpecifyModIOGameId,
                modioModObject,
                CheckAdmin()
            );
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
            while (Console.ReadKey(intercept: true).Key != ConsoleKey.F) { }
            return;
        }

        proxy.StartProxy();
        LogBase.Warn(
            "WARNING: DO NOT MANUALLY CLOSE THIS WINDOW! If you do and your internet breaks clear your proxy settings and restart your computer."
        );
        LogBase.Info("==============================");
        LogBase.Info(
            "Intercepting connections... Now run Insurgency: Sandstorm!"
        );
        LogBase.Info("Press \"F\" to safely close the server.");
        LogBase.Info("==============================");

        if (Console.ReadKey(intercept: true).Key == ConsoleKey.F)
        {
            LogBase.Info("Exiting...");
            proxy.Stop();
        }
    }

    private static void PauseAndWarn()
    {
        LogBase.Warn("Press \"F\" to safely exit.");
        while (Console.ReadKey(intercept: true).Key != ConsoleKey.F) { }
        return;
    }

    private static bool CheckAdmin()
    {
        bool result = false;
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                result = new WindowsPrincipal(
                    WindowsIdentity.GetCurrent()
                ).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        catch (Exception e)
        {
            LogBase.Error(e.ToString());
        }
        return result;
    }
}
