using System;
using Sandstorm.Core.Logger;

namespace Sandstorm.Core.MiniCommon;

public static class CommandLine
{
    public static void ProcessArgument<T>(string[] args, string flag, Action<T> action)
    {
        int index = Array.IndexOf(args, flag);
        if (index != -1)
        {
            T value = (T)Convert.ChangeType(args[index + 1], typeof(T));
            action(value);
        }
    }

    public static void ProcessArgument(string[] args, string flag, Action action)
    {
        int index = Array.IndexOf(args, flag);
        if (index != -1)
        {
            action();
        }
    }

    public static void Pause()
    {
        LogBase.Info("Press \"F\" to safely exit.");
        while (Console.ReadKey(intercept: true).Key != ConsoleKey.F) { }
        return;
    }
}
