using System;
using System.IO;
using System.Threading.Tasks;
using Sandstorm.Core.Helpers;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Proxy;
using Sandstorm.Proxy.Helpers;
using Titanium.Web.Proxy.Models;
using Sandstorm.Core.Models;
using Sandstorm.Core.MiniCommon;

namespace Sandstorm.Launcher;

internal class Program
{
    private static ProxyService proxy;
    private static LocalHostAddr localHostAddr = LocalHostAddr.IP;
    private static bool hasConnection = true;

    private static async Task Main(string[] args)
    {
        Console.Title = "Sandstorm.Launcher";
        LogBase.Add(new NativeLogger());
        LogBase.Add(new FileStreamLogger());
        Watermark.Draw(
            new()
            {
                "Insurgency: Sandstorm Service Emulator",
                "This work is free of charge",
                "If you paid money, you were scammed"
            }
        );

        SettingsProvider.Write();
        SettingsModel settings = SettingsProvider.Read();
        if (settings == null)
        {
            LogBase.Error(
                $"{SettingsProvider.SettingsFileName} could not be read, make sure it is located in \"SandstormServer_Data\" and try again."
            );
            CommandLine.Pause();
            return;
        }

        string processFileName = null;
        if (args.Length != 0)
        {
            CommandLine.ProcessArgument(args, "--gameid", (int value) => settings.GameId = value);
            CommandLine.ProcessArgument(
                args,
                "--subscribe",
                async (int value) => await ModioApiProvider.Add(settings, value)
            );
            CommandLine.ProcessArgument(args, "--build", () => ModioApiProvider.Write(settings));
            CommandLine.ProcessArgument(args, "--launch", (string value) => processFileName = value);
            CommandLine.ProcessArgument(
                args,
                "--host",
                (string value) =>
                {
                    if (value == "localhost")
                        localHostAddr = LocalHostAddr.LocalHost;
                }
            );
            CommandLine.ProcessArgument(args, "--offline", () => hasConnection = false);
        }

        if (settings.GameId == -1)
        {
            LogBase.Warn($"The game id has not been set in \"{SettingsProvider.SettingsFileName}\".");
            CommandLine.Pause();
            return;
        }

        if (settings.AddToSubscription.Count == 0)
        {
            LogBase.Warn($"The mod subscriptions have not been set in \"{SettingsProvider.SettingsFileName}\".");
            CommandLine.Pause();
            return;
        }

        if (settings.ApiKey == SettingsProvider.ApiKeyDefault || string.IsNullOrEmpty(settings.ApiKey))
        {
            LogBase.Warn($"The mod.io API key has not been set in \"{SettingsProvider.SettingsFileName}\".");
            CommandLine.Pause();
            return;
        }

        try
        {
            await ModioApiProvider.Subscribe(settings);
            string subscriptionFilePath = Path.Combine(
                SettingsProvider.SandstormDataPath,
                settings.GameId.ToString(),
                SettingsProvider.ModObjectFileName
            );
            if (!FsProvider.Exists(subscriptionFilePath))
            {
                LogBase.Error($"Could not find the mod subscription data at: {subscriptionFilePath}");
                CommandLine.Pause();
                return;
            }

            string response;
            try
            {
                string fileData = FsProvider.ReadAllText(subscriptionFilePath);
                if (string.IsNullOrEmpty(fileData))
                {
                    LogBase.Error("The mod subscription data is either null or empty.");
                    CommandLine.Pause();
                    return;
                }
                response = fileData;
            }
            catch (IOException e)
            {
                LogBase.Error($"An error occurred while reading the mod subscription data: {e.Message}");
                response = string.Empty;
            }

            if (string.IsNullOrEmpty(response))
                return;

            proxy = new ProxyService(
                settings.GameId,
                response,
                WindowsAdminHelper.IsAdmin(),
                localHostAddr,
                SettingsProvider.SandstormDataPath,
                hasConnection
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
            CommandLine.Pause();
            return;
        }

        proxy.StartProxy();
        LogBase.Warn(
            "DO NOT MANUALLY CLOSE THIS WINDOW! If you do and your internet breaks clear your proxy settings and restart your computer."
        );
        LogBase.Info("==============================");
        LogBase.Info("Intercepting connections... Now run Insurgency: Sandstorm!");
        LogBase.Info("Press \"F\" to safely close the server.");
        LogBase.Info("==============================");

        if (!string.IsNullOrEmpty(processFileName))
        {
            LogBase.Info($"Attempting to start process: {processFileName}");
            ProcessHelper.RunProcess(processFileName);
        }

        if (Console.ReadKey(intercept: true).Key == ConsoleKey.F)
        {
            LogBase.Info("Exiting...");
            proxy.Stop();
        }
    }
}
