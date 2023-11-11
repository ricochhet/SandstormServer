using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Sandstorm.Core.Logger;

namespace Sandstorm.Proxy.Helpers;

public static class WindowsAdminHelper
{
    public static bool IsAdmin()
    {
        bool result = false;
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                result = new WindowsPrincipal(
                    WindowsIdentity.GetCurrent()
                ).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        catch (Exception e)
        {
            LogBase.Error(e.ToString());
        }
        return result;
    }
}
