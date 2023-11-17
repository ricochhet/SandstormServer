using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;
using Sandstorm.Core.Proxy.Helpers;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Network;

namespace Sandstorm.Core.Proxy.Providers;

public class ProxyProvider
{
    private readonly ProxyServer proxyServer;
    private ExplicitProxyEndPoint explicitProxyEndPoint;

    private readonly int id;
    private readonly string response;
    private readonly bool admin;
    private readonly LocalHostAddr localHostAddr = LocalHostAddr.IP;

    public ProxyProvider(int id, string response, bool admin = false, LocalHostAddr localHostAddr = LocalHostAddr.IP)
    {
        this.id = id;
        this.response = response;
        this.admin = admin;
        this.localHostAddr = localHostAddr;
        if (this.admin)
        {
            proxyServer = new ProxyServer(userTrustRootCertificate: true, machineTrustRootCertificate: true, trustRootCertificateAsAdmin: true) { ForwardToUpstreamGateway = true };
        }
        else
        {
            proxyServer = new ProxyServer();
        }
        proxyServer.ExceptionFunc = delegate(Exception exception)
        {
            LogBase.Warn(exception.Message + ": " + exception.InnerException?.Message);
        };
        proxyServer.TcpTimeWaitSeconds = 10;
        proxyServer.ConnectionTimeOutSeconds = 15;
        if (FsProvider.Exists("./rootCert.pfx"))
        {
            LogBase.Info("Found rootCert.pfx");
            proxyServer.CertificateManager.RootCertificate = new X509Certificate2("./rootCert.pfx");
        }
        else
        {
            LogBase.Warn("Could not find rootCert.pfx, generate a certificate and restart the server.");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            proxyServer.CertificateManager.CertificateEngine = CertificateEngine.DefaultWindows;
        }
        else
        {
            proxyServer.CertificateManager.CertificateEngine = CertificateEngine.BouncyCastle;
        }

        if (this.admin)
        {
            LogBase.Info("EnsureRootCertificate() as admin");
            proxyServer.CertificateManager.EnsureRootCertificate(userTrustRootCertificate: true, machineTrustRootCertificate: true, trustRootCertificateAsAdmin: true);
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
        proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
        explicitProxyEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, GetFreeTCPPort());
        explicitProxyEndPoint.BeforeTunnelConnectRequest += OnBeforeTunnelConnectRequest;
        proxyServer.AddEndPoint(explicitProxyEndPoint);
        proxyServer.Start();
        proxyServer.SetAsSystemProxy(explicitProxyEndPoint, ProxyProtocolType.AllHttp, localHostAddr);
    }

    public void Stop()
    {
        proxyServer.RestoreOriginalProxySettings();
        explicitProxyEndPoint.BeforeTunnelConnectRequest -= OnBeforeTunnelConnectRequest;
        proxyServer.BeforeRequest -= OnRequest;
        proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
        proxyServer.Stop();
    }

    public static Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
    {
        e.IsValid = true;
        return Task.CompletedTask;
    }

    private Task OnRequest(object sender, SessionEventArgs e)
    {
        return ModioProxyService.OnRequest(e, response, id);
    }

    private Task OnBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
    {
        return ModioProxyService.OnBeforeTunnelConnectRequest(e);
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
