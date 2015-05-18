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
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using SQLite.Net;
using SQLite.Net.Async;
using SQLite.Net.Platform.Generic;

namespace StarLib.Database
{
	public abstract class SqliteAsyncDb : IDisposable
	{
        public const string DbDirectory = "databases";

        public string FileName { get; private set; }

        public SQLiteAsyncConnection Connection { get; private set; }

        protected DbMigrator Migrator { get; private set; }

        public abstract Task CreateTablesAsync();

        protected SqliteAsyncDb(string fileName)
		{
			FileName = fileName;
            Migrator = new DbMigrator(fileName);
            Init();
        }

        public virtual void Migrate()
        {
            Migrator.MigrateUp();
        }

        protected void Init()
        {
            Connection = CreateAsyncConnection();
        }

		public virtual SQLiteAsyncConnection CreateAsyncConnection()
		{
            string dir = Path.GetDirectoryName(FileName);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var connStr = new SQLiteConnectionString(FileName, false);

            return new SQLiteAsyncConnection(() => new SQLiteConnectionWithLock(new SQLitePlatformGeneric(), connStr));
        }

		#region Disposal
		public void Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
			}
		}

		~SqliteAsyncDb()
		{
			Dispose(false);
		}
		#endregion
	}
}
