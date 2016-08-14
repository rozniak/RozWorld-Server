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
    public class RwAccount : IAccount
    {

        public string ChatPrefix { get; set; }
        public string ChatSuffix { get; set; }
        public IClan Clan
        {
            get { throw new NotImplementedException(); }
        }
        public string ColourModifier { get; set; }
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
            // TODO: code this
            get { return String.Empty; }
            set { }
        }
        public IPAddress LastLoginIP
        {
            get { return AccountFile.LastLogInIP; }
        }
        public bool IsPlayer { get { return PlayerInstance != null; } }
        public IPermissionGroup PermissionGroup { get; private set; }
        public IList<string> Permissions
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
        public Player PlayerInstance { get; private set; }
        public string Username { get { return AccountFile.Username; } }


        private AccountFile AccountFile;
        private bool Exists;
        public bool LoggedIn { get; private set; }
        private Dictionary<string, PermissionState> PermissionStates;
        public static bool ServerAccountCreated { get; private set; }


        public RwAccount(string username)
        {
            string realUsername = username.ToLower();
            string[] accountFiles = Directory.GetFiles(RwServer.DIRECTORY_ACCOUNTS,
                realUsername + ".*.acc");
            string[] permFiles = Directory.GetFiles(RwServer.DIRECTORY_PERMISSIONS,
                "player-" + realUsername + ".acc");

            Exists = false;
            LoggedIn = false;

            if (accountFiles.Length == 1)
            {
                AccountFile = new AccountFile(accountFiles[0]);

                if (permFiles.Length == 0)
                {
                    ((RwPermissionAuthority)RwCore.Server.PermissionAuthority)
                        .CreateDefaultPlayerFile(username);
                }

                var permFile = PlayerPermissionFile.FromFile(permFiles[0]);

                if (!permFile.Name.EqualsIgnoreCase(Username)) // Check for name mismatch
                    RwCore.Server.Logger.Out("[WARN] Permission file for " + Username + ", 'name' value mismatch. This may or may not be intended.");

                ChatPrefix = permFile.Prefix;
                ChatSuffix = permFile.Suffix;
                ColourModifier = permFile.Colour;
                PermissionGroup = RwCore.Server.PermissionAuthority
                    .GetGroup(permFile.Group);

                PermissionStates = new Dictionary<string, PermissionState>();

                // Add permission states, this is ordered by "Allow, Deny"

                foreach (string perm in permFile.Granted) // Allow
                {
                    string realPerm = perm.ToLower();

                    if (RwCore.Server.PermissionAuthority.RegisteredPermissions.Contains(realPerm))
                        PermissionStates.Add(realPerm, PermissionState.Granted);
                }

                foreach (string perm in permFile.Denied) // Deny
                {
                    string realPerm = perm.ToLower();

                    if (RwCore.Server.PermissionAuthority.RegisteredPermissions.Contains(realPerm))
                        PermissionStates.Add(realPerm, PermissionState.Denied);
                }

                // TODO: Ensure this is everything

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

        public bool Save()
        {
            try
            {
                // TODO: Save here
                return true;
            }
            catch (Exception ex)
            {
                RwCore.Server.Logger.Out("[ERR] Unable to save account '" + Username + "'. Exception: " + ex.Message);
                return false;
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
            if (!RwCore.Server.PermissionAuthority.GroupNames.Contains(name))
                return false;

            PermissionGroup = RwCore.Server.PermissionAuthority.GetGroup(name);
            return true;
        }

        public void ValidatePlayerInstance()
        {
            // Check the server to see if this account has a player instance
        }
    }
}
