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
using System.Linq.Expressions;
using System.Reflection;

namespace StarLib.Commands
{
    public abstract class CommandManager<TCommand, TContext, TOut> : IEnumerable<CommandInfo>
        where TCommand : ParsedCommand, new() where TContext : CommandContext where TOut : Command<TCommand, TContext>
    {

        public Dictionary<CommandInfo, Func<TContext, TOut>> Commands { get; private set; }

        protected CommandManager()
        {
            Commands = new Dictionary<CommandInfo, Func<TContext, TOut>>();
        }

        public void AddCommand<T>() where T : TOut, new()
        {
            T t = new T();
            Type tType = typeof(T);

            CommandInfo cInfo = new CommandInfo(tType, t.CommandName, t.Description);
            ConstructorInfo coInfo = typeof(T).GetConstructor(Type.EmptyTypes);

            if (coInfo == null)
                throw new NullReferenceException();

            PropertyInfo cmdNameProp = tType.GetProperty("CommandName", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo cmdDescProp = typeof(TOut).GetProperty("Description", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo cmdCtxProp = tType.GetProperty("Context", BindingFlags.Public | BindingFlags.Instance);

            var ctx = Expression.Parameter(typeof(TContext), "ctx");
            var command = Expression.Variable(typeof(TOut), "cmd");

            var block = Expression.Block(new[] { command },
                Expression.Assign(command, Expression.Convert(Expression.New(coInfo), typeof(TOut))),
                Expression.Assign(Expression.Property(command, cmdCtxProp), ctx),
                Expression.Assign(Expression.Property(command, cmdNameProp), Expression.Constant(t.CommandName)),
                Expression.Assign(Expression.Property(command, cmdDescProp), Expression.Constant(t.Description)),
                command
            );

            var func = Expression.Lambda<Func<TContext, TOut>>(block, ctx).Compile();

            Commands.Add(cInfo, func);
        }

        public virtual bool PassCommand(string command, TContext ctx)
        {
            bool result = false;

            string[] ex = command.Split(' ');
            
            foreach (var cmd in Commands.Where(p => p.Key.Name.Equals(ex[0], StringComparison.CurrentCultureIgnoreCase)))
            {
                var newCmd = cmd.Value(ctx);

                if (newCmd.PassCommand(command))
                    result = true;
            }

            return result;
        }

        public void AddCommands(IDictionary<CommandInfo, Func<TContext, TOut>> commands)
        {
            foreach (var cmd in commands)
            {
                Commands.Add(cmd.Key, cmd.Value);
            }
        }

        public void RemoveCommand(string command)
        {
            var cmds = Commands.Where(p => p.Key.Name.Equals(command, StringComparison.CurrentCultureIgnoreCase));

            foreach (var cmd in cmds)
            {
                Commands.Remove(cmd.Key);
            }
        }

        public IEnumerator<CommandInfo> GetEnumerator()
        {
            return Commands.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}