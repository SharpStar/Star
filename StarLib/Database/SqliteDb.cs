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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using StarLib.Database.Models;
using StarLib.Database.Mono;
using StarLib.Mono;
using Config = NHibernate.Cfg.Configuration;

namespace StarLib.Database
{
    public abstract class SqliteDb : IDisposable
    {
        public const string DbDirectory = "databases";

        public string FileName { get; private set; }

        public ISessionFactory Factory { get; protected set; }

        protected DbMigrator Migrator { get; private set; }
        
        private Config _config;

        protected SqliteDb(string fileName)
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
            string dir = new FileInfo(FileName).DirectoryName;

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            Factory = CreateFactory();
        }

        public virtual ISession CreateSession()
        {
            return Factory.OpenSession();
        }

        protected virtual ISessionFactory CreateFactory()
        {
            var config = Fluently.Configure();

            if (!File.Exists(FileName))
            {
                config = config.ExposeConfiguration(p => new SchemaExport(p).Execute(false, true, false));
            }

            if (MonoHelper.IsRunningOnMono)
            {
                config = config.Database(MonoSQLiteConfiguration.Standard.UsingFile(FileName));
            }
            else
            {
                config = config.Database(SQLiteConfiguration.Standard.UsingFile(FileName));
            }

            _config = CreateConfig(config).BuildConfiguration();

            return _config.BuildSessionFactory();
        }

        protected virtual FluentConfiguration CreateConfig(FluentConfiguration initial)
        {
            return initial.Mappings(p => p.FluentMappings.AddFromAssembly(Assembly.GetExecutingAssembly()));
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

        ~SqliteDb()
        {
            Dispose(false);
        }
        #endregion
    }
}
