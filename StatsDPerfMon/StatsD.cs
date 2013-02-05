using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace StatsDPerfMon
{
	internal interface IStatsD : IDisposable
	{
		bool Gauge(string key, long value, double sampleRate = 1);
		IDisposable Timing(string key, double sampleRate = 1);
		bool Timing(string key, long value, double sampleRate = 1);
		bool Decrement(string key, int magnitude = -1, double sampleRate = 1);
		bool Decrement(params string[] keys);
		bool Decrement(int magnitude, params string[] keys);
		bool Decrement(int magnitude, double sampleRate, params string[] keys);
		bool Increment(string key, int magnitude = 1, double sampleRate = 1);
		bool Increment(int magnitude, double sampleRate, params string[] keys);
	}

	internal class StatsD : IStatsD
	{
		private readonly UdpClient udpClient;
		private readonly Random random = new Random();
		private string prefix;

		public StatsD(string host, int port, string prefix = "")
		{
			udpClient = new UdpClient(host, port);
			this.prefix = prefix;
		}

		public bool Gauge(string key, long value, double sampleRate = 1)
		{
			return Send(sampleRate, String.Format("{0}:{1:d}|g", key, value));
		}

		public IDisposable Timing(string key, double sampleRate = 1)
		{
			var watch = Stopwatch.StartNew();
			return new DisposableAction(() =>
				{
					watch.Stop();
					Timing(key, watch.ElapsedMilliseconds, sampleRate);
				});
		}

		public bool Timing(string key, long value, double sampleRate = 1)
		{
			return Send(sampleRate, String.Format("{0}:{1:d}|ms", key, value));
		}

		public bool Decrement(string key, int magnitude = -1, double sampleRate = 1)
		{
			magnitude = magnitude < 0 ? magnitude : -magnitude;
			return Increment(key, magnitude, sampleRate);
		}

		public bool Decrement(params string[] keys)
		{
			return Increment(-1, 1.0, keys);
		}

		public bool Decrement(int magnitude, params string[] keys)
		{
			magnitude = magnitude < 0 ? magnitude : -magnitude;
			return Increment(magnitude, 1.0, keys);
		}

		public bool Decrement(int magnitude, double sampleRate, params string[] keys)
		{
			magnitude = magnitude < 0 ? magnitude : -magnitude;
			return Increment(magnitude, sampleRate, keys);
		}

		public bool Increment(string key, int magnitude = 1, double sampleRate = 1)
		{
			var stat = String.Format("{0}:{1}|c", key, magnitude);
			return Send(stat, sampleRate);
		}

		public bool Increment(int magnitude, double sampleRate, params string[] keys)
		{
			return Send(sampleRate, keys.Select(key => String.Format("{0}:{1}|c", key, magnitude)).ToArray());
		}

		protected bool Send(String stat, double sampleRate)
		{
			return Send(sampleRate, stat);
		}

		protected bool Send(double sampleRate, params string[] stats)
		{
			var sentSomething = false;
			if (sampleRate < 1.0)
			{
				foreach (var stat in stats)
				{
					if (random.NextDouble() <= sampleRate)
					{
						var statFormatted = String.Format("{0}|@{1:f}", stat, sampleRate);
						DoSend(statFormatted);
						sentSomething = true;
					}
				}
			}
			else
			{
				foreach (var stat in stats)
				{
					DoSend(stat);
					sentSomething = true;
				}
			}

			return sentSomething;
		}

		protected void DoSend(string stat)
		{
			var data = Encoding.Default.GetBytes(prefix + stat + "\n");

			udpClient.Send(data, data.Length);
		}

		public void Dispose()
		{
			try
			{
				if (udpClient != null)
				{
					udpClient.Close();
				}
			}
			catch
			{
			}
		}

		private class DisposableAction : IDisposable
		{
			private readonly Action actionOnDispose;

			public DisposableAction(Action actionOnDispose)
			{
				this.actionOnDispose = actionOnDispose;
			}

			public void Dispose()
			{
				actionOnDispose();
			}
		}
	}
}