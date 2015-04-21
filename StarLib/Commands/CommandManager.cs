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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StarLib.Commands
{
	public abstract class CommandManager : CommandManager<ParsedCommand, Command>
	{
		protected CommandManager()
		{
		}

		protected CommandManager(Command[] commands) : base(commands)
		{
		}
	}

	public abstract class CommandManager<TCommand, TOut> : IEnumerable<TOut> where TCommand : ParsedCommand where TOut : Command<TCommand>
	{

		public List<TOut> Commands { get; private set; }

		protected CommandManager()
		{
			Commands = new List<TOut>();
		}

		protected CommandManager(TOut[] commands)
		{
			Commands = commands.ToList();
		}

		public void AddCommand(TOut command)
		{
			Commands.Add(command);
		}

		public void AddCommands(IEnumerable<TOut> commands)
		{
			Commands.AddRange(commands);
		}

		public void RemoveCommand(TOut command)
		{
			Commands.Remove(command);

		}

		public IEnumerator<TOut> GetEnumerator()
		{
			return Commands.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}