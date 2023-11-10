using Sandstorm.Core.Configuration;
using Sandstorm.Core.Configuration.Enums;
using Sandstorm.Core.Configuration.Structs;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sandstorm.Core.Logger;

public class Logger : ILogger, IDisposable
{
    [DllImport("kernel32", SetLastError = true)]
    private static extern bool AllocConsole();

    private enum LogLevel
    {
        Benchmark = ConsoleColor.Gray,
        Debug = ConsoleColor.DarkGreen,
        Warn = ConsoleColor.DarkYellow,
        Error = ConsoleColor.DarkRed,
        Info = ConsoleColor.DarkCyan,
        Native = ConsoleColor.Magenta
    }

    private static readonly SemaphoreSlim Semaphore = new(1);
    private static readonly FileStream Stream = File.OpenWrite(LogConfiguration.OutputPath);

    public Logger()
    {
        _ = AllocConsole();
        Console.Title = "[DEBUG] SandstormServer";
    }

    public Task Debug(string message) => Write(LogLevel.Debug, message);

    public Task Debug(string format, params object[] args) => Write(LogLevel.Debug, string.Format(format, args));

    public Task Info(string message) => Write(LogLevel.Info, message);

    public Task Info(string format, params object[] args) => Write(LogLevel.Info, string.Format(format, args));

    public Task Warn(string message) => Write(LogLevel.Warn, message);

    public Task Warn(string format, params object[] args) => Write(LogLevel.Warn, string.Format(format, args));

    public Task Native(string message) => Write(LogLevel.Info, message);

    public Task Native(string format, params object[] args) => Write(LogLevel.Info, string.Format(format, args));

    public Task Error(string message) => Write(LogLevel.Error, message);

    public Task Error(string format, params object[] args) => Write(LogLevel.Error, string.Format(format, args));

    public Task Benchmark(string message) => Write(LogLevel.Info, message);

    public Task Benchmark(string format, params object[] args) => Write(LogLevel.Info, string.Format(format, args));

    private static async Task Write(LogLevel level, string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.Write($"[{DateTime.Now.ToLongTimeString()}]");
        Console.ForegroundColor = (ConsoleColor)level;
        Console.Write($"[{level.ToString().ToUpper()}] ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(message);

        try
        {
            await Semaphore.WaitAsync();
            await Stream.WriteAsync(Encoding.UTF8.GetBytes($"[{DateTime.Now.ToLongTimeString()}][{level}] {message}\n"));
            await Stream.FlushAsync();
        }
        catch { }
        finally
        {
            Semaphore.Release();
        }
    }

    public void Dispose()
    {
        try
        {
            GC.SuppressFinalize(this);
            Semaphore.Wait();
            Stream.Dispose();
        }
        catch { }
        finally
        {
            Semaphore.Release();
        }
    }
}
