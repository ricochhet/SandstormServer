﻿using System;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Network.Tcp;

namespace Titanium.Web.Proxy.Network;

internal class RetryPolicy<T> where T : Exception
{
    private readonly int retries;
    private readonly TcpConnectionFactory tcpConnectionFactory;

    private TcpServerConnection? currentConnection;

    internal RetryPolicy(int retries, TcpConnectionFactory tcpConnectionFactory)
    {
        this.retries = retries;
        this.tcpConnectionFactory = tcpConnectionFactory;
    }

    /// <summary>
    ///     Execute and retry the given action until retry number of times.
    /// </summary>
    /// <param name="action">The action to retry with return value specifying whether caller should continue execution.</param>
    /// <param name="generator">The Tcp connection generator to be invoked to get new connection for retry.</param>
    /// <param name="initialConnection">Initial Tcp connection to use.</param>
    /// <returns>Returns the latest connection used and the latest exception if any.</returns>
    internal async Task<RetryResult> ExecuteAsync(Func<TcpServerConnection, Task<bool>> action,
        Func<Task<TcpServerConnection>> generator, TcpServerConnection? initialConnection)
    {
        currentConnection = initialConnection;
        var @continue = true;
        Exception? exception = null;

        var attempts = retries;

        while (true)
        {
            // setup connection
            currentConnection ??= await generator();

            try
            {
                @continue = await action(currentConnection);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            attempts--;

            if (attempts < 0 || exception == null || !(exception is T)) break;

            exception = null;

            // before retry clear connection
            await tcpConnectionFactory.Release(currentConnection, true);
            currentConnection = null;
        }

        return new RetryResult(currentConnection, exception, @continue);
    }
}

internal class RetryResult
{
    internal RetryResult(TcpServerConnection? lastConnection, Exception? exception, bool @continue)
    {
        LatestConnection = lastConnection;
        Exception = exception;
        Continue = @continue;
    }

    internal TcpServerConnection? LatestConnection { get; }

    internal Exception? Exception { get; }

    internal bool Continue { get; }
}