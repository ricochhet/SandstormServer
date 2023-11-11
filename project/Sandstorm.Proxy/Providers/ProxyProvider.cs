using System;
using System.Net;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Network;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Sandstorm.Proxy.Helpers;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;

namespace Sandstorm.Proxy.Providers;

public class ProxyProvider
{
    private readonly ProxyServer proxyServer;

    private ExplicitProxyEndPoint explicitProxyEndPoint;

    private readonly int specifiedGameId;
    private readonly string modioModObject;
    private readonly bool admin;

    public ProxyProvider(
        int specifiedGameId,
        string modioModObject,
        bool admin = false
    )
    {
        if (modioModObject == null || modioModObject == string.Empty)
        {
            throw new Exception("Auth object is null or empty.");
        }

        this.specifiedGameId = specifiedGameId;
        this.modioModObject = modioModObject;
        this.admin = admin;
        if (this.admin)
        {
            proxyServer = new ProxyServer(
                userTrustRootCertificate: true,
                machineTrustRootCertificate: true,
                trustRootCertificateAsAdmin: true
            )
            {
                ForwardToUpstreamGateway = true
            };
        }
        else
        {
            proxyServer = new ProxyServer();
        }
        proxyServer.ExceptionFunc = delegate(Exception exception)
        {
            LogBase.Warn(
                exception.Message + ": " + exception.InnerException?.Message
            );
        };
        proxyServer.TcpTimeWaitSeconds = 10;
        proxyServer.ConnectionTimeOutSeconds = 15;
        if (FsProvider.Exists("./rootCert.pfx"))
        {
            LogBase.Info("Found rootCert.pfx");
            proxyServer.CertificateManager.RootCertificate =
                new X509Certificate2("./rootCert.pfx");
        }
        else
        {
            LogBase.Warn(
                "Could not find rootCert.pfx, generate a certificate and restart the server."
            );
        }
        proxyServer.CertificateManager.CertificateEngine =
            CertificateEngine.DefaultWindows;

        if (this.admin)
        {
            LogBase.Info("EnsureRootCertificate() as admin");
            proxyServer.CertificateManager.EnsureRootCertificate(
                userTrustRootCertificate: true,
                machineTrustRootCertificate: true,
                trustRootCertificateAsAdmin: true
            );
        }
        else
        {
            LogBase.Info("EnsureRootCertificate() as non-admin");
            proxyServer.CertificateManager.EnsureRootCertificate();
        }
    }

    public void StartProxy()
    {
        proxyServer.BeforeRequest += OnRequest;
        proxyServer.ServerCertificateValidationCallback +=
            OnCertificateValidation;
        explicitProxyEndPoint = new ExplicitProxyEndPoint(
            IPAddress.Any,
            GetFreeTCPPort()
        );
        explicitProxyEndPoint.BeforeTunnelConnectRequest +=
            OnBeforeTunnelConnectRequest;
        proxyServer.AddEndPoint(explicitProxyEndPoint);
        proxyServer.Start();
        proxyServer.SetAsSystemProxy(
            explicitProxyEndPoint,
            ProxyProtocolType.AllHttp,
            LocalHostAddr.IP
        );
    }

    public void Stop()
    {
        proxyServer.RestoreOriginalProxySettings();
        explicitProxyEndPoint.BeforeTunnelConnectRequest -=
            OnBeforeTunnelConnectRequest;
        proxyServer.BeforeRequest -= OnRequest;
        proxyServer.ServerCertificateValidationCallback -=
            OnCertificateValidation;
        proxyServer.Stop();
    }

    public static Task OnCertificateValidation(
        object sender,
        CertificateValidationEventArgs e
    )
    {
        e.IsValid = true;
        return Task.CompletedTask;
    }

    private Task OnRequest(object sender, SessionEventArgs e)
    {
        string path = e.HttpClient.Request.RequestUri.AbsolutePath;
        string host = e.HttpClient.Request.RequestUri.Host;
        e.HttpClient.Response.ContentType = "application/json";

        if (host.Contains("api.mod.io"))
        {
            if (
                path.Contains("/v1/me/subscribed")
                || path.Contains($"/v1/games/{specifiedGameId}/mods")
            )
            {
                ResponseHelper.Response(modioModObject, e);
            }
            else if (path.Contains("/v1/me"))
            {
                ResponseHelper.Response(ResponseHelper.User, e);
            }
            else if (path.Contains("/v1/authenticate/terms"))
            {
                ResponseHelper.Response(ResponseHelper.Terms, e);
            }
            else if (path.Contains("/v1/external/steamauth"))
            {
                ResponseHelper.Response(ResponseHelper.Steam, e);
            }
            else
            {
                ResponseHelper.Response(ResponseHelper.NotFound, e);
                LogBase.Warn(
                    $"WARNING: Host: {host + path} found but has no handle."
                );
            }
        }
        else if (host.Contains("mod.io"))
        {
            switch (path)
            {
                default:
                    ResponseHelper.Response(ResponseHelper.NotFound, e);
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private Task OnBeforeTunnelConnectRequest(
        object sender,
        TunnelConnectSessionEventArgs e
    )
    {
        string host = e.HttpClient.Request.RequestUri.Host;
        if (!host.Contains("mod.io"))
        {
            e.DecryptSsl = false;
        }

        return Task.CompletedTask;
    }

    private static int GetFreeTCPPort()
    {
        TcpListener tcpListener = new(IPAddress.Loopback, 0);
        tcpListener.Start();
        int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
        tcpListener.Stop();
        return port;
    }
}
