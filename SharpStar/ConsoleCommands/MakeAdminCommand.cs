using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarLib;
using StarLib.Commands.Console;
using StarLib.Database.Models;
using StarLib.Logging;

namespace SharpStar.ConsoleCommands
{
    public class MakeAdminCommand : ConsoleCommand
    {
        public MakeAdminCommand() : base(StarMain.Instance.CurrentLocalization["MakeAdminConsoleCommandName"] ?? "makeadmin")
        {
            Parts["{0} yes"] = async p =>
            {
                string username = p.Arguments[0];

                Account account = await StarMain.Instance.Database.GetAccountByUsernameAsync(username);

                if (account == null)
                {
                    StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["MakeAdminConsoleCommandAccountNotExistError"]);

                    return;
                }

                account.IsAdmin = true;

                await StarMain.Instance.Database.SaveAccountAsync(account);

                StarLog.DefaultLogger.Info(string.Format(StarMain.Instance.CurrentLocalization["MakeAdminConsoleCommandSuccessMessageFormat"],
                    account.Username, "yes"));
            };

            Parts["{0} no"] = async p =>
            {
                string username = p.Arguments[0];

                Account account = await StarMain.Instance.Database.GetAccountByUsernameAsync(username);

                if (account == null)
                {
                    StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["MakeAdminConsoleCommandAccountNotExistError"]);

                    return;
                }

                account.IsAdmin = false;

                await StarMain.Instance.Database.SaveAccountAsync(account);

                StarLog.DefaultLogger.Info(string.Format(StarMain.Instance.CurrentLocalization["MakeAdminConsoleCommandSuccessMessageFormat"],
                    account.Username, "no"));
            };
        }

        public override string Description
        {
            get
            {
                return StarMain.Instance.CurrentLocalization["MakeAdminConsoleCommandDesc"];
            }
        }

        public override string GetHelp(string[] arguments)
        {
            return StarMain.Instance.CurrentLocalization["MakeAdminConsoleCommandHelp"];
        }
    }
}
