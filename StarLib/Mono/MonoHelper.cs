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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StarLib.Mono
{
	/// <summary>
	/// Helper class for retrieving information on Mono
	/// </summary>
	public static class MonoHelper
	{
		public static bool IsRunningOnMono
		{
			get
			{
				try
				{
					return Type.GetType("Mono.Runtime") != null;
				}
				catch
				{
					return false;
				}
			}
		}

		public static string GetMonoVersion()
		{
			try
			{
				Type type = Type.GetType("Mono.Runtime");
				if (type != null)
				{
					MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
					if (displayName != null)
						return displayName.Invoke(null, null) as string;
				}
			}
			catch
			{
			}

			return string.Empty;
		}
	}
}
