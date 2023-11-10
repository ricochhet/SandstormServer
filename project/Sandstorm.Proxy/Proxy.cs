using System;
using System.IO;
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
using Titanium.Web.Proxy.Http;
using Sandstorm.Proxy.Configuration.Models;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Collections.Generic;

namespace Sandstorm.Proxy;

public class Proxy
{
    private readonly ProxyServer proxyServer;

    private ExplicitProxyEndPoint explicitProxyEndPoint;

    private readonly int specifiedGameId;
    private readonly string modioAuthObject;
    private readonly bool admin;

    private readonly bool useProxyExtensions;
    private readonly ProxyExtensionConfigModel proxyExtensionConfigModel;
    private readonly List<string> proxyExtensionNames;

    public Proxy(int specifiedGameId, string modioAuthObject, bool useProxyExtensions = false, ProxyExtensionConfigModel proxyExtensionConfigModel = null, bool admin = false)
    {
        if (modioAuthObject == null || modioAuthObject == string.Empty)
        {
            throw new Exception("Auth object is null or empty.");
        }

        LogBase.Debug($"useProxyExtensions: {useProxyExtensions}");
        LogBase.Debug($"proxyExtensionConfigModel: {proxyExtensionConfigModel}");
        if (useProxyExtensions == true && proxyExtensionConfigModel != null)
        {
            this.useProxyExtensions = useProxyExtensions;
            this.proxyExtensionConfigModel = proxyExtensionConfigModel;
            foreach (ProxyExtensionModel proxyExtensionModel in proxyExtensionConfigModel.ProxyExtensionModels)
            {
                proxyExtensionNames.Add(proxyExtensionModel.Host);
            }
        }

        this.specifiedGameId = specifiedGameId;
        this.modioAuthObject = modioAuthObject;
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
            if (path.Contains("/v1/me/subscribed") || path.Contains($"/v1/games/{specifiedGameId}/mods"))
            {
                ResponseHelper.Response(modioAuthObject, e);
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
                LogBase.Warn($"WARNING: Host: {host + path} found but has no handle.");
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
        
        if (useProxyExtensions)
        {
            foreach (ProxyExtensionModel proxyExtensionModel in proxyExtensionConfigModel.ProxyExtensionModels)
            {
                if (host.Contains(proxyExtensionModel.Host) && path.Contains(proxyExtensionModel.Path))
                {
                    ResponseHelper.Response(proxyExtensionModel.Response, e);
                    LogBase.Warn($"WARNING: Handle: {proxyExtensionModel.Host + proxyExtensionModel.Path} came from the proxy extension configuration.");
                }
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
        if (!host.Contains("mod.io") && !proxyExtensionNames.Any(host.Contains))
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
