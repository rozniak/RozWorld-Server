using Oddmatics.RozWorld.API.Generic;
using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.API.Server.Event;
using System;
using System.Collections.Generic;

namespace Oddmatics.RozWorld.Server
{
    static class ServerCommands
    {
        private static bool Registered = false;


        /// <summary>
        /// Registers default commands to the server.
        /// </summary>
        public static void Register()
        {
            if (RwCore.Server != null)
            {
                if (!Registered)
                {
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.say.*", "Full talking permissions");
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.say.self", "Talk in game chat.");
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.say.server", "Talk in game chat as the server.");
                    RwCore.Server.RegisterCommand("say", ServerSay);
                }
                else
                    throw new InvalidOperationException("Commands have already been registered.");
            }
            else
                throw new InvalidOperationException("Cannot register commands while the server is null.");
        }


        /// <summary>
        /// [Command Callback] Handles the /say command.
        /// </summary>
        private static bool ServerSay(IAccount sender, IList<string> args)
        {
            if (sender.HasPermission("rwcore.say.server"))
            {
                string message = "<Server>";

                foreach (string arg in args)
                {
                    message += " " + arg;
                }

                RwCore.Server.BroadcastMessage(message);

                return true;
            }

            return false;
        }
    }
}
