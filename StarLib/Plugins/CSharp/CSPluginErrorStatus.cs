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
using System.Text;
using System.Threading.Tasks;
using Mono.Addins;
using StarLib.Logging;

namespace StarLib.Plugins.CSharp
{
	public class CSPluginErrorStatus : IProgressStatus
	{
		private static readonly StarLog _logger = StarLog.DefaultLogger;

		private bool _cancelled;

		public void SetMessage(string msg)
		{
		}

		public void SetProgress(double progress)
		{
		}

		public void Log(string msg)
		{
		}

		public void ReportWarning(string message)
		{
			_logger.Warn(message);
		}

		public void ReportError(string message, Exception exception)
		{
			_logger.Error(message);
			_logger.Error(exception.ToString());
		}

		public void Cancel()
		{
			_cancelled = true;
		}

		public int LogLevel
		{
			get { return 2; }
		}

		public bool IsCanceled
		{
			get { return _cancelled; }
		}
	}
}
