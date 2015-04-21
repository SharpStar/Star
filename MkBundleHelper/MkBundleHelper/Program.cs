using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace MkBundleHelper
{
	class Program
	{
		static void Main(string[] args)
		{

			Console.WriteLine("Finding assemblies...");

			string dir = args[0];
			string file = args[1];

			AppDomainSetup domaininfo = new AppDomainSetup();
			domaininfo.ApplicationBase = dir;
			Evidence adevidence = AppDomain.CurrentDomain.Evidence;
			AppDomain domain = AppDomain.CreateDomain("TempDomain", adevidence, domaininfo);

			var dllsToInclude = new List<string>();
			foreach (string dll in Directory.GetFiles(dir, "*.dll"))
			{
				try
				{
					Console.WriteLine("Loading assembly {0}", Path.GetFileName(dll));

					Type type = typeof(Proxy);
					var instance = (Proxy)domain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);

					Assembly assm = instance.GetAssembly(dll);

					if (assm == null)
						continue;

					foreach (AssemblyName aName in assm.GetReferencedAssemblies())
					{
						Assembly assm2 = Assembly.Load(aName);

						string dll2 = assm2.ManifestModule.FullyQualifiedName;

						dllsToInclude.Add(dll2);

						Console.WriteLine("Added assembly {0}", assm2.FullName);
					}

					dllsToInclude.Add(dll);

					Console.WriteLine("Added assembly {0}", assm.FullName);
				}
				catch
				{
				}
			}

			AppDomain.Unload(domain);

			StringBuilder sb = new StringBuilder("mkbundle " + Path.Combine(dir, file) + " ");

			//var dlls = (from d in dllsToInclude
			//			group d by Path.GetFileName(d) into g
			//			select new
			//			{
			//				Dlls = g
			//			}).Select(x => x.Dlls.Where(p => p.Contains("/mono/gac") || p.Contains("mscorlib"))).SelectMany(p => p.Select(Path.GetFileName).ToList());

			var dlls = (from d in dllsToInclude
						group d by Path.GetFileName(d) into g
						select new
						{
							Dlls = g
						}).Select(p => p.Dlls.First());



			foreach (string dll in dlls)
			{
				sb.Append(dll + " ");
			}

			sb.AppendFormat("--static --machine-config {0} -o {1}", Path.Combine(dir, "machine.config"),
				Path.Combine(dir, Path.GetFileNameWithoutExtension(file)));

			Console.WriteLine("Writing to file...");

			File.WriteAllText(Path.Combine(dir, "mkbundle.txt"), sb.ToString());
		}
	}
}