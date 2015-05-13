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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StarLib.Events.Packets
{
    public class PacketEventManager : EventManager<PacketEventKey, PacketEvent>
    {
        protected override EventObject<PacketEventKey, PacketEvent> CreateEventObject(object obj)
        {
            var methods = new List<IEventMethod<PacketEventKey, PacketEvent>>();

            foreach (MethodInfo mInfo in obj.GetType().GetMethods())
            {
                PacketEventAttribute attrib = mInfo.GetCustomAttribute<PacketEventAttribute>();

                if (attrib == null)
                    continue;

                ParameterInfo[] parameters = mInfo.GetParameters();

                if (parameters.Length != 1 || parameters.First().ParameterType != typeof(PacketEvent))
                    throw new Exception("Invalid Packet Event object!");

                IEventMethod<PacketEventKey, PacketEvent> method;
                if (mInfo.ReturnType == typeof(void))
                {
                    Action<PacketEvent> action = (Action<PacketEvent>)Delegate.CreateDelegate(typeof(Action<PacketEvent>), obj, mInfo);

                    method = new EventMethod<PacketEventKey, PacketEvent>(new PacketEventKey { EventType = attrib.EventType, PacketId = attrib.PacketId }, action);

                }
                else if (mInfo.ReturnType == typeof(Task))
                {
                    Func<PacketEvent, Task> func = (Func<PacketEvent, Task>)Delegate.CreateDelegate(typeof(Func<PacketEvent, Task>), mInfo);

                    method = new AsyncEventMethod<PacketEventKey, PacketEvent>(new PacketEventKey { EventType = attrib.EventType, PacketId = attrib.PacketId }, func);
                }
                else
                {
                    continue; //return type must be either void or Task
                }

                methods.Add(method);

            }

            return new EventObject<PacketEventKey, PacketEvent>(obj, methods);
        }
    }
}
