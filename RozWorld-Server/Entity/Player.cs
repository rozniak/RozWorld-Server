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

using Oddmatics.RozWorld.API.Generic;
using Oddmatics.RozWorld.API.Generic.Level;
using Oddmatics.RozWorld.API.Server.Accounts;
using Oddmatics.RozWorld.API.Server.Game;
using Oddmatics.RozWorld.API.Server.Entity;
using System;

namespace Oddmatics.RozWorld.Server.Entity
{
    public class Player : IPlayer
    {
        public ushort ID { get { return 0; } } // for the sake of building
        public bool IsValid { get; private set; }
        public bool IsControllable { get; private set; }
        public bool AFK { get; set; }
        public IAccount Account { get; private set; }
        public string DisplayName { get; set; }
        public bool IsRealPlayer { get; private set; }
        public bool Joinable { get; set; }
        public bool Online { get; private set; }
        public string Status { get; set; }
        public byte Visibility { get; set; }
        public bool VisibleOnScoreboard { get; set; }
        private static ILivingEntityAttributes _Attributes;
        public ILivingEntityAttributes Attributes
        {
            get { return _Attributes; }
            private set { _Attributes = value; }
        }
        public int Health { get; set; }
        public bool IsAlive { get; private set; }
        public bool IsBurning { get; private set; }
        public bool IsFrozen { get; private set; }
        public bool IsPoisoned { get; private set; }
        public bool IsStunned { get; private set; }
        public SafeStatHandler Stats { get; private set; }
        public ServerEntityUpdate UpdatePermit { get; private set; }
        public Location Location { get; private set; }


        public Player(string name)
        {
            if (Attributes == null)
                Attributes = new PlayerAttributes();

            if (string.IsNullOrWhiteSpace(name) || name == "~")
                throw new ArgumentException("Invalid name specified for this Player instance.");

            if (name.StartsWith("~"))
            {
                DisplayName = name.Substring(1);
            }
            else
            {
                DisplayName = name;
                IsRealPlayer = true;
            }

            Stats = new SafeStatHandler(RwCore.Server.StatCalculator);
        }


        public void Ban(string reason = "")
        {
            throw new NotImplementedException();
        }

        public void BanIP(string reason = "")
        {
            throw new NotImplementedException();
        }

        public void ChangeState(short newState)
        {
            throw new NotImplementedException();
        }

        public bool HasPermission(string key)
        {
            return Account.HasPermission(key);
        }

        public void Kick(string reason = "")
        {
            throw new NotImplementedException();
        }

        public void Kill()
        {
            Health = 0;
            // Trigger an entitydeathevent here
        }

        public void SendInvite(IPlayer sender)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(string message)
        {
            throw new NotImplementedException();
        }

        public void SendPrivateMessage(string message, IPlayer sender)
        {
            throw new NotImplementedException();
        }

        public void SetVelocity(Velocity velocity)
        {
            throw new NotImplementedException();
        }

        public void TeleportTo(int segX, int segY, int segZ, int localX, int localY)
        {
            throw new NotImplementedException();
        }

        public void TeleportTo(Location target)
        {
            throw new NotImplementedException();
        }

        public void TeleportTo(IEntity target)
        {
            throw new NotImplementedException();
        }
    }
}
