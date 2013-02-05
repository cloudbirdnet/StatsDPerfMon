using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace StatsDPerfMon
{
	public class PerfCounterService : ScheduledServiceBase
	{
		private readonly Lazy<Dictionary<string, PerformanceCounter>> counters;
		private readonly IStatsD statsD;

		public PerfCounterService(Func<IEnumerable<CounterDefinition>> getDefinitions, string statsDHost, int statsDPort = 8125, string statsPrefix = null)
		{
			defaultTickTimeSpan = TimeSpan.FromSeconds(5);
			initialDelay = TimeSpan.FromSeconds(10);

			statsPrefix = statsPrefix ?? string.Format("monitor.{0}.", Environment.MachineName);

			statsD = new StatsD(
				host: statsDHost,
				port: statsDPort,
				prefix: statsPrefix
				);

			counters = new Lazy<Dictionary<string, PerformanceCounter>>(() => CreateCounters(getDefinitions()));
		}

		public override void Stop()
		{
			base.Stop();

			// Guages in statsD retain their last value so set everything to 0 when stopping so we can see there is no data
			ZeroAllStats();
		}

		private void ZeroAllStats()
		{
			foreach (var keyValuePair in counters.Value)
			{
				var statsName = keyValuePair.Key;
				statsD.Gauge(statsName, 0);
			}
		}

		protected override void DoWork()
		{
			foreach (var keyValuePair in counters.Value)
			{
				var counter = keyValuePair.Value;
				var statsName = keyValuePair.Key;
				statsD.Gauge(statsName, (long)counter.NextValue());
			}
		}

		private Dictionary<string, PerformanceCounter> CreateCounters(IEnumerable<CounterDefinition> definitions)
		{
			return definitions.ToDictionary(
				definition => definition.StatName,
				definition => new PerformanceCounter(definition.CategoryName, definition.CounterName, definition.InstanceName));
		}
	}
}