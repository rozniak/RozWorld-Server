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

            string realName = name.ToLower();

            if (Directory.GetFiles(RwServer.DIRECTORY_ACCOUNTS, realName + ".*.acc").Length == 0)
            {
                const byte maxAttempts = 4;
                byte attempts = 0;
                string finalDisplayName = String.Empty;
                string underscores = String.Empty;

                while (Directory.GetFiles(RwServer.DIRECTORY_ACCOUNTS, "*." + realName + underscores + ".acc")
                    .Length > 0)
                {
                    if (++attempts > maxAttempts)
                        return ErrorMessage.INTERNAL_ERROR;

                    underscores += "_";
                }

                finalDisplayName = name + underscores;

                var accountFile = new List<byte>();

                /**
                 * Set up account file as:
                 *   [Account name]
                 *   [Display name]
                 *   [Password hash]
                 *   [Creator's IP]
                 *   [Last login IP] -- basically null here
                 */
                accountFile.AddRange(name.GetBytesByLength(1));
                accountFile.AddRange(finalDisplayName.GetBytesByLength(2));
                accountFile.AddRange(passwordHash);
                accountFile.AddRange(creatorIP.ToString().GetBytesByLength(1));
                accountFile.AddRange(IPAddress.None.ToString().GetBytesByLength(1));

                FileSystem.PutBinaryFile(RwServer.DIRECTORY_ACCOUNTS + @"\" + realName + "."
                    + finalDisplayName.ToLower() + ".acc", accountFile.ToArray());

                return ErrorMessage.NO_ERROR;
            }

            return ErrorMessage.ACCOUNT_NAME_TAKEN;
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
            throw new System.NotImplementedException();
        }

        public bool RenameAccount(IAccount account, string newName)
        {
            throw new System.NotImplementedException();
        }
    }
}
