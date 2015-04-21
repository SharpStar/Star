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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StarLib.Security
{
	/// <summary>
	/// Security helper methods for Starbound
	/// </summary>
	public static class StarSecurity
	{
		public static byte[] EmptySalt
		{
			get
			{
				return new byte[128];
			}
		}

		public static string GenerateHash(string account, string password, byte[] salt)
		{
			byte[] passAcct = Encoding.UTF8.GetBytes(password + account);

			var sha256 = SHA256.Create();
			var hash = sha256.ComputeHash(passAcct.Concat(salt).ToArray());

			sha256.Dispose();

			return Convert.ToBase64String(hash);
		}

		public static string GenerateSecureString(int length = 24)
		{
			using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
			{
				byte[] tokenData = new byte[length];
				rng.GetBytes(tokenData);

				return Convert.ToBase64String(tokenData);
			}
		}
	}
}
