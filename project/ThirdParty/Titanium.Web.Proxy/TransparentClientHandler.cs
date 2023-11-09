﻿using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Extensions;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Network.Tcp;
using Titanium.Web.Proxy.StreamExtended;

namespace Titanium.Web.Proxy;

public partial class ProxyServer
{
    /// <summary>
    ///     This is called when this proxy acts as a reverse proxy (like a real http server).
    ///     So for HTTPS requests we would start SSL negotiation right away without expecting a CONNECT request from client
    /// </summary>
    /// <param name="endPoint">The transparent endpoint.</param>
    /// <param name="clientConnection">The client connection.</param>
    /// <returns></returns>
    private Task HandleClient(TransparentProxyEndPoint endPoint, TcpClientConnection clientConnection)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        return HandleClient(endPoint, clientConnection, endPoint.Port, cancellationTokenSource, cancellationToken);
    }

    private async Task HandleClient(TransparentBaseProxyEndPoint endPoint, TcpClientConnection clientConnection,
        int port, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
    {
        var isHttps = false;
        var clientStream = new HttpClientStream(this, clientConnection, clientConnection.GetStream(), BufferPool,
            cancellationToken);

        try
        {
            var clientHelloInfo = await SslTools.PeekClientHello(clientStream, BufferPool, cancellationToken);

            if (clientHelloInfo != null)
            {
                var httpsHostName = clientHelloInfo.GetServerName() ?? endPoint.GenericCertificateName;

                var args = new BeforeSslAuthenticateEventArgs(this, clientConnection, cancellationTokenSource,
                    httpsHostName);

                await endPoint.InvokeBeforeSslAuthenticate(this, args, ExceptionFunc);

                if (cancellationTokenSource.IsCancellationRequested)
                    throw new Exception("Session was terminated by user.");

                if (endPoint.DecryptSsl && args.DecryptSsl)
                {
                    var sslProtocol = clientHelloInfo.SslProtocol & SupportedSslProtocols;
                    if (sslProtocol == SslProtocols.None)
                    {
                        throw new Exception("Unsupported client SSL version.");
                    }

                    clientStream.Connection.SslProtocol = sslProtocol;

                    // do client authentication using certificate
                    X509Certificate2? certificate = null;
                    SslStream? sslStream = null;
                    try
                    {
                        sslStream = new SslStream(clientStream, false);

                        var certName = HttpHelper.GetWildCardDomainName(httpsHostName,
                            CertificateManager.DisableWildCardCertificates);
                        certificate = endPoint.GenericCertificate ??
                                      await CertificateManager.CreateServerCertificate(certName);

                        // Successfully managed to authenticate the client using the certificate
                        await sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls12, false);

                        // HTTPS server created - we can now decrypt the client's traffic
                        clientStream = new HttpClientStream(this, clientStream.Connection, sslStream, BufferPool,
                            cancellationToken);
                        sslStream = null; // clientStream was created, no need to keep SSL stream reference
                        isHttps = true;
                    }
                    catch (Exception e)
                    {
                        sslStream?.Dispose();

                        var certName = certificate?.GetNameInfo(X509NameType.SimpleName, false);
                        var session = new SessionEventArgs(this, endPoint, clientStream, null, cancellationTokenSource);
                        throw new ProxyConnectException(
                            $"Couldn't authenticate host '{httpsHostName}' with certificate '{certName}'.", e, session);
                    }
                }
                else
                {
                    var sessionArgs = new SessionEventArgs(this, endPoint, clientStream, null, cancellationTokenSource);
                    var connection = (await TcpConnectionFactory.GetServerConnection(this, args.ForwardHttpsHostName,
                        args.ForwardHttpsPort,
                        HttpHeader.VersionUnknown, false, null,
                        true, sessionArgs, UpStreamEndPoint,
                        UpStreamHttpsProxy, true, false, cancellationToken))!;

                    try
                    {
                        var available = clientStream.Available;

                        if (available > 0)
                        {
                            // send the buffered data
                            var data = BufferPool.GetBuffer();
                            try
                            {
                                // clientStream.Available should be at most BufferSize because it is using the same buffer size
                                await clientStream.ReadAsync(data, 0, available, cancellationToken);
                                await connection.Stream.WriteAsync(data, 0, available, true, cancellationToken);
                            }
                            finally
                            {
                                BufferPool.ReturnBuffer(data);
                            }
                        }

                        if (!clientStream.IsClosed && !connection.Stream.IsClosed)
                            await TcpHelper.SendRaw(clientStream, connection.Stream, BufferPool,
                                null, null, cancellationTokenSource, ExceptionFunc);
                    }
                    finally
                    {
                        await TcpConnectionFactory.Release(connection, true);
                    }

                    return;
                }
            }

            // HTTPS server created - we can now decrypt the client's traffic
            // Now create the request
            await HandleHttpSessionRequest(endPoint, clientStream, cancellationTokenSource, isHttps: isHttps);
        }
        catch (ProxyException e)
        {
            OnException(clientStream, e);
        }
        catch (IOException e)
        {
            OnException(clientStream, new Exception("Connection was aborted", e));
        }
        catch (SocketException e)
        {
            OnException(clientStream, new Exception("Could not connect", e));
        }
        catch (Exception e)
        {
            OnException(clientStream, new Exception("Error occured in whilst handling the client", e));
        }
        finally
        {
            clientStream.Dispose();
        }
    }
}