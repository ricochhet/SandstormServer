﻿using System;
using Titanium.Web.Proxy.Network.Tcp;

namespace Titanium.Web.Proxy.EventArguments;

/// <summary>
///     The base event arguments
/// </summary>
/// <seealso cref="System.EventArgs" />
public abstract class ProxyEventArgsBase : EventArgs
{
    private readonly TcpClientConnection clientConnection;
    internal readonly ProxyServer Server;

    internal ProxyEventArgsBase(ProxyServer server, TcpClientConnection clientConnection)
    {
        this.clientConnection = clientConnection;
        Server = server;
    }

    public object ClientUserData
    {
        get => clientConnection.ClientUserData;
        set => clientConnection.ClientUserData = value;
    }
}