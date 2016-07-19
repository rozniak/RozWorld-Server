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
        public IClan Clan
        {
            get { throw new NotImplementedException(); }
        }
        public DateTime CreationDate
        {
            get { if (IsServer) return new DateTime(); else return AccountFile.CreationDate; }
        }
        public IPAddress CreationIP
        {
            get { if (IsServer) return IPAddress.Loopback; else return AccountFile.CreationIP; }
        }
        public IPAddress CurrentIP;
        public string DisplayName
        {
            get { if (IsServer) return "server"; else return AccountFile.DisplayName; }
            set
            {
                if (IsServer)
                    throw new InvalidOperationException("RwAccount.DisplayName.Set: Cannot set server account's display name.");
                else { }
            }
        }
        public IPAddress LastLoginIP
        {
            get { if (IsServer) return IPAddress.Loopback; else return AccountFile.LastLogInIP; }
        }
        public bool IsPlayer { get { return PlayerInstance != null; } }
        public bool IsServer { get; private set; }
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
        public string Username { get { if (IsServer) return "server"; else return AccountFile.Username; } }


        private AccountFile AccountFile;
        public bool LoggedIn { get; private set; }
        private Dictionary<string, PermissionState> PermissionStates;
        public static bool ServerAccountCreated { get; private set; }


        public RwAccount(string accountPath)
        {
            if (accountPath.ToLower() == "server")
            {
                if (ServerAccountCreated)
                    throw new ArgumentException("RwAccount.New: Cannot create server account, one has already been made.");

                IsServer = true;
                LoggedIn = true;
            }
            else
            {
                AccountFile = new AccountFile(accountPath);
                IsServer = false;
                PermissionGroup = RwCore.Server.PermissionAuthority
                    .GetGroup(RwCore.Server.PermissionAuthority.DefaultGroupName);
                PermissionStates = new Dictionary<string, PermissionState>(); // TODO: load this
                LoggedIn = false;
            }
        }


        public PermissionState CheckAccountPermission(string key)
        {
            if (IsServer)
                return PermissionState.Granted;
            else
            {
                if (PermissionStates.ContainsKey(key))
                    return PermissionStates[key];
                else
                    return PermissionState.Unset;
            }
        }

        public bool HasPermission(string key)
        {
            if (IsServer)
                return true;
            else
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
        }

        public RwPlayer InstatePlayerInstance(ConnectedClient client)
        {
            if (!IsServer && !LoggedIn)
                return null;

            if (!IsServer && client == null)
                throw new ArgumentNullException("RwAccount.InstatePlayerInstance: Client cannot be null.");

            // Working on putting in a server player instance
            // Need to make the instance get created in a special case for the server (as a chat bot)
            // Make sure the server is not added to player count when tallying online players :D

            PlayerInstance = new RwPlayer(this, client);

            return (RwPlayer)PlayerInstance;
        }

        public byte LogIn(byte[] passwordHash, long utcHashTime)
        {
            if (LoggedIn)
                return ErrorMessage.INTERNAL_ERROR; // Should not happen

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
            if (!IsServer)
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

            return false;
        }

        public bool SetPermissionGroup(string name)
        {
            if (IsServer || !RwCore.Server.PermissionAuthority.GroupNames.Contains(name))
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
