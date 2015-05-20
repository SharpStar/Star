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
using StarLib.Database.Models;
using StarLib.Security;
using SQLiteNetExtensionsAsync.Extensions;

namespace StarLib.Database
{
    public sealed class StarDb : SqliteAsyncDb
    {
        public const string StarDbFileName = "star.db";

        private static readonly string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string DbFileLocation = Path.Combine(AssemblyDir, DbDirectory, StarDbFileName);

        public StarDb() : base(DbFileLocation)
        {
        }

        public override Task CreateTablesAsync()
        {
#if !DEBUG
            Migrate();
#endif

            var tasks = new List<Task>();
            tasks.Add(Connection.CreateTableAsync<Account>());
            tasks.Add(Connection.CreateTableAsync<Ban>());
            tasks.Add(Connection.CreateTableAsync<Character>());
            tasks.Add(Connection.CreateTableAsync<EventHistory>());
            tasks.Add(Connection.CreateTableAsync<EventType>());
            tasks.Add(Connection.CreateTableAsync<EventTypeHistory>());
            tasks.Add(Connection.CreateTableAsync<Group>());
            tasks.Add(Connection.CreateTableAsync<Permission>());

            return Task.WhenAll(tasks);
        }

        public Task<Character> GetCharacterAsync(int id)
        {
            return Connection.GetAsync<Character>(id);
        }

        public Task<Character> GetCharacterByUuidAsync(string uuid)
        {
            return Connection.Table<Character>().Where(p => p.Uuid == uuid).FirstOrDefaultAsync();
        }

        public Task SaveCharacterAsync(Character character)
        {
            return Connection.InsertOrReplaceAsync(character);
        }

        public async Task<bool> CreateAccountAsync(string username, string password, int? groupId = null)
        {
            if (await Connection.Table<Account>().Where(p => p.Username.ToUpper() == username.ToUpper()).CountAsync() > 0)
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

            await Connection.InsertAsync(account);

            return true;
        }

        public Task<Account> GetAccountAsync(int id)
        {
            return Connection.GetWithChildrenAsync<Account>(id);
        }

        public Task<Account> GetAccountByUsernameAsync(string username)
        {
            return Connection.Table<Account>().Where(p => p.Username.ToUpper() == username.ToUpper()).FirstOrDefaultAsync();
        }

        public Task SaveAccountAsync(Account account)
        {
            return Connection.UpdateWithChildrenAsync(account);
        }

        public async Task<bool> AddBanAsync(string playerName, string playerIp, int? accountId, DateTime expirationTime, string reason = "")
        {
            if (accountId.HasValue && await Connection.Table<Account>().Where(p => p.Id == accountId).CountAsync() > 0)
                return false;

            if (await Connection.Table<Ban>().Where(p => p.IpAddress == playerIp).CountAsync() > 0)
                return false;

            await Connection.InsertAsync(new Ban
            {
                PlayerName = playerName,
                AccountId = accountId.HasValue ? accountId.Value : 0,
                ExpirationTime = expirationTime,
                Reason = reason,
                IpAddress = playerIp,
                Active = true
            });

            return true;
        }

        public Task SaveBanAsync(Ban ban)
        {
            return Connection.InsertOrReplaceAsync(ban);
        }

        public async Task<bool> RemoveBanByAccountAsync(int accountId)
        {
            Ban ban = await GetBanByAccountAsync(accountId);

            if (ban == null)
                return false;

            await Connection.DeleteAsync(ban);

            return true;
        }

        public async Task<bool> RemoveBanByIpAsync(string ip)
        {
            Ban ban = await GetBanByIpAsync(ip);

            if (ban == null)
                return false;

            await Connection.DeleteAsync(ban);

            return true;
        }

        public Task<Ban> GetBanByAccountAsync(int accountId)
        {
            return Connection.Table<Ban>().Where(p => p.AccountId == accountId).FirstOrDefaultAsync();
        }

        public Task<Ban> GetBanByIpAsync(string ip)
        {
            return Connection.Table<Ban>().Where(p => p.IpAddress == ip).FirstOrDefaultAsync();
        }

        public async Task<bool> CreateGroupAsync(string name, bool isDefault = false)
        {
            if (await Connection.Table<Group>().Where(p => p.Name.ToUpper() == name.ToUpper()).CountAsync() > 0)
                return false;

            if (isDefault)
            {
                var groups = await Connection.Table<Group>().Where(p => p.IsDefault).ToListAsync();

                foreach (Group group in groups)
                {
                    group.IsDefault = false;

                    await Connection.UpdateAsync(group);
                }
            }

            await Connection.InsertAsync(new Group
            {
                Name = name,
                IsDefault = isDefault
            });

            return true;
        }

        public Task<Group> GetGroupAsync(int id)
        {
            return Connection.GetAsync<Group>(id);
        }

        public Task<Group> GetGroupByNameAsync(string name)
        {
            return Connection.Table<Group>().Where(p => p.Name.ToUpper() == name.ToUpper()).FirstOrDefaultAsync();
        }

        public async Task<List<Group>> GetGroupsWithPermissionAsync(string permission)
        {
            var perms = await Connection.Table<Permission>().Where(p => p.Name.ToUpper() == permission.ToUpper()).ToListAsync();
            var ids = perms.Select(p => p.GroupId);

            var groups = new List<Group>();
            foreach (int id in ids)
            {
                Group g = await Connection.GetAsync<Group>(id);

                if (g == null)
                    continue;

                groups.Add(g);
            }

            return groups;
        }

        public Task SaveGroupAsync(Group group)
        {
            return Connection.InsertOrReplaceWithChildrenAsync(group);
        }

        public async Task AddEventAsync(string text, string[] eventTypes, int? accountId = null)
        {
            EventHistory evt = new EventHistory
            {
                Text = text,
                AccountId = accountId.HasValue ? accountId.Value : 0,
                EventTypes = new List<EventType>()
            };

            foreach (string type in eventTypes)
            {
                EventType evtType = await Connection.Table<EventType>().Where(p => p.Name.ToUpper() == type.ToUpper()).FirstOrDefaultAsync();

                if (evtType != null)
                {
                    evt.EventTypes.Add(evtType);
                }
                else
                {
                    evt.EventTypes.Add(new EventType { Name = type });
                }
            }

            await Connection.InsertWithChildrenAsync(evt);
        }

        public Task SaveEventAsync(EventHistory evt)
        {
            return Connection.InsertOrReplaceWithChildrenAsync(evt);
        }
    }
}
