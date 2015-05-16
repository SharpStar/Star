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
            Parts["{0} yes"] = p =>
            {
                string username = p.Arguments[0];

                Account account = StarMain.Instance.Database.GetAccountByUsername(username);

                if (account == null)
                {
                    StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["MakeAdminConsoleCommandAccountNotExistError"]);

                    return;
                }

                account.IsAdmin = true;

                StarMain.Instance.Database.SaveAccount(account);

                StarLog.DefaultLogger.Info(string.Format(StarMain.Instance.CurrentLocalization["MakeAdminConsoleCommandSuccessMessageFormat"],
                    account.Username, "yes"));
            };

            Parts["{0} no"] = p =>
            {
                string username = p.Arguments[0];

                Account account = StarMain.Instance.Database.GetAccountByUsername(username);

                if (account == null)
                {
                    StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["MakeAdminConsoleCommandAccountNotExistError"]);

                    return;
                }

                account.IsAdmin = false;

                StarMain.Instance.Database.SaveAccount(account);

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
