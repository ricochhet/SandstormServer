using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Sandstorm.Core.Logger;

public class LogBase
{
    private readonly List<ILogger> _io = new();
    private static LogBase _instance;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, Stopwatch> _benchmarkers = new();

    public static LogBase Instance
    {
        get
        {
            _instance ??= new LogBase();

            return _instance;
        }
    }

    public static async void Add(ILogger logger)
    {
        try
        {
            await _semaphore.WaitAsync();
            Instance._io.Add(logger);
        }
        catch { }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    public static async void Debug(string message)
    {
        try
        {
            await _semaphore.WaitAsync();

            foreach (ILogger logger in Instance._io)
                await logger.Debug(message);
        }
        catch { }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    public static async void Debug(string format, params object[] args)
    {
        try
        {
            await _semaphore.WaitAsync();

            foreach (ILogger logger in Instance._io)
                await logger.Debug(format, args);
        }
        catch { }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    public static async void Native(string message)
    {
        try
        {
            await _semaphore.WaitAsync();

            foreach (ILogger logger in Instance._io)
                await logger.Native(message);
        }
        catch { }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    public static async void Native(string format, params object[] args)
    {
        try
        {
            await _semaphore.WaitAsync();

            foreach (ILogger logger in Instance._io)
                await logger.Native(format, args);
        }
        catch { }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    public static async void Info(string message)
    {
        try
        {
            await _semaphore.WaitAsync();

            foreach (ILogger logger in Instance._io)
                await logger.Info(message);
        }
        catch { }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    public static async void Info(string format, params object[] args)
    {
        try
        {
            await _semaphore.WaitAsync();

            foreach (ILogger logger in Instance._io)
                await logger.Info(format, args);
        }
        catch { }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    public static async void Warn(string message)
    {
        try
        {
            await _semaphore.WaitAsync();

            foreach (ILogger logger in Instance._io)
                await logger.Warn(message);
        }
        catch { }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    public static async void Warn(string format, params object[] args)
    {
        try
        {
            await _semaphore.WaitAsync();

            foreach (ILogger logger in Instance._io)
                await logger.Warn(format, args);
        }
        catch { }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    public static async void Error(string message)
    {
        try
        {
            await _semaphore.WaitAsync();

            foreach (ILogger logger in Instance._io)
                await logger.Error(message);
        }
        catch { }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    public static async void Error(string format, params object[] args)
    {
        try
        {
            await _semaphore.WaitAsync();

            foreach (ILogger logger in Instance._io)
                await logger.Error(format, args);
        }
        catch { }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    private static async void BenchmarkLog(string message)
    {
        try
        {
            await _semaphore.WaitAsync();

            foreach (ILogger logger in Instance._io)
                await logger.Benchmark(message);
        }
        catch { }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    public static void Benchmark([CallerMemberName] string name = "")
    {
        if (Instance._benchmarkers.ContainsKey(name))
            return;

        BenchmarkLog($"Starting benchmark for '{name}'");
        Instance._benchmarkers.Add(name, Stopwatch.StartNew());
    }

    public static void BenchmarkEnd([CallerMemberName] string name = "")
    {
        if (!Instance._benchmarkers.ContainsKey(name))
            return;

        Stopwatch benchmarker = Instance._benchmarkers[name];
        BenchmarkLog($"Time taken for '{name}': {benchmarker.ElapsedMilliseconds}ms");
        benchmarker.Stop();
        _ = Instance._benchmarkers.Remove(name);
    }
}
