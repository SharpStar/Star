﻿// SharpStar. A Starbound wrapper.
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
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace StarLib.Database.Models
{
    public class Ban
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string PlayerName { get; set; }

        public string Reason { get; set; }

        public bool Active { get; set; }

        public DateTime ExpirationTime { get; set; }
        
        public string Uuid { get; set; }

        [Indexed(Unique = true)]
        public string IpAddress { get; set; }

        [ForeignKey(typeof(Account))]
        public int AccountId { get; set; }
    }
}
