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
	public class EventObject<TKey> : EventObject<TKey, StarEvent> where TKey : IEquatable<TKey>
	{
		public EventObject(object obj, IEnumerable<EventMethod<TKey, StarEvent>> methods) : base(obj, methods)
		{
		}
	}

	public class EventObject<TKey, TVal> where TKey : IEquatable<TKey> where TVal : StarEvent
	{
		public object EventObj { get; protected set; }

		public EventMethod<TKey, TVal>[] Methods { get; protected set; }

		public EventObject(object obj, IEnumerable<EventMethod<TKey, TVal>> methods)
		{
			EventObj = obj;
			Methods = methods.ToArray();
		}

		public void PassEvent(TKey key, TVal val)
		{
			foreach (var evtMethod in Methods.Where(p => p.Key.Equals(key)))
			{
				evtMethod.Execute(val);
			}
		}

	}
}
