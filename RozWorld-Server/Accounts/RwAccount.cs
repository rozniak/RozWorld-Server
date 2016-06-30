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
using Oddmatics.RozWorld.Net.Packets;
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


        public bool LoggedIn { get; private set; }
        private IList<byte> PasswordHash;
        private Dictionary<string, PermissionState> PermissionStates;
        public bool Valid { get; private set; }


        public RwAccount(string username)
        {
            string realUsername = username.ToLower();

            if (realUsername == "server")
                Username = realUsername;
            else
            {
                //string[] accountFiles = Directory.GetFiles(RwServer.DIRECTORY_ACCOUNTS, realUsername + ".*.acc");

                //if (accountFiles.Length == 1)
                //{
                //    // Load bytes into a List<byte> collection so .GetRange() can be used
                //    var accountFile = new List<byte>(FileSystem.GetBinaryFile(accountFiles[0]));

                //    int currentIndex = 0;
                //    Username = ByteParse.NextStringByLength(accountFile, ref currentIndex, 1);
                //    DisplayName = ByteParse.NextStringByLength(accountFile, ref currentIndex, 2);
                //    PasswordHash = accountFile.GetRange(currentIndex, 32).AsReadOnly();
                //    currentIndex += 32;

                //    IPAddress creationIP = IPAddress.None;
                //    IPAddress lastIP = IPAddress.None;
                //    bool validIPs = 
                //        IPAddress.TryParse(ByteParse.NextStringByLength(accountFile, ref currentIndex, 1), out creationIP) &&
                //        IPAddress.TryParse(ByteParse.NextStringByLength(accountFile, ref currentIndex, 1), out lastIP);

                //    CreationIP = creationIP;
                //    LastLoginIP = lastIP;
                //}
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

        public byte LogIn(byte[] passwordHash, long utcHashTime)
        {
            if (!Valid) // Username invalid since account isn't loaded
                return ErrorMessage.INCORRECT_LOGIN;

            if (LoggedIn)
                return ErrorMessage.INTERNAL_ERROR; // Should not happen

            var saltedPass = new List<byte>(PasswordHash);
            saltedPass.AddRange(utcHashTime.GetBytes());

            byte[] comparisonHash = new SHA256Managed().ComputeHash(saltedPass.ToArray());

            if (comparisonHash.SequenceEqual(passwordHash))
            {
                PasswordHash = null;
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
