using FluentMigrator.Runner.Processors;

namespace StarLib.Database.Mono
{
	public class MonoSqliteDbFactory : ReflectionBasedDbFactory
	{
		public MonoSqliteDbFactory() : base("Mono.Data.Sqlite", "Mono.Data.Sqlite.SqliteFactory")
        {
		}
	}
}
