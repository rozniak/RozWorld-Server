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
using Oddmatics.RozWorld.API.Server;
using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.API.Server.Entities;
using Oddmatics.RozWorld.API.Server.Event;
using System;
using System.Collections.Generic;

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
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.list", "Lists all visible players currently online.");
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.list.all", "Lists everyone online regardless of visibility.");
                    RwCore.Server.RegisterCommand("list", ServerList);

                    // Command /me
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.me", "Enacts an action in the game chat.");
                    RwCore.Server.RegisterCommand("me", ServerMe);

                    // Command /msg
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.msg", "Send a private message to a player.");
                    RwCore.Server.RegisterCommand("msg", ServerPrivateMessage);

                    // Command /plugins
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.plugins", "View the installed server plugins.");
                    RwCore.Server.RegisterCommand("plugins", ServerPlugins);

                    // Command /say
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.say.*", "Full talking permissions");
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.say.self", "Talk in game chat.");
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.say.server", "Talk in game chat as the server.");
                    RwCore.Server.RegisterCommand("say", ServerSay);

                    // Command /slap
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.slap", "Slaps a player.");
                    RwCore.Server.RegisterCommand("slap", ServerSlap);

                    // Command /stop
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.stop", "Stops the server.");
                    RwCore.Server.RegisterCommand("stop", ServerStop);

                    // Command /whisper
                    RwCore.Server.PermissionAuthority.RegisterPermission("rwcore.whisper", "Send a quick private message to a player.");
                    RwCore.Server.RegisterCommand("whisper", ServerWhisper);
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
        private static bool ServerKick(ICommandCaller sender, IList<string> args)
        {
            const string cmdName = "/kick";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.kick"))
            {
                if (args.Count == 0)
                {
                    sender.SendMessage(ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
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
                    sender.SendMessage(ChatColour.RED + "Invalid player specified to kick.");
                    return false;
                }

                return true;
            }

            sender.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /list command.
        /// </summary>
        private static bool ServerList(ICommandCaller sender, IList<string> args)
        {
            // TODO: patch this function up a bit, it raises an exception right now

            const string cmdName = "/list";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.list"))
            {
                IList<Player> onlinePlayers = RwCore.Server.OnlinePlayers;

                string playerList = String.Empty;

                for (int i = 0; i < onlinePlayers.Count; i++)
                {
                    Player foundPlayer = onlinePlayers[i];
                    string fullDisplayName = String.Empty;

                    if (sender.HasPermission("rwcore.list.all"))
                    {
                        if (!foundPlayer.IsRealPlayer) // Player is a bot
                            fullDisplayName += "B|";

                        if (!foundPlayer.IsValid) // Player is chat-only
                            fullDisplayName += "C|";

                        if (!foundPlayer.VisibleOnScoreboard)
                            fullDisplayName += "I|";

                        fullDisplayName += foundPlayer.DisplayName;

                        if (foundPlayer.Joinable)
                            fullDisplayName += "*";

                        if (foundPlayer.AFK)
                            fullDisplayName += "(AFK)";
                    }
                    else
                        fullDisplayName = foundPlayer.DisplayName;

                    playerList += fullDisplayName;

                    if (i < onlinePlayers.Count - 1)
                        playerList += ", ";
                }

                sender.SendMessage(onlinePlayers.Count.ToString() + " currently online players:");
                sender.SendMessage(playerList + ".");
            }

            sender.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /me command.
        /// </summary>
        private static bool ServerMe(ICommandCaller sender, IList<string> args)
        {
            const string cmdName = "/me";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.me"))
            {
                if (args.Count == 0)
                {
                    sender.SendMessage(ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
                    return false;
                }

                string message = ChatColour.YELLOW + " *" + sender.DisplayName;

                foreach (string arg in args)
                {
                    message += " " + arg;
                }

                RwCore.Server.BroadcastMessage(message);

                return true;
            }

            sender.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /plugins command.
        /// </summary>
        private static bool ServerPlugins(ICommandCaller sender, IList<string> args)
        {
            const string cmdName = "/plugins";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.plugins"))
            {
                IList<IPlugin> plugins = RwCore.Server.Plugins;
                string message = "Installed plugins (" + plugins.Count + "): ";

                for (int i = 0; i < plugins.Count; i++)
                {
                    message += plugins[i].Name;

                    if (i < plugins.Count - 1)
                        message += ", ";
                }

                sender.SendMessage(message);
                return true;
            }

            sender.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /msg command.
        /// </summary>
        private static bool ServerPrivateMessage(ICommandCaller sender, IList<string> args)
        {
            const string cmdName = "/msg";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.msg"))
            {
                if (args.Count <= 1)
                {
                    sender.SendMessage(ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
                    return false;
                }

                string message = String.Empty;
                Player targetPlayer = RwCore.Server.GetPlayerAbsolute(args[0]);

                for (int i = 1; i <= args.Count; i++)
                {
                    message += args[i];

                    if (i < args.Count - 1)
                        message += " ";
                }

                // TODO: Sort this out... sending private messages will most likely be implemented via
                // a function like:
                //     RwServer.SendPrivateMessageTo(message, sender, recipient)

                //sender.PlayerInstance.SendPrivateMessageTo(message, targetPlayer); old code

                // Placeholder for now
                sender.SendMessage("Private messaging not implemented yet.");

                return true;
            }

            sender.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /say command.
        /// </summary>
        private static bool ServerSay(ICommandCaller sender, IList<string> args)
        {
            const string cmdName = "/say";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.say.*") ||
                sender.HasPermission("rwcore.say.server"))
            {
                if (args.Count == 0)
                {
                    sender.SendMessage(ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
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

            sender.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /slap command.
        /// </summary>
        private static bool ServerSlap(ICommandCaller sender, IList<string> args)
        {
            const string cmdName = "/slap";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.slap"))
            {
                if (args.Count != 1)
                {
                    sender.SendMessage(ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
                    return false;
                }

                Player targetPlayer = RwCore.Server.GetPlayer(args[0]);

                if (targetPlayer == null)
                {
                    sender.SendMessage(ChatColour.RED + "You can't slap someone that isn't here!");
                    return false;
                }

                RwCore.Server.BroadcastMessage(ChatColour.ORANGE + " *" + sender.DisplayName
                    + " slaps " + targetPlayer.DisplayName + " with a large eel.");

                return true;
            }

            sender.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /stop command.
        /// </summary>
        private static bool ServerStop(ICommandCaller sender, IList<string> args)
        {
            const string cmdName = "/stop";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.stop"))
            {
                ((RwServer)RwCore.Server).Stop();
                return true;
            }

            sender.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /whisper command.
        /// </summary>
        private static bool ServerWhisper(ICommandCaller sender, IList<string> args)
        {
            const string cmdName = "/whisper";

            if (sender.HasPermission("rwcore.*") || sender.HasPermission("rwcore.whisper"))
            {
                if (args.Count <= 1)
                {
                    sender.SendMessage(ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
                    return false;
                }

                Player targetPlayer = RwCore.Server.GetPlayer(args[0]);

                if (targetPlayer == null)
                {
                    sender.SendMessage(ChatColour.RED + "There's no one by that name to whisper to!");
                    return false;
                }

                string senderMessage = "[me -> " + targetPlayer.DisplayName + "]";
                string recipientMessage = "[" + sender.DisplayName + " -> me]";
                string message = String.Empty;

                for (int i = 1; i <= args.Count; i++)
                {
                    message += " " + args[i];
                }

                sender.SendMessage(senderMessage + message);
                targetPlayer.SendMessage(recipientMessage + message);

                return true;
            }

            return false;
        }
    }
}
