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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ServiceStack.OrmLite;
using StarLib.Database.Models;
using StarLib.Security;
using StarLib.Starbound;

namespace StarLib.Database
{
    public sealed class StarDb : SqliteDb
    {
        public const string StarDbFileName = "star.db";
        public const string DbDirectory = "databases";

        private static readonly string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string DbFileLocation = Path.Combine(AssemblyDir, DbDirectory, StarDbFileName);

        private readonly DbMigrator _migrator;

        public StarDb() : base(DbFileLocation)
        {
            _migrator = new DbMigrator(StarDbFileName);

            CreateTables();

#if !DEBUG
            Migrate();
#endif
        }

        public void Migrate()
        {
            _migrator.MigrateUp();
        }

        public void CreateTables()
        {
            using (var conn = CreateConnection())
            {
                conn.CreateTableIfNotExists<Account>();
                conn.CreateTableIfNotExists<Group>();
                conn.CreateTableIfNotExists<Permission>();
                conn.CreateTableIfNotExists<Character>();
                conn.CreateTableIfNotExists<Ban>();
                conn.CreateTableIfNotExists<EventHistory>();
                conn.CreateTableIfNotExists<EventType>();
            }
        }

        public Character GetCharacter(int id)
        {
            using (var conn = CreateConnection())
            {
                return conn.LoadSingleById<Character>(id);
            }
        }

        public Character GetCharacterByUuid(string uuid)
        {
            using (var conn = CreateConnection())
            {
                return conn.Single<Character>(new
                {
                    Uuid = uuid
                });
            }
        }

        public void SaveCharacter(Character character)
        {
            using (var conn = CreateConnection())
            {
                conn.Save(character);
            }
        }

        public bool CreateAccount(string username, string password, int? groupId = null)
        {
            using (var conn = CreateConnection())
            {
                if (conn.Count<Account>(p => p.Username.ToUpper() == username.ToUpper()) > 0)
                    return false;

                string salt = StarSecurity.GenerateSecureString();
                string hash = StarSecurity.GenerateHash(username, password, Encoding.UTF8.GetBytes(salt));

                Account account = new Account
                {
                    Username = username,
                    InternalId = Guid.NewGuid(),
                    PasswordSalt = salt,
                    PasswordHash = hash,
                    GroupId = groupId
                };

                conn.Save(account);
            }

            return true;
        }

        public Account GetAccount(int id)
        {
            using (var conn = CreateConnection())
            {
                return conn.LoadSingleById<Account>(id);
            }
        }

        public Account GetAccountByUsername(string username)
        {
            using (var conn = CreateConnection())
            {
                return conn.Single<Account>(new
                {
                    Username = username
                });
            }
        }

        public void SaveAccount(Account account)
        {
            using (var conn = CreateConnection())
            {
                conn.Save(account);
                conn.SaveAllReferences(account);
            }
        }

        public bool AddBan(string playerName, string playerIp, int? accountId, DateTime expirationTime, string reason = "")
        {
            using (var conn = CreateConnection())
            {
                if (accountId.HasValue && conn.Count<Ban>(p => p.AccountId == accountId) > 0)
                    return false;

                if (conn.Count<Ban>(p => p.IpAddress == playerIp) > 0)
                    return false;

                conn.Save(new Ban
                {
                    PlayerName = playerName,
                    AccountId = accountId,
                    ExpirationTime = expirationTime,
                    Reason = reason,
                    IpAddress = playerIp,
                    Active = true
                });
            }

            return true;
        }

        public void SaveBan(Ban ban)
        {
            using (var conn = CreateConnection())
            {
                conn.Save(ban);
            }
        }

        public bool RemoveBanByAccount(int accountId)
        {
            using (var conn = CreateConnection())
            {
                Ban ban = GetBanByAccount(accountId);

                if (ban == null)
                    return false;

                conn.DeleteById<Ban>(ban.Id);
            }

            return true;
        }

        public bool RemoveBanByIp(string ip)
        {
            using (var conn = CreateConnection())
            {
                Ban ban = GetBanByIp(ip);

                if (ban == null)
                    return false;

                conn.DeleteById<Ban>(ban.Id);
            }

            return true;
        }

        public Ban GetBanByAccount(int accountId)
        {
            using (var conn = CreateConnection())
            {
                return conn.Single<Ban>(new
                {
                    AccountId = accountId
                });
            }
        }

        public Ban GetBanByIp(string ip)
        {
            using (var conn = CreateConnection())
            {
                return conn.Single<Ban>(new
                {
                    IpAddress = ip
                });
            }
        }

        public bool CreateGroup(string name, bool isDefault = false)
        {
            using (var conn = CreateConnection())
            {
                if (conn.Count<Group>(p => p.Name.ToUpper() == name.ToUpper()) > 0)
                    return false;

                if (isDefault)
                {
                    var groups = conn.Where<Group>(new
                    {
                        IsDefault = true
                    });

                    groups.ForEach(p =>
                    {
                        p.IsDefault = false;

                        conn.Update(p);
                    });
                }

                conn.Save(new Group
                {
                    Name = name,
                    IsDefault = isDefault
                });
            }

            return true;
        }

        public Group GetGroup(int id)
        {
            using (var conn = CreateConnection())
            {
                return conn.SingleById<Group>(id);
            }
        }

        public Group GetGroupByName(string name)
        {
            using (var conn = CreateConnection())
            {
                return conn.Single<Group>(new
                {
                    Name = name
                });
            }
        }

        public IList<Group> GetGroupsWithPermission(string permission)
        {
            using (var conn = CreateConnection())
            {
                return conn.Select(conn.From<Group>().Where(p => Sql.In(p.Permissions.Select(x => x.Name), permission)));
            }
        }

        public void SaveGroup(Group group)
        {
            using (var conn = CreateConnection())
            {
                conn.Save(group);
            }
        }

        public void AddEvent(string text, string[] eventTypes, int? accountId = null)
        {
            using (var conn = CreateConnection())
            {
                EventHistory evt = new EventHistory
                {
                    Text = text,
                    AccountId = accountId,
                    EventTypes = new List<EventType>()
                };

                foreach (string type in eventTypes)
                {
                    EventType evtType = conn.Single<EventType>(new
                    {
                        Name = type
                    });

                    if (evtType != null)
                        evt.EventTypes.Add(evtType);
                    else
                        evt.EventTypes.Add(new EventType { Name = type });
                }

                conn.SaveAllReferences(evt);
            }
        }

        public void SaveEvent(EventHistory evt)
        {
            using (var conn = CreateConnection())
            {
                conn.Save(evt);
            }
        }
    }
}
