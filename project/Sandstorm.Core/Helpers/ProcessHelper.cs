using System;
using System.Diagnostics;
using Sandstorm.Core.Logger;

namespace Sandstorm.Core.Helpers;

public static class ProcessHelper
{
    public static void RunProcess(string fileName)
    {
        try
        {
            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = true
                },
            };

            process.Start();
        }
        catch (Exception e)
        {
            LogBase.Error(e.ToString());
        }
    }
}
