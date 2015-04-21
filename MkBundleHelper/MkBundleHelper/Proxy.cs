using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MkBundleHelper
{
	public class Proxy : MarshalByRefObject
	{
		public Assembly GetAssembly(string assemblyPath)
		{
			try
			{
				return Assembly.LoadFile(assemblyPath);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
