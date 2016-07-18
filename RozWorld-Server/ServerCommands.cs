/**
 * Oddmatics.RozWorld.Server.ServerCommands -- RozWorld Server Default Commands
 *
 * This source-code is part of the server library for the RozWorld project by rozza of Oddmatics:
 * <<http://www.oddmatics.uk>>
 * <<http://roz.world>>
 * <<http://github.com/rozniak/RozWorld-Server>>
 *
 * Sharing, editing and general licence term information can be found inside of the "LICENCE.MD" file that should be located in the root of this project's directory structure.
 */

using Oddmatics.RozWorld.API.Generic;
using Oddmatics.RozWorld.API.Generic.Chat;
using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.API.Server.Event;
using System;
using System.Collections.Generic;
using Oddmatics.RozWorld.API.Server.Entities;

namespace Oddmatics.RozWorld.Server
{
    static class ServerCommands
    {
        private const string ERROR_INVALID_ARGS_LENGTH = "Invalid amount of arguments passed for ";
        private const string ERROR_INVALID_PERMISSIONS = "You do not have permission to execute command ";


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
                    // All commands
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.*", "Full RozWorld base permissions.");

                    // Command /kick
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.kick", "Kick players from the server.");
                    RwCore.Server.RegisterCommand("kick", ServerKick);

                    // Command /list
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.list", "Lists all players currently online.");
                    RwCore.Server.RegisterCommand("list", ServerList);

                    // Command /say
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.say.*", "Full talking permissions");
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.say.self", "Talk in game chat.");
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.say.server", "Talk in game chat as the server.");
                    RwCore.Server.RegisterCommand("say", ServerSay);

                    // Command /stop
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.stop", "Stops the server.");
                    RwCore.Server.RegisterCommand("stop", ServerStop);
                }
                else
                    throw new InvalidOperationException("ServerCommands.Register: Commands have already been registered.");
            }
            else
                throw new InvalidOperationException("ServerCommands.Register: Cannot register commands while the server is null.");
        }


        /// <summary>
        /// [Command Callback] Handles the /kick command.
        /// </summary>
        private static bool ServerKick(IAccount sender, IList<string> args)
        {
            const string cmdName = "/kick";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.kick"))
            {
                if (args.Count == 0)
                {
                    sender.PlayerInstance.SendMessage(ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
                    return false;
                }

                string reason = String.Empty;

                if (args.Count > 1)
                {
                    for (int i = 1; i < args.Count; i++)
                    {
                        reason += args[i];

                        if (i < args.Count - 1)
                            reason += " ";
                    }
                }

                if (!RwCore.Server.Kick(args[0], reason)) // Try to kick, if unsuccessful then the player didn't exist
                {
                    sender.PlayerInstance.SendMessage(ChatColour.RED + "Invalid player specified to kick.");
                    return false;
                }

                return true;
            }

            sender.PlayerInstance.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /list command.
        /// </summary>
        private static bool ServerList(IAccount sender, IList<string> args)
        {
            const string cmdName = "/list";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.list"))
            {
                IList<Player> onlinePlayers = RwCore.Server.OnlinePlayers;

                string playerList = String.Empty;

                for (int i = 0; i < onlinePlayers.Count; i++)
                {
                    playerList += onlinePlayers[i].DisplayName;

                    if (i < onlinePlayers.Count - 1)
                        playerList += ", ";
                }

                sender.PlayerInstance.SendMessage(onlinePlayers.Count.ToString() + " currently online players:");
                sender.PlayerInstance.SendMessage(playerList + ".");
            }

            sender.PlayerInstance.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /say command.
        /// </summary>
        private static bool ServerSay(IAccount sender, IList<string> args)
        {
            const string cmdName = "/say";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.say.*") ||
                sender.HasPermission("rwcore.say.server"))
            {
                if (args.Count == 0)
                {
                    sender.PlayerInstance.SendMessage(ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
                    return false;
                }

                string message = "<Server>";

                foreach (string arg in args)
                {
                    message += " " + arg;
                }

                RwCore.Server.BroadcastMessage(message);

                return true;
            }

            sender.PlayerInstance.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /stop command.
        /// </summary>
        private static bool ServerStop(IAccount sender, IList<string> args)
        {
            const string cmdName = "/stop";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.stop"))
                ((RwServer)RwCore.Server).Stop();

            sender.PlayerInstance.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }
    }
}
