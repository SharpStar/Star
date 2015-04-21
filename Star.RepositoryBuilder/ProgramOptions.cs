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
using CommandLine;
using CommandLine.Text;

namespace Star.RepositoryBuilder
{
	public class ProgramOptions
	{

		[Option('r', "repo", HelpText = "The url of the repository", Required = true)]
		public string AddinRepoUrl { get; set; }

		[Option('p', "plugins", HelpText = "Location where the plugins are located")]
		public string PluginDir { get; set; }

		[Option('t', "template", HelpText = "The template to use to build the repository")]
		public string Template { get; set; }

		[Option('o', "output", HelpText = "The directory to export the files to", Required = true)]
		public string OutputDirectory { get; set; }

		[ParserState]
		public IParserState LastParserState { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}
}
