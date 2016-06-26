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
using Oddmatics.RozWorld.Server.Entities;
using Oddmatics.Util.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Oddmatics.RozWorld.Server.Accounts
{
    public class RwAccount : IAccount
    {
        public IClan Clan
        {
            get { throw new NotImplementedException(); }
        }
        public DateTime CreationDate { get; private set; }
        public IPAddress CreationIP { get; private set; }
        public IPAddress CurrentIP { get; private set; }
        public string DisplayName { get; set; }
        public IPAddress LastLoginIP { get; private set; }
        public bool IsPlayer { get { return PlayerInstance != null; } }
        public bool IsServer { get { return Username == "server"; } }
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
        public string Username { get; private set; }


        private Dictionary<string, PermissionState> PermissionStates;


        public RwAccount(string username, byte[] passwordHash, IPAddress loginIP)
        {
            string realUsername = username.ToLower();

            if (realUsername == "server")
                Username = realUsername;
            else
            {
                string[] account = Directory.GetFiles(RwServer.DIRECTORY_ACCOUNTS, realUsername + ".*.acc");

                if (account.Length == 1) // Account exists - load it
                {

                }
                else if (account.Length == 0) // Account not found
                    throw new IOException("RwAccount.New: Account file not found for '" + realUsername + "'.");
                else // Duplicate accounts found - unacceptable
                    throw new IOException("RwAccount.New: Duplicate account files found for the account '" + realUsername + "'.");
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
                    default:
                    case PermissionState.Unset: break;
                }

                return PermissionGroup.HasPermission(key);
            }
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
