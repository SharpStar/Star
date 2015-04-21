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

		public StarDb() : base(StarDbFileName)
		{
			CreateTables();
		}

		public void CreateTables()
		{
			using (var conn = CreateConnection())
			{
				conn.CreateTableIfNotExists<Account>();
				conn.CreateTableIfNotExists<Group>();
				conn.CreateTableIfNotExists<Permission>();
				conn.CreateTableIfNotExists<Character>();
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
					PasswordSalt = salt,
					PasswordHash = hash,
					GroupId = groupId,
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
			}
		}

		public bool AddBan(Player player, bool includeAccount = true)
		{
			using (var conn = CreateConnection())
			{
				if (player.Account != null && includeAccount && conn.Count<Ban>(p => p.AccountId == player.Account.Id) > 0)
					return false;

				if (conn.Count<Ban>(p => p.Uuid == player.Uuid.Id) > 0)
					return false;

				conn.Save(new Ban
				{
					 PlayerName = player.Name,
					 Uuid = player.Uuid.Id,
					 AccountId = (includeAccount && player.Account != null) ? player.Account.Id : (int?)null
				});
			}

			return true;
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

		public bool RemoveBanByUuid(string uuid)
		{
			using (var conn = CreateConnection())
			{
				Ban ban = GetBanByUuid(uuid);

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

		public Ban GetBanByUuid(string uuid)
		{
			using (var conn = CreateConnection())
			{
				return conn.Single<Ban>(new
				{
					Uuid = uuid
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
	}
}
