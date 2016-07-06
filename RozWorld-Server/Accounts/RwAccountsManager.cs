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
    public class RwAccountsManager : IAccountsManager
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
            throw new System.NotImplementedException();
        }

        public bool DeleteAccount(IAccount account)
        {
            throw new System.NotImplementedException();
        }

        public IAccount GetAccount(string name)
        {
            // TODO: Perform check to see if player with account is currently online

            string accountName = name.ToLower();
            string[] foundAccounts = Directory.GetFiles(RwServer.DIRECTORY_ACCOUNTS, accountName + ".*.acc");

            if (foundAccounts.Length == 1)
                return new RwAccount(foundAccounts[0]);

            return null;
        }

        public bool RenameAccount(IAccount account, string newName)
        {
            throw new System.NotImplementedException();
        }
    }
}
