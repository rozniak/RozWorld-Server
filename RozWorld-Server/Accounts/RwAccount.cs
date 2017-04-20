/**
 * Oddmatics.RozWorld.Server.Accounts.Account -- RozWorld Server User Account Implementation
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
using Oddmatics.RozWorld.API.Server.Game;
using Oddmatics.RozWorld.Formats;
using Oddmatics.RozWorld.Net.Packets;
using Oddmatics.RozWorld.Net.Server;
using Oddmatics.RozWorld.Server.Entities;
using Oddmatics.Util.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;

namespace Oddmatics.RozWorld.Server.Accounts
{
    public sealed class RwAccount : IAccount
    {
        public string ChatPrefix { get; set; }
        public string ChatSuffix { get; set; }
        public IClan Clan
        {
            get { throw new NotImplementedException(); }
        }
        private string _ColourModifier;
        public string ColourModifier
        {
            get { return _ColourModifier; }
            set { if (ChatColour.IsChatColour(value) || value == String.Empty) _ColourModifier = value; }
        }
        public DateTime CreationDate
        {
            get { return AccountFile.CreationDate; }
        }
        public IPAddress CreationIP
        {
            get { return AccountFile.CreationIP; }
        }
        public IPAddress CurrentIP;
        public string DisplayName
        {
            get { return AccountFile.DisplayName; }
            set
            {
                // Validation - ensure account file safety
                if (RwPlayer.ValidName(value))
                {
                    string realDisplayName = DisplayName.ToLower();
                    string realUsername = Username.ToLower();
                    string realValue = value.ToLower();
                    RwServer server = (RwServer)RwCore.Server;

                    // Check if name already in use
                    if (Directory.GetFiles(RwServer.DIRECTORY_ACCOUNTS, "*." + realValue + ".acc").Length == 0)
                    {
                        // Update filesystem
                        try
                        {
                            string oldFile = RwServer.DIRECTORY_ACCOUNTS + "\\" + realUsername +
                                "." + DisplayName + ".acc";

                            if (File.Exists(oldFile))
                                File.Delete(oldFile);

                            AccountFile.Save(RwServer.DIRECTORY_ACCOUNTS + "\\" + realUsername + "." +
                                realValue + ".acc");
                        }
                        catch (Exception ex)
                        {
                            // Some error, log it and do not continue the changes
                            server.Logger.Out("Problem whilst changing display name for user '" + Username +
                                "', exception: " + ex.Message, LogLevel.Error);

                            server.Logger.Out("Stack trace: " + ex.StackTrace,LogLevel.Error);
                        }
                    }
                    else
                        server.Logger.Out("Failed to set new display name for user '" + Username +
                            "': name value is already in use!", LogLevel.Error);
                }
            }
        }
        public string Fqn { get { return Username + "." + DisplayName; } }
        public IPAddress LastLoginIP
        {
            get { return AccountFile.LastLogInIP; }
        }
        public IList<string> LocalPermissions
        {
            get
            {
                var grantedPermissions = new List<string>();
                foreach (var item in PermissionStates)
                {
                    if (item.Value == PermissionState.Granted)
                        grantedPermissions.Add(item.Key);
                }
                return grantedPermissions.AsReadOnly();
            }
        }
        public bool IsPlayer { get { return PlayerInstance != null; } }
        public IPermissionGroup PermissionGroup { get; private set; }
        public IList<string> Permissions
        {
            get
            {
                var grantedPermissions = new List<string>();
                grantedPermissions.AddRange(PermissionGroup.Permissions);

                foreach (var item in PermissionStates)
                {
                    if (item.Value == PermissionState.Granted)
                        grantedPermissions.Add(item.Key);
                    else if (item.Value == PermissionState.Denied &&
                        grantedPermissions.Contains(item.Key))
                        grantedPermissions.Remove(item.Key);
                }

                return grantedPermissions.AsReadOnly();
            }
        }
        public Player PlayerInstance { get; private set; }
        public string Username
        {
            get { return AccountFile.Username; }
            set { if (RwPlayer.ValidName(value)) AccountFile.Username = value; }
        }


        private AccountRecord AccountRecord;
        private bool Exists;
        public bool LoggedIn { get; private set; }
        private Dictionary<string, PermissionState> PermissionStates;
        public static bool ServerAccountCreated { get; private set; }


        public RwAccount(string username)
        {
            string realUsername = username.ToLower();
            string[] permFiles = Directory.GetFiles(RwServer.DIRECTORY_PERMISSIONS,
                "player-" + realUsername + ".json");
            RwServer server = (RwServer)RwCore.Server;

            AccountRecord record = ((RwAccountsManager)server.AccountsManager).GetAccountRecord(username);

            Exists = false;
            LoggedIn = false;

            if (record != null)
            {
                AccountRecord = record;

                if (permFiles.Length == 0)
                {
                    ((RwPermissionAuthority)RwCore.Server.PermissionAuthority)
                        .CreateDefaultPlayerFile(username);
                    permFiles = Directory.GetFiles(RwServer.DIRECTORY_PERMISSIONS,
                        "player-" + realUsername + ".json"); // Update permFiles listing
                }

                var permFile = PlayerPermissionFile.FromFile(permFiles[0]);

                if (!permFile.Name.EqualsIgnoreCase(Username)) // Check for name mismatch
                    server.Logger.Out("Permission file for " + Username +
                        ", 'name' value mismatch. This may or may not be intended.",
                        LogLevel.Warning);

                ChatPrefix = permFile.Prefix;
                ChatSuffix = permFile.Suffix;
                ColourModifier = permFile.Colour;
                PermissionGroup = server.PermissionAuthority
                    .GetGroup(permFile.Group);

                PermissionStates = new Dictionary<string, PermissionState>();

                // Add permission states, this is ordered by "Allow, Deny"

                foreach (string perm in permFile.Granted) // Allow
                {
                    string realPerm = perm.ToLower();

                    if (server.PermissionAuthority.RegisteredPermissions.Contains(realPerm))
                        PermissionStates.Add(realPerm, PermissionState.Granted);
                }

                foreach (string perm in permFile.Denied) // Deny
                {
                    string realPerm = perm.ToLower();

                    if (server.PermissionAuthority.RegisteredPermissions.Contains(realPerm))
                        PermissionStates.Add(realPerm, PermissionState.Denied);
                }

                Exists = true;
            }
        }


        public PermissionState CheckAccountPermission(string key)
        {
            if (PermissionStates.ContainsKey(key))
                return PermissionStates[key];
            else
                return PermissionState.Unset;
        }

        public bool HasPermission(string key)
        {
            // Check this accounts permissions first as they take priority
            switch (CheckAccountPermission(key))
            {
                case PermissionState.Denied: return false;
                case PermissionState.Granted: return true;
                case PermissionState.Unset:
                default:
                    break;
            }

            return PermissionGroup.HasPermission(key);
        }

        public RwPlayer InstatePlayerInstance(ConnectedClient client)
        {
            if (PlayerInstance != null) // Already has instated player instance
                return null;

            if (!LoggedIn)
                return null;

            if (client == null)
                throw new ArgumentNullException("RwAccount.InstatePlayerInstance: Client cannot be null.");

            PlayerInstance = new RwPlayer(this, client);

            return (RwPlayer)PlayerInstance;
        }

        public byte LogIn(byte[] passwordHash, long utcHashTime)
        {
            if (!Exists)
                return ErrorMessage.INCORRECT_LOGIN;

            if (LoggedIn)
                return ErrorMessage.INTERNAL_ERROR;

            var saltedPass = new List<byte>(AccountFile.PasswordHash);
            saltedPass.AddRange(utcHashTime.GetBytes());

            byte[] comparisonHash = new SHA256Managed().ComputeHash(saltedPass.ToArray());

            if (comparisonHash.SequenceEqual(passwordHash))
            {
                LoggedIn = true;
                return ErrorMessage.NO_ERROR;
            }

            return ErrorMessage.INCORRECT_LOGIN;
        }

        public void Save(string destination = "")
        {
            try
            {
                // **This account was MEANT to be saved!
                string finalDestination = String.IsNullOrEmpty(destination) ?
                    RwServer.DIRECTORY_ACCOUNTS + "\\" + Fqn + ".acc" :
                    destination;

                AccountFile.Save(finalDestination);
            }
            catch (Exception ex)
            {
                ((RwServer)RwCore.Server).Logger.Out("Unable to save account '" + Username +
                    "'. Exception: " + ex.Message, LogLevel.Error);
            }
        }

        public bool SetAccountPermission(string key, PermissionState newState)
        {
            if (PermissionStates.ContainsKey(key))
            {
                if (newState == PermissionState.Unset)
                    PermissionStates.Remove(key);
                else
                    PermissionStates[key] = newState;
            }
            else if (newState != PermissionState.Unset)
                PermissionStates.Add(key, newState);

            return true;
        }

        public bool SetPermissionGroup(string name)
        {
            string realName = name.ToLower();

            if (!RwCore.Server.PermissionAuthority.GroupNames.Contains(realName))
                return false;

            PermissionGroup = RwCore.Server.PermissionAuthority.GetGroup(realName);
            return true;
        }

        public void ValidatePlayerInstance()
        {
            // Check the server to see if this account has a player instance
        }
    }
}
