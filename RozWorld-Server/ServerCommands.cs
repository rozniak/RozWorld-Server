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
using Oddmatics.RozWorld.API.Server.Entities;
using Oddmatics.RozWorld.API.Server.Event;
using System;
using System.Collections.Generic;
using Oddmatics.RozWorld.API.Server;

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
        /// Handles sending a message to the command caller, must be the server or a player.
        /// </summary>
        /// <param name="recipient">The command caller, must be the server or a player.</param>
        /// <param name="message">The message to send.</param>
        private static void SafeMessage(object recipient, string message)
        {
            if (recipient is IRwServer)
                ((IRwServer)recipient).Logger.Out(message);
            else if (recipient is Player)
                ((Player)recipient).SendMessage(message);
            else
                throw new ArgumentException("ServerCommands.SafeMessage: Invalid Type of recipient given.");
        }


        /// <summary>
        /// [Command Callback] Handles the /kick command.
        /// </summary>
        private static bool ServerKick(object sender, IList<string> args)
        {
            const string cmdName = "/kick";
            Player player = sender is Player ? (Player)sender : null;

            if (sender is IRwServer ||
                player.HasPermission("rwcore.*") || player.HasPermission("rwcore.kick"))
            {
                if (args.Count == 0)
                {
                    SafeMessage(sender, ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
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
                    SafeMessage(sender, ChatColour.RED + "Invalid player specified to kick.");
                    return false;
                }

                return true;
            }

            player.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /list command.
        /// </summary>
        private static bool ServerList(object sender, IList<string> args)
        {
            const string cmdName = "/list";
            Player player = sender is Player ? (Player)sender : null;

            if (sender is IRwServer ||
                player.HasPermission("rwcore.*") || player.HasPermission("rwcore.list"))
            {
                IList<Player> onlinePlayers = RwCore.Server.OnlinePlayers;

                string playerList = String.Empty;

                for (int i = 0; i < onlinePlayers.Count; i++)
                {
                    Player foundPlayer = onlinePlayers[i];
                    string fullDisplayName = String.Empty;

                    if (sender is IRwServer ||
                        player.HasPermission("rwcore.list.all"))
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

                SafeMessage(sender, onlinePlayers.Count.ToString() + " currently online players:");
                SafeMessage(sender, playerList + ".");
            }

            player.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /me command.
        /// </summary>
        private static bool ServerMe(object sender, IList<string> args)
        {
            const string cmdName = "/me";
            Player player = sender is Player ? (Player)sender : null;

            if (sender is IRwServer ||
                player.HasPermission("rwcore.*") || player.HasPermission("rwcore.me"))
            {
                if (args.Count == 0)
                {
                    SafeMessage(sender, ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
                    return false;
                }

                string displayName = sender is IRwServer ? "server" : player.DisplayName;
                string message = ChatColour.YELLOW + " *" + displayName;

                foreach (string arg in args)
                {
                    message += " " + arg;
                }

                RwCore.Server.BroadcastMessage(message);

                return true;
            }

            player.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /msg command.
        /// </summary>
        private static bool ServerPrivateMessage(object sender, IList<string> args)
        {
            const string cmdName = "/msg";
            Player player = sender is Player ? (Player)sender : null;

            if (sender is IRwServer ||
                player.HasPermission("rwcore.*") || player.HasPermission("rwcore.msg"))
            {
                if (args.Count <= 1)
                {
                    SafeMessage(sender, ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
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
                SafeMessage(sender, "Private messaging not implemented yet.");

                return true;
            }

            player.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /say command.
        /// </summary>
        private static bool ServerSay(object sender, IList<string> args)
        {
            const string cmdName = "/say";
            Player player = sender is Player ? (Player)sender : null;

            if (sender is IRwServer ||
                player.HasPermission("rwcore.*") || player.HasPermission("rwcore.say.*") ||
                player.HasPermission("rwcore.say.server"))
            {
                if (args.Count == 0)
                {
                    SafeMessage(sender, ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
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

            player.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /slap command.
        /// </summary>
        private static bool ServerSlap(object sender, IList<string> args)
        {
            const string cmdName = "/slap";
            Player player = sender is Player ? (Player)sender : null;

            if (sender is IRwServer ||
                player.HasPermission("rwcore.*") || player.HasPermission("rwcore.slap"))
            {
                if (args.Count != 1)
                {
                    SafeMessage(sender, ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
                    return false;
                }

                Player targetPlayer = RwCore.Server.GetPlayer(args[0]);

                if (targetPlayer == null)
                {
                    SafeMessage(sender, ChatColour.RED + "You can't slap someone that isn't here!");
                    return false;
                }

                string displayName = sender is IRwServer ? "server" : player.DisplayName;
                RwCore.Server.BroadcastMessage(ChatColour.ORANGE + " *" + displayName + " slaps " +
                    targetPlayer.DisplayName + " with a large eel.");

                return true;
            }

            player.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /stop command.
        /// </summary>
        private static bool ServerStop(object sender, IList<string> args)
        {
            const string cmdName = "/stop";
            Player player = sender is Player ? (Player)sender : null;

            if (sender is IRwServer ||
                player.HasPermission("rwcore.*") || player.HasPermission("rwcore.stop"))
            {
                ((RwServer)RwCore.Server).Stop();
                return true;
            }

            player.SendMessage(ChatColour.RED + ERROR_INVALID_PERMISSIONS + cmdName + ".");
            return false;
        }

        /// <summary>
        /// [Command Callback] Handles the /whisper command.
        /// </summary>
        private static bool ServerWhisper(object sender, IList<string> args)
        {
            const string cmdName = "/whisper";
            Player player = sender is Player ? (Player)sender : null;

            if (sender is IRwServer ||
                player.HasPermission("rwcore.*") || player.HasPermission("rwcore.whisper"))
            {
                if (args.Count <= 1)
                {
                    SafeMessage(sender, ChatColour.RED + ERROR_INVALID_ARGS_LENGTH + cmdName + ".");
                    return false;
                }

                Player targetPlayer = RwCore.Server.GetPlayer(args[0]);

                if (targetPlayer == null)
                {
                    SafeMessage(sender, ChatColour.RED + "There's no one by that name to whisper to!");
                    return false;
                }

                string displayName = sender is IRwServer ? "server" : player.DisplayName;
                string message = "[" + displayName + " -> me]";

                for (int i = 1; i <= args.Count; i++)
                {
                    message += " " + args[i];
                }

                targetPlayer.SendMessage(message);

                return true;
            }

            return false;
        }
    }
}
