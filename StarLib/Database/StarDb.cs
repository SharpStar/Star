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
using System.Threading.Tasks;
using NHibernate.Linq;
using StarLib.Database.Models;
using StarLib.Security;

namespace StarLib.Database
{
    public sealed class StarDb : SqliteDb
    {
        public const string StarDbFileName = "star.db";

        private static readonly string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string DbFileLocation = Path.Combine(AssemblyDir, DbDirectory, StarDbFileName);

        public StarDb() : base(DbFileLocation)
        {
        }

        public Character GetCharacter(int id)
        {
            using (var session = CreateSession())
            {
                return session.Get<Character>(id);
            }
        }

        public List<Character> GetAccountCharacters(int accountId)
        {
            using (var session = CreateSession())
            {
                return session.Query<Character>().Fetch(p => p.Account).Where(p => p.Account.Id == accountId).ToList();
            }
        }

        public Character GetCharacterByUuid(string uuid)
        {
            using (var session = CreateSession())
            {
                return session.Query<Character>().SingleOrDefault(p => p.Uuid == uuid);
            }
        }

        public void SaveCharacter(Character character)
        {
            using (var session = CreateSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    session.SaveOrUpdate(character);

                    transaction.Commit();
                }
            }
        }

        public bool CreateAccount(string username, string password, int? groupId = null)
        {
            using (var session = CreateSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    if (session.Query<Account>().Count(p => p.Username.ToUpper() == username.ToUpper()) > 0)
                        return false;

                    string salt = StarSecurity.GenerateSecureString();
                    string hash = StarSecurity.GenerateHash(username, password, Encoding.UTF8.GetBytes(salt));

                    Account account = new Account
                    {
                        Username = username,
                        InternalId = Guid.NewGuid(),
                        PasswordSalt = salt,
                        PasswordHash = hash,
                        Group = groupId.HasValue ? session.Get<Group>(groupId.Value) : null
                    };

                    session.Save(account);

                    transaction.Commit();
                }
            }

            return true;
        }

        public Account GetAccount(int id)
        {
            using (var session = CreateSession())
            {
                return session.Get<Account>(id);
            }
        }

        public Account GetAccountByUsername(string username)
        {
            using (var session = CreateSession())
            {
                return session.Query<Account>().SingleOrDefault(p => p.Username.ToUpper() == username.ToUpper());
            }
        }

        public void SaveAccount(Account account)
        {
            using (var session = CreateSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    session.SaveOrUpdate(account);

                    transaction.Commit();
                }
            }
        }

        public bool AddBan(string playerName, string playerIp, int? accountId, DateTime expirationTime, string reason = "")
        {
            using (var session = CreateSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    if (accountId.HasValue && session.Query<Ban>().Fetch(p => p.Account).Any(p => p.Account.Id == accountId))
                        return false;

                    if (session.Query<Ban>().Count(p => p.IpAddress == playerIp) > 0)
                        return false;

                    Ban ban = new Ban
                    {
                        PlayerName = playerName,
                        Account = accountId.HasValue ? session.Get<Account>(accountId.Value) : null,
                        ExpirationTime = expirationTime,
                        Reason = reason,
                        IpAddress = playerIp,
                        Active = true
                    };

                    session.Save(ban);
                    transaction.Commit();
                }
            }

            return true;
        }

        public void SaveBan(Ban ban)
        {
            using (var session = CreateSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    session.SaveOrUpdate(ban);

                    transaction.Commit();
                }
            }
        }

        public bool RemoveBanByAccount(int accountId)
        {
            Ban ban = GetBanByAccount(accountId);

            if (ban == null)
                return false;

            using (var session = CreateSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    session.Delete(ban);

                    transaction.Commit();
                }
            }

            return true;
        }

        public bool RemoveBanByIp(string ip)
        {
            Ban ban = GetBanByIp(ip);

            if (ban == null)
                return false;

            using (var session = CreateSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    session.Delete(ban);

                    transaction.Commit();
                }
            }

            return true;
        }

        public Ban GetBanByAccount(int accountId)
        {
            using (var session = CreateSession())
            {
                return session.Query<Ban>().Fetch(p => p.Account).SingleOrDefault(p => p.Account.Id == accountId);
            }
        }

        public Ban GetBanByIp(string ip)
        {
            using (var session = CreateSession())
            {
                return session.Query<Ban>().SingleOrDefault(p => p.IpAddress == ip);
            }
        }

        public bool CreateGroup(string name, bool isDefault = false)
        {
            using (var session = CreateSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    if (session.Query<Group>().Any(p => p.Name.ToUpper() == name.ToUpper()))
                        return false;

                    //if (await Connection.Table<Group>().Where(p => p.Name.ToUpper() == name.ToUpper()).CountAsync() > 0)
                    //    return false;

                    if (isDefault)
                    {
                        var groups = session.Query<Group>().Where(p => p.IsDefault);

                        foreach (Group group in groups)
                        {
                            group.IsDefault = false;

                            session.Update(group);
                        }
                    }

                    Group newGroup = new Group
                    {
                        Name = name,
                        IsDefault = isDefault
                    };

                    session.Save(newGroup);

                    transaction.Commit();
                }
            }

            return true;
        }

        public Group GetGroup(int id)
        {
            using (var session = CreateSession())
            {
                return session.Get<Group>(id);
            }
        }

        public Group GetGroupByName(string name)
        {
            using (var session = CreateSession())
            {
                return session.Query<Group>().SingleOrDefault(p => p.Name.ToUpper() == name.ToUpper());
            }
        }

        public List<Group> GetGroupsWithPermission(string permission)
        {
            using (var session = CreateSession())
            {
                var perms = session.Query<Permission>().Fetch(p => p.Group).Where(p => p.Group != null && p.Name.ToUpper() == permission.ToUpper());

                return perms.Select(p => p.Group).ToList();
            }
        }

        public void SaveGroup(Group group)
        {
            using (var session = CreateSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    session.SaveOrUpdate(group);

                    transaction.Commit();
                }
            }
        }

        public void AddEvent(string text, string[] eventTypes, int? accountId = null)
        {
            using (var session = CreateSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    EventHistory evt = new EventHistory
                    {
                        Text = text,
                        Account = accountId.HasValue ? session.Get<Account>(accountId.Value) : null,
                        EventTypes = new List<EventType>()
                    };

                    foreach (string type in eventTypes)
                    {
                        EventType evtType = session.Query<EventType>().SingleOrDefault(p => p.Name.ToUpper() == type.ToUpper());

                        if (evtType != null)
                        {
                            evt.EventTypes.Add(evtType);
                        }
                        else
                        {
                            evt.EventTypes.Add(new EventType { Name = type });
                        }
                    }

                    session.Save(evt);

                    transaction.Commit();
                }
            }
        }

        public void SaveEvent(EventHistory evt)
        {
            using (var session = CreateSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    session.SaveOrUpdate(evt);

                    transaction.Commit();
                }
            }
        }
    }
}
