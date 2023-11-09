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

namespace Sandstorm.Proxy;

public class Proxy
{
	private readonly ProxyServer proxyServer;

	private ExplicitProxyEndPoint explicitProxyEndPoint;

	private readonly bool admin;

	private readonly string modioAuthModel;

	public Proxy(bool admin = false)
	{
		try 
		{
			modioAuthModel = File.ReadAllText("./Models/subscription.json");
		} 
		catch (IOException e)
		{
			Console.WriteLine("An error occurred while reading the file: " + e.Message);
			modioAuthModel = string.Empty;
		}

		this.admin = admin;
		if (this.admin)
		{
			proxyServer = new ProxyServer(userTrustRootCertificate: true, machineTrustRootCertificate: true, trustRootCertificateAsAdmin: true) { ForwardToUpstreamGateway = true };
		}
		else
		{
			proxyServer = new ProxyServer();
		}
		proxyServer.ExceptionFunc = async delegate (Exception exception)
		{
			Console.WriteLine(exception.Message + ": " + exception.InnerException?.Message);
			Console.WriteLine();
		};
		proxyServer.TcpTimeWaitSeconds = 10;
		proxyServer.ConnectionTimeOutSeconds = 15;
		proxyServer.CertificateManager.RootCertificate = new X509Certificate2("./rootCert.pfx");
		proxyServer.CertificateManager.CertificateEngine = CertificateEngine.DefaultWindows;

		if (this.admin)
		{
			Console.WriteLine("EnsureRootCertificate() as admin");
			proxyServer.CertificateManager.EnsureRootCertificate(userTrustRootCertificate: true, machineTrustRootCertificate: true, trustRootCertificateAsAdmin: true);
		}
		else
		{
			Console.WriteLine("EnsureRootCertificate() as non-admin");
			proxyServer.CertificateManager.EnsureRootCertificate();
		}
	}

	public void StartProxy()
	{
		proxyServer.BeforeRequest += OnRequest;
		proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
		explicitProxyEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8080);
		explicitProxyEndPoint.BeforeTunnelConnectRequest += OnBeforeTunnelConnectRequest;
		proxyServer.AddEndPoint(explicitProxyEndPoint);
		proxyServer.Start();
		proxyServer.SetAsSystemProxy(explicitProxyEndPoint, ProxyProtocolType.AllHttp, LocalHostAddr.IP);
	}

	public void Stop()
	{
		proxyServer.RestoreOriginalProxySettings();
		explicitProxyEndPoint.BeforeTunnelConnectRequest -= OnBeforeTunnelConnectRequest;
		proxyServer.BeforeRequest -= OnRequest;
		proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
		proxyServer.Stop();
	}

	public async Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
	{
        e.IsValid = true;
    }

	private async Task OnRequest(object sender, SessionEventArgs e)
	{
		string path = e.HttpClient.Request.RequestUri.AbsolutePath;
		string host = e.HttpClient.Request.RequestUri.Host;
		e.HttpClient.Response.ContentType = "application.json";

		if (host.Contains("api.mod.io") && path.Contains("v1/me/subscribed"))
		{
			e.Ok(modioAuthModel);
			e.HttpClient.Response.ContentType = "application/json";
			Console.WriteLine($"SUCCESS (mod.io): {host + path}");
		}
		else if (host.Contains("mod.io"))
		{
			switch (path)
			{
				default:
					e.Ok(ResponseHelper.NotFound());
					e.HttpClient.Response.ContentType = "application/json";
					Console.WriteLine($"SUCCESS (mod.io): {host + path}");
					break;
			}
		}
	}

	private async Task OnBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
	{
		string host = e.HttpClient.Request.RequestUri.Host;
		if (!host.Contains("mod.io"))
		{
			e.DecryptSsl = false;
		}
	}

	private static int GetFreeTCPPort()
	{
		TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);
		tcpListener.Start();
		int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
		tcpListener.Stop();
		return port;
	}
}
