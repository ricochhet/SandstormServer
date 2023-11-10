using System.Collections.Generic;
using System.Text.Json;
using Sandstorm.Core.Configuration.Models;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;
using Sandstorm.Proxy.Configuration.Models;

namespace Sandstorm.Proxy.Configuration.Helpers;

public static class ProxyExtensionHelper
{
    private static string ConfigurationFileName
    {
        get { return "SandstormProxyExtensionCfg.json"; }
    }

    public static string ConfigurationPath
    {
        get { return $"./{ConfigurationFileName}"; }
    }

    public static void CheckFirstRun()
    {
        if (!FsProvider.Exists(ConfigurationPath))
        {
            LogBase.Info("Could not find proxy extension configuration file. Creating...");
            JsonSerializerOptions options = new() { WriteIndented = true };
            ProxyExtensionModel dummy = new ProxyExtensionModel() { Host="null", Path="null", Response="null" };
            List<ProxyExtensionModel> proxyExtensionModels = new List<ProxyExtensionModel> { dummy };
            ProxyExtensionConfigModel configurationModel =
                new()
                {
                    ProxyExtensionModels = proxyExtensionModels
                };
            string outputJson = JsonSerializer.Serialize(
                configurationModel,
                options
            );
            FsProvider.WriteFile("./", ConfigurationFileName, outputJson);
        }
    }

    public static ProxyExtensionConfigModel Read()
    {
        if (FsProvider.Exists(ConfigurationPath))
        {
            string fileData = FsProvider.ReadAllText(ConfigurationPath);
            ProxyExtensionConfigModel inputJson =
                JsonSerializer.Deserialize<ProxyExtensionConfigModel>(fileData);

            if (inputJson != null)
            {
                return inputJson;
            }

            return null;
        }

        return null;
    }
}
