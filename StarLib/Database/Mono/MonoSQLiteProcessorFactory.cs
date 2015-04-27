using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Generators.SQLite;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.SQLite;

namespace StarLib.Database.Mono
{
	public class MonoSQLiteProcessorFactory : MigrationProcessorFactory
	{
		public override IMigrationProcessor Create(string connectionString, IAnnouncer announcer, IMigrationProcessorOptions options)
		{
			var factory = new MonoSqliteDbFactory();
			var connection = factory.CreateConnection(connectionString);
			return new SQLiteProcessor(connection, new SQLiteGenerator(), announcer, options, factory);
		}
	}
}
