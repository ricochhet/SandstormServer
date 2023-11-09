using System;

namespace Titanium.Web.Proxy.Models;

[Flags]
public enum LocalHostAddr
{
    /// <summary>
    ///     IP
    /// </summary>
    IP = 0,

    /// <summary>
    ///     LocalHost
    /// </summary>
    LocalHost = 1,
}