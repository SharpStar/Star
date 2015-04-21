// SharpStar. A Starbound wrapper.
// Copyright (C) 2015 Mitchell Kutchuk
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Addins.Setup;
using RazorEngine;
using RazorEngine.Templating;

namespace Star.RepositoryBuilder
{
	class Program
	{
		private const string MaUtil = "mautil.exe";

		static void Main(string[] args)
		{
			ProgramOptions options = new ProgramOptions();

			if (CommandLine.Parser.Default.ParseArguments(args, options))
			{
				if (!Directory.Exists(options.OutputDirectory))
					Directory.CreateDirectory(options.OutputDirectory);

				if (!string.IsNullOrEmpty(options.PluginDir) && string.IsNullOrEmpty(options.AddinRepoUrl))
				{
					if (!File.Exists(MaUtil))
					{
						Console.WriteLine("Could not find mautil, exiting!");

						return;
					}

					if (!Directory.Exists(options.PluginDir))
					{
						Console.WriteLine("Invalid directory!");

						return;
					}

					DirectoryInfo dInfo = new DirectoryInfo(options.PluginDir);

					foreach (FileInfo fInfo in dInfo.GetFiles("*.dll"))
					{
						string pack = string.Format("pack {0}", fInfo.FullName);

						Process proc = CreateMaUtilProc(pack);

						Console.WriteLine("Packing {0}", fInfo.Name);

						proc.Start();
						proc.BeginOutputReadLine();
						proc.WaitForExit();
						proc.Dispose();

						Console.WriteLine("Finished packing {0}", fInfo.Name);
					}

					DirectoryInfo thisDir = new DirectoryInfo(".");

					foreach (FileInfo fInfo in thisDir.GetFiles())
					{
						string newPath = Path.Combine(options.OutputDirectory, fInfo.Name);

						Console.WriteLine("Moving {0} to {1}", fInfo.FullName, newPath);

						File.Move(fInfo.FullName, newPath);
					}

					string build = string.Format("rep-build {0}", options.OutputDirectory);

					Console.WriteLine("Building repository...");

					Process buildProc = CreateMaUtilProc(build);
					buildProc.Start();
					buildProc.BeginOutputReadLine();
					buildProc.WaitForExit();
					buildProc.Dispose();

					Console.WriteLine("Finished building repository!");
				}
				else if (!string.IsNullOrEmpty(options.AddinRepoUrl))
				{
					var setupService = new SetupService();

					if (!setupService.Repositories.ContainsRepository(options.AddinRepoUrl))
						setupService.Repositories.RegisterRepository(null, options.AddinRepoUrl, false);

					setupService.Repositories.UpdateAllRepositories(null);

					AddinRepositoryEntry[] addins = setupService.Repositories.GetAvailableAddins();

					var groupedAddins = (from p in addins
										 group p by p.Addin.Name into g
										 select g);

					var addinEntries = new List<AddinEntry>();

					foreach (var addin in groupedAddins)
					{
						var entries = addin.Select(p => p.Addin).ToList();

						if (!entries.Any())
							continue;

						AddinHeader maxEntry = entries.First();

						for (int i = 0; i < entries.Count; i++)
						{
							if (maxEntry.CompareVersionTo(entries[i]) > 0)
								maxEntry = entries[i];
						}

						addinEntries.Add(new AddinEntry { Name = entries[0].Name, LatestVersion = maxEntry.Version });
					}

					string html = Engine.Razor.RunCompile("plugins", typeof(IEnumerable<AddinEntry>), addinEntries.AsEnumerable());

					File.WriteAllText("index.html", html);
				}

				Console.WriteLine("All done!");
			}
		}

		private static Process CreateMaUtilProc(string arguments)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo(MaUtil, arguments);
			Process proc = new Process();
			proc.StartInfo = startInfo;
			proc.EnableRaisingEvents = true;
			proc.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
			
			return proc;
		}

	}
}
