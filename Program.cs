using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DiningPhilosophers.Models;
using DiningPhilosophers.Utils;

class Program
{
	public static int PhilosopherCount { get; private set; }

	static void Main(string[] args)
	{
		if (args.Length < 4 || args.Length > 5)
		{
			Console.WriteLine("Usage: number_of_philosophers time_to_die time_to_eat time_to_sleep [number_of_times_each_philosopher_must_eat]");
			return;
		}

		if (!int.TryParse(args[0], out int philCount) || philCount <= 0 ||
			!int.TryParse(args[1], out int timeToDie) || timeToDie <= 0 ||
			!int.TryParse(args[2], out int timeToEat) || timeToEat <= 0 ||
			!int.TryParse(args[3], out int timeToSleep) || timeToSleep <= 0)
		{
			Console.WriteLine("Invalid arguments. All values must be positive integers.");
			return;
		}

		PhilosopherCount = philCount;

		int? mustEatCount = null;
		if (args.Length == 5)
		{
			if (int.TryParse(args[4], out int mec) && mec > 0)
				mustEatCount = mec;
			else
			{
				Console.WriteLine("Invalid mandatory meal count. It must be a positive integer.");
				return;
			}
		}

		Fork[] forks = new Fork[philCount];
		for (int i = 0; i < philCount; i++)
			forks[i] = new Fork(i);

		Philosopher[] philosophers = new Philosopher[philCount];
		for (int i = 0; i < philCount; i++)
		{
			int leftIndex = i;
			int rightIndex = (i + 1) % philCount;
			philosophers[i] = new Philosopher(i + 1, forks[leftIndex], forks[rightIndex], timeToDie, timeToEat, timeToSleep, mustEatCount);
		}

		ConsoleHelper.StartTimer();

		foreach (var p in philosophers)
			p.Start();

		Philosopher.SignalStart();

		// Main thread monitors for completion (everyone eaten enough)
		while (!ConsoleHelper.ShouldStop)
		{
			// Check if everyone has eaten enough
			if (mustEatCount.HasValue && philosophers.All(p => p.IsFinished()))
				break;

			// If any philosopher died, the ConsoleHelper logic will stop output.
			// However, we might want to shut down the app.
			// In C# we can use a Shared state or just let the process die or exit.
			// For 42, the simulation ends when someone dies or all ate enough.
			
			// Checking for death via a flag would be cleaner.
			// Let's add a static flag for simulation end.
			
			Thread.Sleep(10);
		}

		// Cleanup
		foreach (var p in philosophers)
			p.Stop();
	}
}

