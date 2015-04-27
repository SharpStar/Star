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
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Announcers;
using FluentMigrator.Runner.Initialization;
using StarLib.Database.Mono;

namespace StarLib.Database
{
	public class DbMigrator
	{
		private class MigrationOptions : IMigrationProcessorOptions
		{
			public bool PreviewOnly { get; set; }
			public int Timeout { get; set; }
			public string ProviderSwitches { get; set; }
		}

		public string DatabaseFile { get; set; }

		public DbMigrator(string dbFile)
		{
			if (dbFile == null)
				throw new ArgumentNullException("dbFile");

			DatabaseFile = dbFile;
		}

		public virtual MigrationRunner GetRunner()
		{
			string connString = string.Format("Data Source={0}", DatabaseFile);

			var options = new MigrationOptions { PreviewOnly = false, Timeout = 0 };
			//var factory = new FluentMigrator.Runner.Processors.SQLite.SQLiteProcessorFactory();
			var factory = new MonoSQLiteProcessorFactory();
			var assembly = Assembly.GetExecutingAssembly();
			
			var announcer = new NullAnnouncer();
			var migrationContext = new RunnerContext(announcer);
			var processor = factory.Create(connString, announcer, options);
			var runner = new MigrationRunner(assembly, migrationContext, processor);

			return runner;
		}

		public void MigrateUp()
		{
			var runner = GetRunner();

			runner.MigrateUp(true);
			runner.Processor.Dispose();
		}
	}
}
