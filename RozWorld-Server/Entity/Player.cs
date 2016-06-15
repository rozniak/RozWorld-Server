/**
 * Oddmatics.RozWorld.Server.Entity.Player -- RozWorld Player
 *
 * This source-code is part of the server library for the RozWorld project by rozza of Oddmatics:
 * <<http://www.oddmatics.uk>>
 * <<http://roz.world>>
 * <<http://github.com/rozniak/RozWorld-Server>>
 *
 * Sharing, editing and general licence term information can be found inside of the "LICENCE.MD" file that should be located in the root of this project's directory structure.
 */

using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.API.Server.Entity;
using System;

namespace Oddmatics.RozWorld.Server.Entity
{
    class Player : Entity, IPlayer
    {
        public bool AFK { get; set; }
        public IAccount Account { get; set; }
        public bool Online { get { return true; } } // for the sake of building
        public bool Joinable { get; set; }
        public string Status { get; set; }


        public void Ban(string reason = "") { }
        public void BanIP(string reason = "") { }
        public bool HasPermission(string key) { return Account.HasPermission(key); }
        public void Kick(string reason = "") { }
        public void PrivateMessage(string message, IPlayer sender) { }
        public void SendInvite(IPlayer sender) { }
        public void SendMessage(string message) { }
        public void SendPrivateMessage(string message, IPlayer sender) { }
    }
}
