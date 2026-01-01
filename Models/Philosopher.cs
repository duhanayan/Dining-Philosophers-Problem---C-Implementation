using System;
using System.Threading;
using DiningPhilosophers.Utils;

namespace DiningPhilosophers.Models;

public class Philosopher
{
	public int ID { get; }

	private readonly Fork _leftFork;
	private readonly Fork _rightFork;

	private readonly int _timeToDie;
	private readonly int _timeToEat;
	private readonly int _timeToSleep;
	private readonly int? _mustEatCount;

	private long _lastMealTime;
	private int _mealsEaten;

	private readonly CancellationTokenSource _cts = new CancellationTokenSource();
	private static readonly ManualResetEventSlim _startSignal = new ManualResetEventSlim(false);
	
	private Thread? _dineThread;
	private Thread? _monitorThread;

	public Philosopher(int id, Fork leftFork, Fork rightFork, int timeToDie, int timeToEat, int timeToSleep, int? mustEatCount)
	{
		ID = id;

		_leftFork = leftFork;
		_rightFork = rightFork;

		_timeToDie = timeToDie;
		_timeToEat = timeToEat;
		_timeToSleep = timeToSleep;

		_mustEatCount = mustEatCount;
		_lastMealTime = 0;
	}

	public void Start()
	{
		_monitorThread = new Thread(() => 
		{
			_startSignal.Wait();
			_lastMealTime = ConsoleHelper.ElapsedMilliseconds;
			MonitorDeath();
		});
		_monitorThread.IsBackground = true;
		_monitorThread.Start();
		
		_dineThread = new Thread(() => 
		{
			_startSignal.Wait();
			Dine();
		});
		_dineThread.IsBackground = true;
		_dineThread.Start();
	}

	public static void SignalStart()
	{
		_startSignal.Set();
	}

	private void Dine()
	{
		// Staggering is key for survival, especially in odd counts.
		// Even philosophers wait for one full eating cycle.
		if (ID % 2 == 0)
			SafeDelay(_timeToEat);
		// Special case for the last philosopher in odd counts to avoid immediate contention with Philo 1.
		else if (Program.PhilosopherCount % 2 != 0 && ID == Program.PhilosopherCount)
			SafeDelay(_timeToEat / 2);

		while (!_cts.Token.IsCancellationRequested)
		{
			Eat();

			if (IsFinished())
				break;

			ConsoleHelper.Write(ID, "is sleeping");
			SafeDelay(_timeToSleep);

			ConsoleHelper.Write(ID, "is thinking");
			
			// Mandatory think time to ensure fairness among neighbors.
			int thinkTime = 0;
			if (Program.PhilosopherCount % 2 != 0)
			{
				// Standard formula for odd counts: enough time for two neighbor turns.
				thinkTime = _timeToEat * 2 - _timeToSleep;
			}
			else
			{
				// For even counts, a minimal yield or tiny delay is enough.
				if (_timeToDie - (_timeToEat + _timeToSleep) < 50)
					thinkTime = 1;
			}

			if (thinkTime < 0) thinkTime = 0;
			
			// Caps think time to avoid accidental death. 
			// We want a safety buffer of at least 10% of timeToDie.
			int remainingLifeBuffer = _timeToDie - _timeToEat - _timeToSleep;
			int maxThink = remainingLifeBuffer - (int)(_timeToDie * 0.1); 
			
			if (thinkTime > maxThink && maxThink > 0) thinkTime = maxThink;
			
			if (thinkTime > 0)
				SafeDelay(thinkTime);
			else if (Program.PhilosopherCount > 50)
				Thread.Sleep(1); // Force a context switch under high load
			else
				Thread.Sleep(0);
		}
	}

	private void Eat()
	{
		// Asymmetrical solution to prevent deadlock and starvation:
		// Even philosophers: left fork first, then right
		// Odd philosophers: right fork first, then left
		Fork firstFork, secondFork;
		string firstSide, secondSide;
		if (ID % 2 == 0)
		{
			firstFork = _leftFork;
			secondFork = _rightFork;
			firstSide = "left";
			secondSide = "right";
		}
		else
		{
			firstFork = _rightFork;
			secondFork = _leftFork;
			firstSide = "right";
			secondSide = "left";
		}

		lock (firstFork.LockObject)
		{
			ConsoleHelper.Write(ID, $"has taken {firstSide} fork ({firstFork.ID})");
			if (firstFork == secondFork)
			{
				SafeDelay(_timeToDie + 10);
				return;
			}

			lock (secondFork.LockObject)
			{
				ConsoleHelper.Write(ID, $"has taken {secondSide} fork ({secondFork.ID})");
				_lastMealTime = ConsoleHelper.ElapsedMilliseconds;
				ConsoleHelper.Write(ID, $"is eating (meal #{_mealsEaten + 1})");

				SafeDelay(_timeToEat);
				++_mealsEaten;
			}
		}
	}


	private void MonitorDeath()
	{
		while (!_cts.Token.IsCancellationRequested)
		{
			if (IsFinished())
				break;

			long current = ConsoleHelper.ElapsedMilliseconds;
			if (current - _lastMealTime > _timeToDie)
			{
				ConsoleHelper.Write(ID, "died");
				_cts.Cancel();
				break;
			}
			Thread.Sleep(1);
		}
	}

	private void SafeDelay(int ms)
	{
		long start = ConsoleHelper.ElapsedMilliseconds;
		while (ConsoleHelper.ElapsedMilliseconds - start < ms)
		{
			if (_cts.Token.IsCancellationRequested) break;
			
			long remaining = ms - (ConsoleHelper.ElapsedMilliseconds - start);
			if (remaining > 5)
				Thread.Sleep(1);
			else
				Thread.SpinWait(10);
		}
	}

	public void Stop()
	{
		_cts.Cancel();
	}

	public bool IsFinished()
	{
		if (_mustEatCount.HasValue && _mealsEaten >= _mustEatCount.Value)
			return true;
		return false;
	}
}
