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

using LiteDB;
using Oddmatics.RozWorld.API.Server.Entities;
using Oddmatics.RozWorld.API.Generic;
using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.Formats;
using Oddmatics.RozWorld.Net.Packets;
using Oddmatics.RozWorld.Server.Entities;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace Oddmatics.RozWorld.Server.Accounts
{
    public sealed class RwAccountsManager : IAccountsManager
    {
        public const string ACCOUNT_DB_FILENAME = @"accounts.db";


        public int AccountCount { get { return AccountTable.Count(); } }


        private readonly LiteDatabase AccountDatabase;
        private readonly LiteCollection<AccountRecord> AccountTable;


        public RwAccountsManager()
        {
            AccountDatabase = new LiteDatabase(Environment.CurrentDirectory + "\\" + ACCOUNT_DB_FILENAME);
            AccountTable = AccountDatabase.GetCollection<AccountRecord>("Accounts");

            // Ensure indexing is on for required fields
            AccountTable.EnsureIndex(x => x.DisplayName);
            AccountTable.EnsureIndex(x => x.Username);
        }


        public byte CreateAccount(string name, byte[] passwordHash, IPAddress creationIP)
        {
            // Ensure username is valid
            if (!RwPlayer.ValidName(name))
                return ErrorMessage.ACCOUNT_NAME_INVALID;

            // Ensure username is not already in use
            if (AccountTable.Find(x => x.Username.ToLower() == name.ToLower()).Any())
                return ErrorMessage.ACCOUNT_NAME_TAKEN;

            // TODO: Add a check, if disallow duplicate display names is enabled, append randomly generated number on the end of their username

            // Create and insert the account record into the table

            var record = new AccountRecord
            {
                CreationDate = DateTime.UtcNow,
                CreationIP = creationIP,
                DisplayName = name, // Refer to TODO above
                PasswordHash = passwordHash,
                Username = name
            };

            AccountTable.Insert(record);

            return ErrorMessage.NO_ERROR;
        }

        public bool DeleteAccount(string name)
        {
            // TODO: Change this to a HRESULT equivilent

            // TODO: Test this


            // If a player is logged into this account, do not proceed
            if (RwCore.Server.GetPlayerByUsername(name) != null)
                return false;

            // Remove account if it exists
            var results = AccountTable.Find(x => x.Username.ToLower() == name.ToLower());

            if (results.Any())
                AccountTable.Delete(results.ToArray()[0].Id);            

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
