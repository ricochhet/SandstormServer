using System.Threading.Tasks;
using Sandstorm.Core.Logger;
using Titanium.Web.Proxy.EventArguments;

namespace Sandstorm.Proxy.Helpers;

public static class ModioProxyService
{
    public static Task OnRequest(SessionEventArgs e, string response, int id)
    {
        string path = e.HttpClient.Request.RequestUri.AbsolutePath;
        string host = e.HttpClient.Request.RequestUri.Host;
        e.HttpClient.Response.ContentType = "application/json";

        if (host.Contains("api.mod.io"))
        {
            if (path.Contains("/v1/me/subscribed") || path.Contains($"/v1/games/{id}/mods"))
            {
                ModioResponseHelper.Response(response, e);
            }
            else if (path.Contains("/v1/me"))
            {
                ModioResponseHelper.Response(ModioResponseHelper.User, e);
            }
            else if (path.Contains("/v1/authenticate/terms"))
            {
                ModioResponseHelper.Response(ModioResponseHelper.Terms, e);
            }
            else if (path.Contains("/v1/external/steamauth"))
            {
                ModioResponseHelper.Response(ModioResponseHelper.Steam, e);
            }
            else
            {
                ModioResponseHelper.Response(ModioResponseHelper.NotFound, e);
                LogBase.Warn($"Found: {host + path}, but it has no handle.");
            }
        }
        else if (host.Contains("mod.io"))
        {
            switch (path)
            {
                default:
                    ModioResponseHelper.Response(ModioResponseHelper.NotFound, e);
                    break;
            }
        }

        return Task.CompletedTask;
    }

    public static Task OnBeforeTunnelConnectRequest(TunnelConnectSessionEventArgs e)
    {
        string host = e.HttpClient.Request.RequestUri.Host;
        if (!host.Contains("mod.io"))
        {
            e.DecryptSsl = false;
        }

        return Task.CompletedTask;
    }
}