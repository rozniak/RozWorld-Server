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
using System;
using System.Collections.Generic;
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
        public IPAddress LastLoginIP { get; private set; }
        public bool IsPlayer { get; private set; }
        public bool IsServer { get; private set; }
        private string _PermissionGroupName = String.Empty;
        public string PermissionGroupName
        {
            get { return _PermissionGroupName; }
            set
            {
                if (IsServer)
                    throw new NotSupportedException("Permission group cannot be set for the server account.");

                if (!RwCore.Server.PermissionAuthority.GroupNames.Contains(value))
                    throw new MissingMemberException("The permission group \"" + value + "\" does not exist.");

                _PermissionGroupName = value;
            }
        }
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


        public RwAccount(string username)
        {
            string realUsername = username.ToLower();

            if (username == "server")
            {
                IsServer = true;
                Username = realUsername;
            }
            else
            {
                // Load account details here
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

                return RwCore.Server.PermissionAuthority.GetGroup(PermissionGroupName).HasPermission(key);
            }
        }

        public void SetAccountPermission(string key, PermissionState newState)
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
            }
            else
                throw new InvalidOperationException("Account permissions cannot be set for the server account.");
        }

        public void ValidatePlayerInstance()
        {
            // Check the server to see if this account has a player instance
        }
    }
}
