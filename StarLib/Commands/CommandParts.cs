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
using System.Threading.Tasks;

namespace StarLib.Commands
{
    public class CommandParts : CommandParts<ParsedCommand>
    {
        public CommandParts() : base(string.Empty)
        {
        }

        public CommandParts(string commandName) : base(commandName)
        {
        }
    }

    public class CommandParts<T> : IEnumerable<CommandExecutorPart<T>> where T : ParsedCommand
    {

        public string CommandName { get; set; }

        protected readonly Dictionary<string, CommandExecutorPart<T>> Parts;

        public CommandParts()
            : this(string.Empty)
        {
        }

        public CommandParts(string commandName)
        {
            Parts = new Dictionary<string, CommandExecutorPart<T>>();
            CommandName = commandName;
        }

        public Action<T> this[string exp]
        {
            get
            {
                return Parts[exp].Executor;
            }
            set
            {
                if (value == null)
                    Parts[exp] = null;
                else
                    Parts[exp] = new CommandExecutorPart<T>(new CommandPart(CommandName, exp), value);
            }
        }

        public IEnumerator<CommandExecutorPart<T>> GetEnumerator()
        {
            return Parts.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
