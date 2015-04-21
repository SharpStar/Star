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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Cryptography;
using Nancy.TinyIoc;
using Star.WebPanel.Nancy;
using System.Reflection;
using Nancy.ViewEngines.Razor;

namespace Star.WebPanel
{
	internal class Bootstrapper : DefaultNancyBootstrapper
	{
		private static readonly Lazy<FormsAuthenticationConfiguration> formsConfiguration = new Lazy<FormsAuthenticationConfiguration>(() => new FormsAuthenticationConfiguration());

		public static FormsAuthenticationConfiguration FormsConfiguration
		{
			get
			{
				return formsConfiguration.Value;
			}
		}

		protected override IRootPathProvider RootPathProvider
		{
			get { return new CustomRootPathProvider(); }
		}

		protected override void ConfigureApplicationContainer(TinyIoCContainer container)
		{
			container.Register<IUserMapper, StarUserMapper>();

			//var signalrDependency = new SignalRDependencyResolver(container);
			//GlobalHost.DependencyResolver = signalrDependency;
		}

		protected override void ConfigureConventions(NancyConventions nancyConventions)
		{
			base.ConfigureConventions(nancyConventions);

			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("Scripts", "Scripts"));
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("lib", "lib"));
		}

		protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
		{
			//TokenAuthentication.Enable(pipelines, new TokenAuthenticationConfiguration(container.Resolve<ITokenizer>()));

			string pass = StarWeb.WebConfig.AuthPassword;
			byte[] salt = Convert.FromBase64String(StarWeb.WebConfig.AuthSalt);

			var cryptoConfig = new CryptographyConfiguration(new RijndaelEncryptionProvider(new PassphraseKeyGenerator(pass, salt)),
															new DefaultHmacProvider(new PassphraseKeyGenerator(pass, salt)));

			//FormsConfiguration.DisableRedirect = true;
			FormsConfiguration.RedirectUrl = "~/login";
			FormsConfiguration.UserMapper = container.Resolve<IUserMapper>();
			FormsConfiguration.CryptographyConfiguration = cryptoConfig;

			FormsAuthentication.Enable(pipelines, FormsConfiguration);
		}
	}
}
