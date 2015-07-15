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

namespace StarLib.Commands
{
    public abstract class Command : Command<ParsedCommand, CommandContext>
    {
        protected Command(string commandName, CommandParts<ParsedCommand> parts, CommandContext context) : base(commandName, parts, context)
        {
        }
    }

    public abstract class Command<TParts, TContext> : ICommand where TParts : ParsedCommand, new() where TContext : CommandContext
    {

        protected CommandParts<TParts> Parts { get; private set; }

        public TContext Context { get; internal set; }

        public virtual string Description { get; protected set; }

        public string CommandName { get; protected set; }

        protected Command(string commandName) : this(commandName, new CommandParts<TParts>(commandName), null)
        {
        }

        protected Command(string commandName, CommandParts<TParts> parts, TContext context) : this(commandName, context)
        {
            Parts = parts;
        }

        protected Command(string commandName, TContext context)
        {
            CommandName = commandName;
            Context = context;
        }

        protected virtual CommandParts<TParts> CreateCommandParts()
        {
            return new CommandParts<TParts>(CommandName);
        }

        public virtual string GetHelp(string[] arguments)
        {
            throw new NotImplementedException();
        }

        public virtual bool PassCommand(string command)
        {
            string[] result;
            var parsed = TryParseCommand(command, out result);

            if (result != null)
            {
                parsed.Executor(new TParts { Arguments = result });
            }

            return result != null;
        }

        public virtual CommandExecutorPart<TParts> TryParseCommand(string command, out string[] result)
        {
            string[] r = null;
            CommandPart errorPart = null;
            CommandError error = CommandError.Success;
            
            foreach (CommandExecutorPart<TParts> part in Parts)
            {
                error = part.Part.TryParse(command, out result);

                if (error == CommandError.Success)
                    return part;

                if (error != CommandError.StaticTextMismatch)
                    continue;

                errorPart = part.Part;
                r = result;
            }

            result = r;

            if (result != null)
            {
                OnError(errorPart, error);
            }

            return null;
        }

        protected virtual void OnError(CommandPart part, CommandError error)
        {
        }
    }
}
