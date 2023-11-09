using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Sandstorm.Core.Configuration;
using Sandstorm.Core.Logger;
using Sandstorm.Core.Providers;
using Sandstorm.Proxy;
using System.Threading.Tasks;

namespace Sandstorm;

internal class Program
{
	private static Proxy.Proxy proxy;

	private static void Main()
	{
		Console.Title = "SandstormServer";
		ILogger logger = new NativeLogger();
		LogBase.Add(logger);
		LogBase.Info("Insurgency: Sandstorm Service Emulator");

				try
		{
			proxy = new Proxy.Proxy();
		}
		catch (Exception ex)
		{
			LogBase.Error("Error while initializing proxy.");
			LogBase.Error("==============================");
			LogBase.Error(ex.Message);
			LogBase.Error(ex.StackTrace);
			LogBase.Error("==============================");
			LogBase.Error(ex.InnerException.Message);
			LogBase.Error(ex.InnerException.StackTrace);
			while (Console.ReadKey(intercept: true).Key != ConsoleKey.F)
			{
			}
			return;
		}

		proxy.StartProxy();
		LogBase.Info("WARNING: DO NOT MANUALLY CLOSE THIS WINDOW! If you do and your internet breaks clear your proxy settings and restart your computer.\n");
		LogBase.Info("==============================");
		LogBase.Info("Intercepting connections... Now run Insurgency: Sandstorm!");
		LogBase.Info("Press \"F\" to safely close the server.");
		LogBase.Info("==============================");
		
		if (Console.ReadKey(intercept: true).Key == ConsoleKey.F)
		{
			LogBase.Info("Exiting...");
			proxy.Stop();
		}
	}
}
