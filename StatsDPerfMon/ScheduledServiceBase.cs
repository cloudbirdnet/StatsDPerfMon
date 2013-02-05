using System;
using System.Threading;
using NLog;

namespace StatsDPerfMon
{
	public abstract class ScheduledServiceBase
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private Timer timer;
		protected TimeSpan defaultTickTimeSpan;
		protected TimeSpan initialDelay;
		private TimeSpan nextTickTimeSpan;
		private readonly TimeSpan maxNextTickTimeSpan;

		protected ScheduledServiceBase()
		{
			defaultTickTimeSpan = TimeSpan.FromSeconds(5);
			initialDelay = TimeSpan.Zero;
			nextTickTimeSpan = defaultTickTimeSpan;
			maxNextTickTimeSpan = TimeSpan.FromSeconds(300);
		}

		public void Start()
		{
			Logger.Info("Starting service " + GetType().Name);

			try
			{
				timer = new Timer(state => Tick());
				ScheduleNextOccurrence(initialDelay);
			}
			catch (Exception ex)
			{
				Logger.ErrorException("Error starting service", ex);
				throw;
			}
		}

		private void Tick()
		{
			try
			{
				DoWork();
				nextTickTimeSpan = defaultTickTimeSpan;
			}
			catch (Exception ex)
			{
				Logger.ErrorException(GetType().Name + " service threw exception while working", ex);
				nextTickTimeSpan += nextTickTimeSpan;
				if (nextTickTimeSpan > maxNextTickTimeSpan)
					nextTickTimeSpan = maxNextTickTimeSpan;
			}
			finally
			{
				ScheduleNextOccurrence(nextTickTimeSpan);
			}
		}

		protected abstract void DoWork();

		public virtual void Stop()
		{
			Logger.Info("Stopping service " + GetType().Name);
			timer.Dispose();
		}

		public void ScheduleNextOccurrence(TimeSpan next)
		{
			var disablePeriodicSignalling = TimeSpan.FromMilliseconds(-1);
			timer.Change(next, disablePeriodicSignalling);
		}
	}
}