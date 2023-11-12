using System;
using Sandstorm.Core.Logger;

namespace Sandstorm.Core.Helpers;

public static class CommandLineHelper
{
    public static void Pause()
    {
        LogBase.Info("Press \"F\" to safely exit.");
        while (Console.ReadKey(intercept: true).Key != ConsoleKey.F) { }
        return;
    }
}
