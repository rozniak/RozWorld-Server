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
using System;
using System.Text.RegularExpressions;

namespace Oddmatics.RozWorld.Server.Entities
{
    public class RwPlayer : Player
    {
        public override bool AFK { get; set; }
        public override string DisplayName { get; set; }
        public override IInventory Inventory { get; set; }
        public override bool IsControllable { get { return false; } } // For the sake of building rn
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


        public RwPlayer(string name)
        {

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
