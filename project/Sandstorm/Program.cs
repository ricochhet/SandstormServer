using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Sandstorm.Proxy;

namespace Sandstorm;

internal class Program
{
	private static Proxy.Proxy proxy;

	private static void Main(string[] args)
	{
		Console.Title = "SandstormServer";
		Console.WriteLine("Insurgency: Sandstorm Service Emulator");
		Console.WriteLine();

		try
		{
			proxy = new Proxy.Proxy();
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error while initializing proxy.");
			Console.WriteLine("==============================");
			Console.WriteLine(ex.Message);
			Console.WriteLine(ex.StackTrace);
			Console.WriteLine("==============================");
			Console.WriteLine(ex.InnerException.Message);
			Console.WriteLine(ex.InnerException.StackTrace);
			while (Console.ReadKey(intercept: true).Key != ConsoleKey.F)
			{
			}
			return;
		}

		Console.WriteLine();
		proxy.StartProxy();
		Console.WriteLine();
		Console.WriteLine("WARNING: DO NOT MANUALLY CLOSE THIS WINDOW! If you do and your internet breaks clear your proxy settings and restart your computer.\n");
		Console.WriteLine("==============================");
		Console.WriteLine("Intercepting connections... Now run Insurgency: Sandstorm!");
		Console.WriteLine("Press \"F\" to safely close the server.");
		Console.WriteLine("==============================");
		Console.WriteLine();
		
		if (Console.ReadKey(intercept: true).Key == ConsoleKey.F)
		{
			Console.WriteLine("Exiting...");
			proxy.Stop();
		}
	}
}
