/**
 * Oddmatics.RozWorld.Server.Entity.Player -- RozWorld Player Implementation
 *
 * This source-code is part of the server library for the RozWorld project by rozza of Oddmatics:
 * <<http://www.oddmatics.uk>>
 * <<http://roz.world>>
 * <<http://github.com/rozniak/RozWorld-Server>>
 *
 * Sharing, editing and general licence term information can be found inside of the "LICENCE.MD" file that should be located in the root of this project's directory structure.
 */

using Oddmatics.RozWorld.API.Server.Item;
using Oddmatics.RozWorld.API.Server.Entities;
using Oddmatics.RozWorld.Net.Server;
using Oddmatics.RozWorld.Server.Accounts;
using System;
using System.Text.RegularExpressions;

namespace Oddmatics.RozWorld.Server.Entities
{
    public class RwPlayer : Player
    {
        public override bool AFK { get; set; }
        private string _DisplayName; // For bots only
        public override string DisplayName
        {
            get
            {
                if (IsRealPlayer)
                    return Account.DisplayName;
                else
                    return _DisplayName;
            }

            set // TODO: add some verif on value!!
            {
                if (IsRealPlayer)
                    Account.DisplayName = value;
                else
                    _DisplayName = value;
            }
        }
        public override IInventory Inventory { get; set; }
        public override bool IsControllable { get { return IsRealPlayer; } } // For the sake of building rn
        public override bool IsFreezable { get { return true; } }
        public override bool IsFlammable { get { return true; } }
        public override int Mass { get { return 0; } } // TODO: decide this mass much later on
        public override bool IsRealPlayer { get; protected set; }
        public override IItem ItemInHand { get; set; }
        public override bool Joinable { get; set; }
        public override string Status { get; set; }
        public override byte Visibility { get; set; }
        public override bool Online { get { return false; } } // For the sake of building rn
        public override bool VisibleOnScoreboard { get; set; }


        public readonly RwAccount Account;
        private ConnectedClient Client;


        public RwPlayer(RwAccount account, ConnectedClient client)
        {
            if (account.IsServer)
                throw new ArgumentException("RwPlayer.New: Cannot create a player instance for the server account.");

            if (client == null)
                throw new ArgumentNullException("RwPlayer.New: Client cannot be null.");

            Account = account;
            Client = client;
            IsRealPlayer = true;
            AFK = false;
            Joinable = false;
            Status = String.Empty;
            Visibility = 255;
            VisibleOnScoreboard = true;

            // TODO: Load inventory, location, item in hand etc.
        }

        public RwPlayer(string botName)
        {
            // TODO: Create a bot
            IsRealPlayer = false;
            Status = String.Empty;
            Visibility = 255;
            VisibleOnScoreboard = false;
        }


        public override void Ban(string reason = "")
        {
            throw new NotImplementedException();
        }

        public override void BanIP(string reason = "")
        {
            throw new NotImplementedException();
        }

        public override bool HasPermission(string key)
        {
            return false;
        }

        public override void Kick(string reason = "")
        {
            throw new NotImplementedException();
        }

        

        public override void SendInviteTo(Player recipient)
        {
            throw new NotImplementedException();
        }

        public override void SendMessage(string message)
        {
            throw new NotImplementedException();
        }

        public override void SendPrivateMessageTo(string message, Player recipient)
        {
            throw new NotImplementedException();
        }

        public override void SendPublicMessage(string message)
        {
            throw new NotImplementedException();
        }

        public static bool ValidName(string name)
        {
            return new Regex("^[A-Za-z0-9_]+$").IsMatch(name);
        }
    }
}
