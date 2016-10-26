/**
 * Oddmatics.RozWorld.Server.Accounts.RwAccountsManager -- RozWorld Server Accounts Manager Implementation
 *
 * This source-code is part of the server library for the RozWorld project by rozza of Oddmatics:
 * <<http://www.oddmatics.uk>>
 * <<http://roz.world>>
 * <<http://github.com/rozniak/RozWorld-Server>>
 *
 * Sharing, editing and general licence term information can be found inside of the "LICENCE.MD" file that should be located in the root of this project's directory structure.
 */

using Oddmatics.RozWorld.API.Server.Entities;
using Oddmatics.RozWorld.API.Generic;
using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.Formats;
using Oddmatics.RozWorld.Net.Packets;
using Oddmatics.RozWorld.Server.Entities;
using Oddmatics.Util.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Oddmatics.RozWorld.Server.Accounts
{
    public sealed class RwAccountsManager : IAccountsManager
    {
        public int AccountCount { get { return Directory.GetFiles(RwServer.DIRECTORY_ACCOUNTS, "*.*.acc").Length; } }


        public byte CreateAccount(string name, byte[] passwordHash, IPAddress creatorIP)
        {
            if (!RwPlayer.ValidName(name))
                return ErrorMessage.ACCOUNT_NAME_INVALID;

            if (AccountFile.Create(name, passwordHash, creatorIP,
                RwServer.DIRECTORY_ACCOUNTS) == null)
                return ErrorMessage.ACCOUNT_NAME_TAKEN;

            return ErrorMessage.NO_ERROR;
        }

        public bool DeleteAccount(string name)
        {
            // If a player is logged into this account, do not proceed
            if (RwCore.Server.GetPlayerByUsername(name) != null)
                return false;

            // Remove any and all account matches
            string[] accountsFound = Directory.GetFiles(RwServer.DIRECTORY_ACCOUNTS,
                name.ToLower() + ".*.acc");

            foreach (string file in accountsFound)
            {
                File.Delete(file);
            }

            return true;
        }

        public bool DeleteAccount(IAccount account)
        {
            return DeleteAccount(account.Username);
        }

        public IAccount GetAccount(string name)
        {
            // See if player is online, retrieve their account if so
            Player player = RwCore.Server.GetPlayerByUsername(name);

            if (player != null)
                return player.Account;

            // Player not online, load from disk
            string accountName = name.ToLower();
            string[] foundAccounts = Directory.GetFiles(RwServer.DIRECTORY_ACCOUNTS, accountName + ".*.acc");

            if (foundAccounts.Length == 1)
                return new RwAccount(foundAccounts[0]);

            return null;
        }

        public bool RenameAccount(IAccount account, string newName)
        {
            // If a player is logged into this account, do not proceed
            if (RwCore.Server.GetPlayerByUsername(account.Username) != null)
                return false;

            var rwAccount = (RwAccount)account;
            string oldFile = RwServer.DIRECTORY_ACCOUNTS + "\\" + account.Fqn + ".acc";

            rwAccount.Username = newName;

            if (rwAccount.Username == newName)
            {

                if (File.Exists(oldFile))
                    File.Delete(oldFile);

                account.Save();

                return true;
            }

            return false;
        }

        public void Save()
        {
            foreach (Player player in RwCore.Server.OnlinePlayers)
            {
                player.Account.Save();
            }
        }
    }
}
