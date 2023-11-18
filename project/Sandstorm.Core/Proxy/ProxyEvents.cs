using System.Net;
using System.Threading.Tasks;
using Sandstorm.Core.Logger;
using Titanium.Web.Proxy.EventArguments;

namespace Sandstorm.Core.Proxy;

public static class ProxyEvents
{
    private static readonly string respNotFound =
        "{\"error\":{\"code\":404,\"error_ref\":14000,\"message\":\"The resource requested could not be found.\"}}";
    private static readonly string respUser =
        "{\"id\":1234567,\"name_id\":\"player\",\"username\":\"Player\",\"display_name_portal\":null,\"date_online\":1696969690,\"date_joined\":1696900000,\"avatar\":{\"filename\":\"image.jpg\",\"original\":\"https:\\/\\/example.com\\/image.jpg\",\"thumb_50x50\":\"https:\\/\\/example.com\\/image.jpg\",\"thumb_100x100\":\"https:\\/\\/example.com\\/image.jpg\"},\"timezone\":\"\",\"language\":\"\",\"profile_url\":\"https:\\/\\/example.com\"}";
    private static readonly string respSteamAuth =
        "{\"code\":200,\"access_token\":\"0000000\",\"date_expires\":1999999999}";
    private static readonly string respModioTerms =
        "{\"plaintext\":\"We use mod.io to support user-generated content in-game. By clicking \\\"I Agree\\\" you agree to the mod.io Terms of Use and a mod.io account will be created for you (using your display name, avatar and ID). Please see the mod.io Privacy Policy on how mod.io processes your personal data.\",\"html\":\"<p>We use <a href=\\\"https:\\/\\/mod.io\\\">mod.io<\\/a> to support user-generated content in-game. By clicking \\\"I Agree\\\" you agree to the mod.io <a href=\\\"https:\\/\\/mod.io\\/terms\\\">Terms of Use<\\/a> and a mod.io account will be created for you (using your  display name, avatar and ID). Please see the mod.io <a href=\\\"https:\\/\\/mod.io\\/privacy\\\">Privacy Policy<\\/a> on how mod.io processes your personal data.<\\/p>\",\"buttons\":{\"agree\":{\"text\":\"I Agree\"},\"disagree\":{\"text\":\"No, Thanks\"}},\"links\":{\"website\":{\"text\":\"mod.io\",\"url\":\"https:\\/\\/mod.io\",\"required\":false},\"terms\":{\"text\":\"Terms of Use\",\"url\":\"https:\\/\\/mod.io\\/terms\",\"required\":true},\"privacy\":{\"text\":\"Privacy Policy\",\"url\":\"https:\\/\\/mod.io\\/privacy\",\"required\":true},\"manage\":{\"text\":\"Manage Account\",\"url\":\"https:\\/\\/mod.io\\/me\\/account\",\"required\":false}}}";

    private static void Ok(string response, SessionEventArgs e)
    {
        e.Ok(response);
        e.HttpClient.Response.ContentType = "application/json";
        LogBase.Info(
            $"SUCCESS: {e.HttpClient.Request.RequestUri.Host + e.HttpClient.Request.RequestUri.AbsolutePath} | Content-Length: {response.Length}"
        );
    }

    public static Task OnRequest(SessionEventArgs e, int id, string response, bool hasConnection)
    {
        string path = e.HttpClient.Request.RequestUri.AbsolutePath;
        string host = e.HttpClient.Request.RequestUri.Host;
        e.HttpClient.Response.ContentType = "application/json";

        if (host.Contains("api.mod.io"))
        {
            if (path.Contains("/v1/me/subscribed") || path.Contains($"/v1/games/{id}/mods"))
            {
                Ok(response, e);
            }
            else if (path.Contains("/v1/me"))
            {
                Ok(respUser, e);
            }
            else if (path.Contains("/v1/authenticate/terms"))
            {
                Ok(respModioTerms, e);
            }
            else if (path.Contains("/v1/external/steamauth"))
            {
                Ok(respSteamAuth, e);
            }
            else
            {
                Ok(respNotFound, e);
                LogBase.Warn($"Found: {host + path}, but it has no handle.");
            }
        }
        else if (host.Contains("mod.io"))
        {
            Ok(respNotFound, e);
        }
        else if (!hasConnection)
        {
            e.GenericResponse("502 Bad Gateway", HttpStatusCode.BadGateway);
        }

        return Task.CompletedTask;
    }

    public static Task OnBeforeTunnelConnectRequest(TunnelConnectSessionEventArgs e, bool hasConnection)
    {
        if (hasConnection)
        {
            string host = e.HttpClient.Request.RequestUri.Host;
            if (!host.Contains("mod.io"))
            {
                e.DecryptSsl = false;
            }
        }
        else
        {
            e.DecryptSsl = true;
        }

        return Task.CompletedTask;
    }
}
