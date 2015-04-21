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

namespace StarLib.Events
{
	public abstract class EventManager<TKey, TVal> where TKey : IEquatable<TKey> where TVal : StarEvent
	{
		public Dictionary<object, EventObject<TKey, TVal>> Events { get; protected set; }

		protected EventManager()
		{
			Events = new Dictionary<object, EventObject<TKey, TVal>>();
		}

		public void PassEvent(TKey key, TVal evt)
		{
			foreach (var evtObj in Events.Values)
			{
				evtObj.PassEvent(key, evt);
			}
		}

		public void AddEventObject(object obj)
		{
			Events.Add(obj, CreateEventObject(obj));
		}

		protected abstract EventObject<TKey, TVal> CreateEventObject(object obj);
	}
}
