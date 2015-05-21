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
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using StarLib.Database.Models;

namespace StarLib.Database.Mappings
{
    public class AccountMap : ClassMap<Account>
    {
        public AccountMap()
        {
            Id(p => p.Id);
            Map(p => p.InternalId).Not.Nullable();
            Map(p => p.Username).Unique().Not.Nullable();
            Map(p => p.PasswordHash).Not.Nullable();
            Map(p => p.PasswordSalt).Not.Nullable();
            References(p => p.Group).Not.LazyLoad();
            Map(p => p.LastLogin).Nullable();
            Map(p => p.IsAdmin);
            Map(p => p.Banned);
            HasMany(p => p.Permissions).Not.LazyLoad();
            HasMany(p => p.Characters).LazyLoad();
        }
    }
}
