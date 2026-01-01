using System;
using System.Diagnostics;

namespace DiningPhilosophers.Utils;

public static class ConsoleHelper
{
	private static readonly object _consoleLock = new object();
	private static readonly Stopwatch _stopwatch = new Stopwatch();
	private static bool _shouldStop = false;

	public static void StartTimer()
	{
		_stopwatch.Start();
	}

	public static long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
	public static bool ShouldStop => _shouldStop;

	public static void Write(int philosopherID, string action)
	{
		lock (_consoleLock)
		{
			if (_shouldStop) return;

			if (action.Contains("died"))
				_shouldStop = true;

			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write($"{ElapsedMilliseconds} ");

			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write($"{philosopherID} ");

			Console.ForegroundColor = GetColorForAction(action);
			Console.WriteLine(action);

			Console.ResetColor();
		}
	}

	private static ConsoleColor GetColorForAction(string action)
	{
		if (action.Contains("died")) return ConsoleColor.Red;
		if (action.Contains("eating")) return ConsoleColor.Green;
		if (action.Contains("sleeping")) return ConsoleColor.Blue;
		if (action.Contains("thinking")) return ConsoleColor.Magenta;
		if (action.Contains("fork")) return ConsoleColor.White;
		return ConsoleColor.Gray;
	}
}
