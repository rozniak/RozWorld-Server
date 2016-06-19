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

using Oddmatics.RozWorld.API.Generic;
using Oddmatics.RozWorld.API.Generic.Level;
using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.API.Server.Game;
using Oddmatics.RozWorld.API.Server.Entities;
using System;

namespace Oddmatics.RozWorld.Server.Entities
{
    public class RwPlayer : Player
    {
        public RwPlayer(string name)
        {

        }

        public override bool AFK { get; set; }

        public override void Ban(string reason = "")
        {
            throw new NotImplementedException();
        }

        public override void BanIP(string reason = "")
        {
            throw new NotImplementedException();
        }

        public override string DisplayName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override bool HasPermission(string key)
        {
            throw new NotImplementedException();
        }

        public override API.Server.Item.IInventory Inventory
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsRealPlayer
        {
            get
            {
                throw new NotImplementedException();
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }

        public override API.Server.Item.IItem ItemInHand
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override bool Joinable
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Kick(string reason = "")
        {
            throw new NotImplementedException();
        }

        public override bool Online
        {
            get { throw new NotImplementedException(); }
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

        public override string Status
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override byte Visibility
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override bool VisibleOnScoreboard
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsFreezable
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsControllable
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsFlammable
        {
            get { throw new NotImplementedException(); }
        }

        public override int Mass
        {
            get { throw new NotImplementedException(); }
        }
    }
}
