using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Topshelf;

namespace StatsDPerfMon.ExampleHost
{
	class Program
	{
		static void Main()
		{
			HostFactory.Run(x =>
			{
				x.UseNLog();

				x.Service<PerfCounterService>(s =>
				{
					var statsDHost = ConfigurationManager.AppSettings["StatsDHost"];

					s.ConstructUsing(name => new PerfCounterService(GetCounterDefinitions, statsDHost));
					s.WhenStarted(service => service.Start());
					s.WhenStopped(service => service.Stop());
				});

				x.RunAsLocalSystem();
				x.EnableServiceRecovery(sr => sr.RestartService(1));

				x.SetDisplayName("StatsDPerfMon");
				x.SetServiceName("StatsDPerfMon");
			});
		}
		
		private static IEnumerable<CounterDefinition> GetCounterDefinitions()
		{
			var definitions = new List<CounterDefinition>
				{
					new CounterDefinition
						{
							StatName = "cpu.usage",
							CategoryName = "Processor",
							CounterName = "% Processor Time",
							InstanceName = "_Total"
						},
					new CounterDefinition
						{
							StatName = "cpu.queuelength",
							CategoryName = "System",
							CounterName = "Processor Queue Length",
							InstanceName = ""
						},
					new CounterDefinition
						{
							StatName = "memory.available.MBytes",
							CategoryName = "Memory",
							CounterName = "Available MBytes"
						},
					new CounterDefinition
						{
							StatName = "memory.pages.persec",
							CategoryName = "Memory",
							CounterName = "Pages/sec"
						},
					new CounterDefinition
						{
							CategoryName = "ASP.Net v4.0.30319",
							CounterName = "Requests Queued",
							StatName = "aspnet.requests.queued"
						},
					new CounterDefinition
						{
							CategoryName = "ASP.Net Apps v4.0.30319",
							CounterName = "Requests/Sec",
							InstanceName = "__Total__",
							StatName = "aspnet.requests.persec"
						}
				};

			definitions.AddRange(
				DriveInfo.GetDrives()
					.Where(drive => drive.DriveType == DriveType.Fixed)
					.Select(drive => drive.RootDirectory.FullName)
					.Select(drive => drive.TrimEnd('\\'))
					.SelectMany(
						drive => new[]
							{
								new CounterDefinition
									{
										StatName = string.Format("disk.{0}.percentfreespace", drive.TrimEnd(':')),
										CategoryName = "LogicalDisk",
										CounterName = "% Free Space",
										InstanceName = drive
									},
								new CounterDefinition
									{
										StatName = string.Format("disk.{0}.queuelength", drive.TrimEnd(':')),
										CategoryName = "LogicalDisk",
										CounterName = "Current Disk Queue Length",
										InstanceName = drive
									},
								new CounterDefinition
									{
										StatName = string.Format("disk.{0}.averagequeuelength", drive.TrimEnd(':')),
										CategoryName = "LogicalDisk",
										CounterName = "Avg. Disk Queue Length",
										InstanceName = drive
									}
							}
					));

			return definitions;
		}
	}
}
